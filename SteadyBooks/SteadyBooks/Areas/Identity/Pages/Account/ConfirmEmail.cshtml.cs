using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;
using System.Text;
using SteadyBooks.Models;

namespace SteadyBooks.Areas.Identity.Pages.Account
{
    public class ConfirmEmailModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<ConfirmEmailModel> _logger;

        public ConfirmEmailModel(UserManager<ApplicationUser> userManager, ILogger<ConfirmEmailModel> logger)
        {
            _userManager = userManager;
            _logger = logger;
        }

        public bool IsSuccess { get; set; }
        public string? ErrorMessage { get; set; }
        public string? ReturnUrl { get; set; }

        public async Task<IActionResult> OnGetAsync(string userId, string code, string? returnUrl = null)
        {
            if (userId == null || code == null)
            {
                IsSuccess = false;
                ErrorMessage = "Invalid confirmation link. Please check your email and try again.";
                return Page();
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                IsSuccess = false;
                ErrorMessage = $"Unable to load user with ID '{userId}'.";
                return Page();
            }

            try
            {
                code = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(code));
                var result = await _userManager.ConfirmEmailAsync(user, code);
                
                if (result.Succeeded)
                {
                    IsSuccess = true;
                    ReturnUrl = returnUrl;
                    _logger.LogInformation("User {UserId} confirmed their email successfully", userId);
                }
                else
                {
                    IsSuccess = false;
                    ErrorMessage = "Error confirming your email. The link may have expired or already been used.";
                    _logger.LogWarning("Failed to confirm email for user {UserId}: {Errors}", 
                        userId, string.Join(", ", result.Errors.Select(e => e.Description)));
                }
            }
            catch (Exception ex)
            {
                IsSuccess = false;
                ErrorMessage = "An error occurred while confirming your email. Please try again or contact support.";
                _logger.LogError(ex, "Exception while confirming email for user {UserId}", userId);
            }

            return Page();
        }
    }
}
