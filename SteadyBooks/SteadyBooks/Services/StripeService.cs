using Stripe;
using Stripe.Checkout;
using SteadyBooks.Models;
using Microsoft.Extensions.Options;

namespace SteadyBooks.Services;

public interface IStripeService
{
    Task<string> CreateCheckoutSessionAsync(string userId, string userEmail, SubscriptionPlan plan, BillingInterval interval, string successUrl, string cancelUrl);
    Task<string> CreateCustomerPortalSessionAsync(string customerId, string returnUrl);
    Task<Stripe.Subscription?> GetSubscriptionAsync(string subscriptionId);
    Task<Stripe.Subscription> CancelSubscriptionAsync(string subscriptionId);
    Task<Customer> GetOrCreateCustomerAsync(string userId, string email, string? name = null);
    PlanDetails GetPlanDetails(SubscriptionPlan plan, BillingInterval interval);
}

public class StripeService : IStripeService
{
    private readonly StripeSettings _settings;
    private readonly ILogger<StripeService> _logger;

    public StripeService(IOptions<StripeSettings> settings, ILogger<StripeService> logger)
    {
        _settings = settings.Value;
        _logger = logger;
        StripeConfiguration.ApiKey = _settings.SecretKey;
    }

    public async Task<string> CreateCheckoutSessionAsync(
        string userId,
        string userEmail,
        SubscriptionPlan plan,
        BillingInterval interval,
        string successUrl,
        string cancelUrl)
    {
        try
        {
            var planDetails = GetPlanDetails(plan, interval);

            var options = new SessionCreateOptions
            {
                PaymentMethodTypes = new List<string> { "card" },
                Mode = "subscription",
                CustomerEmail = userEmail,
                ClientReferenceId = userId,
                LineItems = new List<SessionLineItemOptions>
                {
                    new SessionLineItemOptions
                    {
                        Price = planDetails.StripePriceId,
                        Quantity = 1,
                    },
                },
                SuccessUrl = successUrl,
                CancelUrl = cancelUrl,
                BillingAddressCollection = "required",
                AllowPromotionCodes = true,
                Metadata = new Dictionary<string, string>
                {
                    { "user_id", userId },
                    { "plan", plan.ToString() },
                    { "interval", interval.ToString() }
                },
                SubscriptionData = new SessionSubscriptionDataOptions
                {
                    Metadata = new Dictionary<string, string>
                    {
                        { "user_id", userId },
                        { "plan", plan.ToString() }
                    },
                    TrialPeriodDays = plan == SubscriptionPlan.Professional || plan == SubscriptionPlan.Business ? 14 : null
                }
            };

            var service = new SessionService();
            var session = await service.CreateAsync(options);

            _logger.LogInformation("Created Stripe checkout session {SessionId} for user {UserId}", session.Id, userId);

            return session.Url;
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Error creating Stripe checkout session for user {UserId}", userId);
            throw;
        }
    }

    public async Task<string> CreateCustomerPortalSessionAsync(string customerId, string returnUrl)
    {
        try
        {
            var options = new Stripe.BillingPortal.SessionCreateOptions
            {
                Customer = customerId,
                ReturnUrl = returnUrl,
            };

            var service = new Stripe.BillingPortal.SessionService();
            var session = await service.CreateAsync(options);

            _logger.LogInformation("Created customer portal session for customer {CustomerId}", customerId);

            return session.Url;
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Error creating customer portal session for customer {CustomerId}", customerId);
            throw;
        }
    }

    public async Task<Stripe.Subscription?> GetSubscriptionAsync(string subscriptionId)
    {
        try
        {
            var service = new SubscriptionService();
            return await service.GetAsync(subscriptionId);
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Error retrieving subscription {SubscriptionId}", subscriptionId);
            return null;
        }
    }

    public async Task<Stripe.Subscription> CancelSubscriptionAsync(string subscriptionId)
    {
        try
        {
            var service = new SubscriptionService();
            var subscription = await service.CancelAsync(subscriptionId);

            _logger.LogInformation("Canceled subscription {SubscriptionId}", subscriptionId);

            return subscription;
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Error canceling subscription {SubscriptionId}", subscriptionId);
            throw;
        }
    }

