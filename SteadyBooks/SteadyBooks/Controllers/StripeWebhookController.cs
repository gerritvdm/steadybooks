using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Stripe;
using Stripe.Checkout;
using SteadyBooks.Data;
using SteadyBooks.Models;
using SteadyBooks.Services;

namespace SteadyBooks.Controllers;

[Route("api/[controller]")]
[ApiController]
public class StripeWebhookController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly StripeSettings _stripeSettings;
    private readonly ILogger<StripeWebhookController> _logger;

    public StripeWebhookController(
        ApplicationDbContext context,
        IOptions<StripeSettings> stripeSettings,
        ILogger<StripeWebhookController> logger)
    {
        _context = context;
        _stripeSettings = stripeSettings.Value;
        _logger = logger;
    }

    [HttpPost]
    public async Task<IActionResult> Index()
    {
        var json = await new StreamReader(HttpContext.Request.Body).ReadToEndAsync();

        try
        {
            var stripeEvent = EventUtility.ConstructEvent(
                json,
                Request.Headers["Stripe-Signature"],
                _stripeSettings.WebhookSecret
            );

            _logger.LogInformation("Received Stripe webhook: {EventType}", stripeEvent.Type);

            // Handle the event
            switch (stripeEvent.Type)
            {
                case "checkout.session.completed":
                    await HandleCheckoutSessionCompletedAsync(stripeEvent);
                    break;

                case "customer.subscription.created":
                case "customer.subscription.updated":
                    await HandleSubscriptionUpdatedAsync(stripeEvent);
                    break;

                case "customer.subscription.deleted":
                    await HandleSubscriptionDeletedAsync(stripeEvent);
                    break;

                case "invoice.payment_succeeded":
                    await HandleInvoicePaymentSucceededAsync(stripeEvent);
                    break;

                case "invoice.payment_failed":
                    await HandleInvoicePaymentFailedAsync(stripeEvent);
                    break;

                default:
                    _logger.LogInformation("Unhandled event type: {EventType}", stripeEvent.Type);
                    break;
            }

            return Ok();
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Stripe webhook error");
            return BadRequest();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing webhook");
            return StatusCode(500);
        }
    }

    private async Task HandleCheckoutSessionCompletedAsync(Event stripeEvent)
    {
        var session = stripeEvent.Data.Object as Session;
        if (session == null) return;

        var userId = session.ClientReferenceId ?? session.Metadata?["user_id"];
        if (string.IsNullOrEmpty(userId))
        {
            _logger.LogWarning("Checkout session completed but no user ID found");
            return;
        }

        try
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                _logger.LogWarning("User {UserId} not found for checkout session", userId);
                return;
            }

            // Update user's Stripe customer ID
            user.StripeCustomerId = session.CustomerId;
            
            await _context.SaveChangesAsync();

            _logger.LogInformation("Checkout completed for user {UserId}, subscription {SubscriptionId}", 
                userId, session.Subscription);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling checkout session completed for user {UserId}", userId);
        }
    }

    private async Task HandleSubscriptionUpdatedAsync(Event stripeEvent)
    {
        var stripeSubscription = stripeEvent.Data.Object as Stripe.Subscription;
        if (stripeSubscription == null) return;

        try
        {
            var userId = stripeSubscription.Metadata?["user_id"];
            if (string.IsNullOrEmpty(userId))
            {
                _logger.LogWarning("Subscription event received but no user ID in metadata");
                return;
            }

            var user = await _context.Users
                .Include(u => u.Subscription)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
            {
                _logger.LogWarning("User {UserId} not found for subscription update", userId);
                return;
            }

            // Parse plan from metadata
            Enum.TryParse<SubscriptionPlan>(stripeSubscription.Metadata?["plan"], out var plan);

            // Check if subscription exists
            var dbSubscription = user.Subscription;
            if (dbSubscription == null)
            {
                dbSubscription = new Models.Subscription
                {
                    UserId = userId,
                    User = user,
                    CreatedDate = DateTime.UtcNow
                };
                _context.Subscriptions.Add(dbSubscription);
            }

            // Update subscription - just basic fields for now
            dbSubscription.StripeCustomerId = stripeSubscription.CustomerId;
            dbSubscription.StripeSubscriptionId = stripeSubscription.Id;
            dbSubscription.StripePriceId = stripeSubscription.Items.Data[0].Price.Id;
            dbSubscription.Plan = plan;
            dbSubscription.Status = MapStripeStatus(stripeSubscription.Status);
            dbSubscription.Amount = (stripeSubscription.Items.Data[0].Price.UnitAmount ?? 0) / 100m;
            dbSubscription.Currency = stripeSubscription.Currency;
            dbSubscription.Interval = stripeSubscription.Items.Data[0].Price.Recurring?.Interval == "year" 
                ? BillingInterval.Year 
                : BillingInterval.Month;
            
            // Set current period dates to now for MVP
            dbSubscription.CurrentPeriodStart = DateTime.UtcNow;
            dbSubscription.CurrentPeriodEnd = DateTime.UtcNow.AddMonths(dbSubscription.Interval == BillingInterval.Year ? 12 : 1);
            
            dbSubscription.ModifiedDate = DateTime.UtcNow;

            // Update user's current plan
            user.CurrentPlan = plan;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Subscription updated for user {UserId}: {Status}", userId, dbSubscription.Status);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling subscription update");
        }
    }

    private async Task HandleSubscriptionDeletedAsync(Event stripeEvent)
    {
        var stripeSubscription = stripeEvent.Data.Object as Stripe.Subscription;
        if (stripeSubscription == null) return;

        try
        {
            var dbSubscription = await _context.Subscriptions
                .Include(s => s.User)
                .FirstOrDefaultAsync(s => s.StripeSubscriptionId == stripeSubscription.Id);

            if (dbSubscription != null)
            {
                dbSubscription.Status = SubscriptionStatus.Canceled;
                dbSubscription.CanceledAt = DateTime.UtcNow;
                dbSubscription.ModifiedDate = DateTime.UtcNow;

                // Revert user to free trial
                dbSubscription.User.CurrentPlan = SubscriptionPlan.FreeTrial;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Subscription canceled for user {UserId}", dbSubscription.UserId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling subscription deletion");
        }
    }

    private async Task HandleInvoicePaymentSucceededAsync(Event stripeEvent)
    {
        try
        {
            // Access the subscription ID from the raw JSON data
            var data = stripeEvent.Data.Object as Stripe.StripeEntity;
            if (data == null) return;

            // Get subscription ID from raw data
            var subscriptionId = data.RawJObject?["subscription"]?.ToString();
            if (string.IsNullOrEmpty(subscriptionId)) return;

            var dbSubscription = await _context.Subscriptions
                .FirstOrDefaultAsync(s => s.StripeSubscriptionId == subscriptionId);

            if (dbSubscription != null)
            {
                dbSubscription.Status = SubscriptionStatus.Active;
                dbSubscription.ModifiedDate = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Invoice payment succeeded for subscription {SubscriptionId}", 
                    dbSubscription.StripeSubscriptionId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling invoice payment succeeded");
        }
    }

    private async Task HandleInvoicePaymentFailedAsync(Event stripeEvent)
    {
        try
        {
            // Access the subscription ID from the raw JSON data
            var data = stripeEvent.Data.Object as Stripe.StripeEntity;
            if (data == null) return;

            // Get subscription ID from raw data
            var subscriptionId = data.RawJObject?["subscription"]?.ToString();
            if (string.IsNullOrEmpty(subscriptionId)) return;

            var dbSubscription = await _context.Subscriptions
                .FirstOrDefaultAsync(s => s.StripeSubscriptionId == subscriptionId);

            if (dbSubscription != null)
            {
                dbSubscription.Status = SubscriptionStatus.PastDue;
                dbSubscription.ModifiedDate = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                _logger.LogWarning("Invoice payment failed for subscription {SubscriptionId}", 
                    dbSubscription.StripeSubscriptionId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling invoice payment failed");
        }
    }

    private SubscriptionStatus MapStripeStatus(string stripeStatus)
    {
        return stripeStatus.ToLower() switch
        {
            "trialing" => SubscriptionStatus.Trialing,
            "active" => SubscriptionStatus.Active,
            "past_due" => SubscriptionStatus.PastDue,
            "canceled" => SubscriptionStatus.Canceled,
            "unpaid" => SubscriptionStatus.Unpaid,
            "incomplete" => SubscriptionStatus.Incomplete,
            "incomplete_expired" => SubscriptionStatus.IncompleteExpired,
            _ => SubscriptionStatus.Active
        };
    }
}
