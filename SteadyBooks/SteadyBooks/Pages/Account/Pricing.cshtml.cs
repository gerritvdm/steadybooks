using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SteadyBooks.Models;
using SteadyBooks.Services;

namespace SteadyBooks.Pages.Account
{
    public class PricingModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IStripeService _stripeService;
        private readonly ILogger<PricingModel> _logger;

        public PricingModel(
            UserManager<ApplicationUser> userManager,
            IStripeService stripeService,
            ILogger<PricingModel> logger)
        {
            _userManager = userManager;
            _stripeService = stripeService;
            _logger = logger;
        }

        [TempData]
        public string? ErrorMessage { get; set; }

        public void OnGet()
        {
        }

        public async Task<IActionResult> OnPostSubscribeAsync(string plan, string interval)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToPage("/Identity/Account/Login");
            }

            try
            {
                // Parse plan and interval
                if (!Enum.TryParse<SubscriptionPlan>(plan, out var subscriptionPlan))
                {
                    ErrorMessage = "Invalid plan selected.";
                    return RedirectToPage();
                }

                if (!Enum.TryParse<BillingInterval>(interval, out var billingInterval))
                {
                    ErrorMessage = "Invalid billing interval selected.";
                    return RedirectToPage();
                }

                // Create checkout URLs
                var successUrl = Url.Page("/Account/CheckoutSuccess", null, null, Request.Scheme);
                var cancelUrl = Url.Page("/Account/Pricing", null, null, Request.Scheme);

                // Create Stripe checkout session
                var checkoutUrl = await _stripeService.CreateCheckoutSessionAsync(
                    user.Id,
                    user.Email!,
                    subscriptionPlan,
                    billingInterval,
                    successUrl!,
                    cancelUrl!
                );

                _logger.LogInformation("User {UserId} initiated checkout for {Plan} {Interval}", 
                    user.Id, plan, interval);

                // Redirect to Stripe Checkout
                return Redirect(checkoutUrl);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating checkout session for user {UserId}", user?.Id);
                ErrorMessage = "An error occurred while processing your request. Please try again.";
                return RedirectToPage();
            }
        }
    }
}