    public async Task<Customer> GetOrCreateCustomerAsync(string userId, string email, string? name = null)
    {
        try
        {
            var service = new CustomerService();

            // Try to find existing customer
            var searchOptions = new CustomerSearchOptions
            {
                Query = $"email:'{email}'",
            };

            var customers = await service.SearchAsync(searchOptions);
            var customer = customers.FirstOrDefault();

            if (customer != null)
            {
                _logger.LogInformation("Found existing Stripe customer {CustomerId} for user {UserId}", customer.Id, userId);
                return customer;
            }

            // Create new customer
            var createOptions = new CustomerCreateOptions
            {
                Email = email,
                Name = name,
                Metadata = new Dictionary<string, string>
                {
                    { "user_id", userId }
                }
            };

            customer = await service.CreateAsync(createOptions);

            _logger.LogInformation("Created new Stripe customer {CustomerId} for user {UserId}", customer.Id, userId);

            return customer;
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Error getting or creating Stripe customer for user {UserId}", userId);
            throw;
        }
    }

    public PlanDetails GetPlanDetails(SubscriptionPlan plan, BillingInterval interval)
    {
        return plan switch
        {
            SubscriptionPlan.Professional when interval == BillingInterval.Month => new PlanDetails
            {
                Plan = SubscriptionPlan.Professional,
                Name = "Professional",
                Description = "Perfect for growing firms",
                Price = 29,
                Interval = BillingInterval.Month,
                MaxDashboards = 10,
                Features = new List<string>
                {
                    "10 Client Dashboards",
                    "QuickBooks Online Integration",
                    "White-label Branding",
                    "Email Support",
                    "14-day free trial"
                },
                StripePriceId = _settings.ProfessionalMonthlyPriceId
            },
            SubscriptionPlan.Professional when interval == BillingInterval.Year => new PlanDetails
            {
                Plan = SubscriptionPlan.Professional,
                Name = "Professional (Annual)",
                Description = "Perfect for growing firms - Save 20%",
                Price = 278, // $23.17/month
                Interval = BillingInterval.Year,
                MaxDashboards = 10,
                Features = new List<string>
                {
                    "10 Client Dashboards",
                    "QuickBooks Online Integration",
                    "White-label Branding",
                    "Email Support",
                    "14-day free trial",
                    "Save $70/year"
                },
                StripePriceId = _settings.ProfessionalYearlyPriceId
            },
            SubscriptionPlan.Business when interval == BillingInterval.Month => new PlanDetails
            {
                Plan = SubscriptionPlan.Business,
                Name = "Business",
                Description = "Best for established firms",
                Price = 79,
                Interval = BillingInterval.Month,
                MaxDashboards = -1, // Unlimited
                Features = new List<string>
                {
                    "Unlimited Client Dashboards",
                    "QuickBooks Online Integration",
                    "White-label Branding",
                    "Priority Support",
                    "Custom Reports",
                    "14-day free trial"
                },
                StripePriceId = _settings.BusinessMonthlyPriceId
            },
            SubscriptionPlan.Business when interval == BillingInterval.Year => new PlanDetails
            {
                Plan = SubscriptionPlan.Business,
                Name = "Business (Annual)",
                Description = "Best for established firms - Save 20%",
                Price = 758, // $63.17/month
                Interval = BillingInterval.Year,
                MaxDashboards = -1, // Unlimited
                Features = new List<string>
                {
                    "Unlimited Client Dashboards",
                    "QuickBooks Online Integration",
                    "White-label Branding",
                    "Priority Support",
                    "Custom Reports",
                    "14-day free trial",
                    "Save $190/year"
                },
                StripePriceId = _settings.BusinessYearlyPriceId
            },
            _ => new PlanDetails
            {
                Plan = SubscriptionPlan.FreeTrial,
                Name = "Free Trial",
                Description = "Try all features risk-free for 30 days",
                Price = 0,
                Interval = BillingInterval.Month,
                MaxDashboards = 3,
                Features = new List<string>
                {
                    "3 Client Dashboards",
                    "All Features Included",
                    "30-day trial",
                    "No credit card required"
                },
                StripePriceId = string.Empty
            }
        };
    }
}

public class PlanDetails
{
    public SubscriptionPlan Plan { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public BillingInterval Interval { get; set; }
    public int MaxDashboards { get; set; } // -1 for unlimited
    public List<string> Features { get; set; } = new();
    public string StripePriceId { get; set; } = string.Empty;
    public bool IsPopular => Plan == SubscriptionPlan.Business;
}

public class StripeSettings
{
    public string PublishableKey { get; set; } = string.Empty;
    public string SecretKey { get; set; } = string.Empty;
    public string WebhookSecret { get; set; } = string.Empty;
    
    // Price IDs (create these in Stripe Dashboard)
    public string ProfessionalMonthlyPriceId { get; set; } = string.Empty;
    public string ProfessionalYearlyPriceId { get; set; } = string.Empty;
    public string BusinessMonthlyPriceId { get; set; } = string.Empty;
    public string BusinessYearlyPriceId { get; set; } = string.Empty;
}
