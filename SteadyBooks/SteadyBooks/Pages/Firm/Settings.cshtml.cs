using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SteadyBooks.Models;
using SteadyBooks.Services;
using System.ComponentModel.DataAnnotations;

namespace SteadyBooks.Pages.Firm
{
    [Authorize]
    public class SettingsModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IFileUploadService _fileUploadService;
        private readonly ILogger<SettingsModel> _logger;

        public SettingsModel(
            UserManager<ApplicationUser> userManager,
            IFileUploadService fileUploadService,
            ILogger<SettingsModel> logger)
        {
            _userManager = userManager;
            _fileUploadService = fileUploadService;
            _logger = logger;
            Input = new InputModel(); // Initialize to prevent null reference
        }

        [BindProperty]
        public InputModel Input { get; set; }

        [TempData]
        public string? SuccessMessage { get; set; }

        [TempData]
        public string? ErrorMessage { get; set; }

        public class InputModel
        {
            [Required]
            [StringLength(200)]
            [Display(Name = "Firm Name")]
            public string FirmName { get; set; } = string.Empty;

            [Required]
            [EmailAddress]
            [Display(Name = "Primary Contact Email")]
            public string PrimaryContactEmail { get; set; } = string.Empty;

            [Phone]
            [StringLength(20)]
            [Display(Name = "Contact Phone")]
            public string? ContactPhone { get; set; }

            [Display(Name = "Logo")]
            public IFormFile? LogoFile { get; set; }

            public string? CurrentLogoPath { get; set; }

            [Required]
            [RegularExpression(@"^#[0-9A-Fa-f]{6}$", ErrorMessage = "Please enter a valid hex color (e.g., #667eea)")]
            [Display(Name = "Brand Color")]
            public string BrandColor { get; set; } = "#667eea";

            [StringLength(500)]
            [Display(Name = "Footer Message")]
            public string? FooterMessage { get; set; }
        }

        public async Task<IActionResult> OnGetAsync()
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    _logger.LogWarning("User not found when accessing settings");
                    return NotFound("User not found.");
                }

                Input = new InputModel
                {
                    FirmName = user.FirmName ?? string.Empty,
                    PrimaryContactEmail = user.PrimaryContactEmail ?? user.Email ?? string.Empty,
                    ContactPhone = user.ContactPhone,
                    CurrentLogoPath = user.LogoPath,
                    BrandColor = user.BrandColor ?? "#667eea",
                    FooterMessage = user.FooterMessage
                };

                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading firm settings");
                ErrorMessage = "An error occurred while loading settings.";
                return Page();
            }
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                // Reload user to prevent null reference
                var currentUser = await _userManager.GetUserAsync(User);
                if (currentUser != null && Input != null)
                {
                    Input.CurrentLogoPath = currentUser.LogoPath;
                }
                
                return Page();
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                _logger.LogWarning("User not found when saving settings");
                ErrorMessage = "User not found. Please log in again.";
                return RedirectToPage("/Identity/Account/Login");
            }

            try
            {
                // Update basic fields
                user.FirmName = Input.FirmName ?? user.FirmName ?? "";
                user.PrimaryContactEmail = Input.PrimaryContactEmail ?? user.PrimaryContactEmail ?? "";
                user.ContactPhone = Input.ContactPhone;
                user.BrandColor = Input.BrandColor ?? "#667eea";
                user.FooterMessage = Input.FooterMessage;

                // Handle logo upload if file provided
                if (Input.LogoFile != null && Input.LogoFile.Length > 0)
                {
                    if (!_fileUploadService.IsValidLogoFile(Input.LogoFile))
                    {
                        ModelState.AddModelError("Input.LogoFile", "Invalid logo file. Please upload a PNG, JPG, or SVG file under 2MB.");
                        Input.CurrentLogoPath = user.LogoPath;
                        return Page();
                    }

                    // Delete old logo if exists
                    if (!string.IsNullOrEmpty(user.LogoPath))
                    {
                        await _fileUploadService.DeleteLogoAsync(user.LogoPath);
                    }

                    // Upload new logo
                    var logoPath = await _fileUploadService.UploadLogoAsync(Input.LogoFile, user.Id);
                    
                    if (logoPath != null)
                    {
                        user.LogoPath = logoPath;
                        Input.CurrentLogoPath = logoPath;
                        _logger.LogInformation("Logo uploaded successfully for user {UserId}", user.Id);
                    }
                    else
                    {
                        _logger.LogError("Failed to upload logo for user {UserId}", user.Id);
                        ModelState.AddModelError("Input.LogoFile", "Failed to upload logo. Please try again.");
                        Input.CurrentLogoPath = user.LogoPath;
                        return Page();
                    }
                }

                // Save to database
                var result = await _userManager.UpdateAsync(user);
                
                if (result.Succeeded)
                {
                    _logger.LogInformation("User {UserId} updated firm settings successfully", user.Id);
                    SuccessMessage = "Settings saved successfully!";
                    return RedirectToPage();
                }

                // Handle save errors
                foreach (var error in result.Errors)
                {
                    _logger.LogWarning("Error updating user {UserId}: {Error}", user.Id, error.Description);
                    ModelState.AddModelError(string.Empty, error.Description);
                }

                Input.CurrentLogoPath = user.LogoPath;
                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating firm settings for user {UserId}", user.Id);
                ErrorMessage = "An error occurred while saving settings. Please try again.";
                
                // Try to reload user data
                try
                {
                    if (Input != null)
                    {
                        Input.CurrentLogoPath = user.LogoPath;
                    }
                }
                catch
                {
                    // Ignore errors during error recovery
                }

                return Page();
            }
        }

        public async Task<IActionResult> OnPostRemoveLogoAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                _logger.LogWarning("User not found when removing logo");
                return NotFound("User not found.");
            }

            try
            {
                if (!string.IsNullOrEmpty(user.LogoPath))
                {
                    await _fileUploadService.DeleteLogoAsync(user.LogoPath);
                    user.LogoPath = null;

                    var result = await _userManager.UpdateAsync(user);
                    if (result.Succeeded)
                    {
                        _logger.LogInformation("User {UserId} removed firm logo successfully", user.Id);
                        SuccessMessage = "Logo removed successfully!";
                    }
                    else
                    {
                        _logger.LogWarning("Failed to update user {UserId} after removing logo", user.Id);
                        ErrorMessage = "Failed to remove logo. Please try again.";
                    }
                }
                else
                {
                    _logger.LogInformation("User {UserId} attempted to remove logo but none exists", user.Id);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception occurred while removing logo for user {UserId}", user.Id);
                ErrorMessage = "An error occurred while removing the logo. Please try again.";
            }

            return RedirectToPage();
        }
    }
}
