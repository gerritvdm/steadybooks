using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SteadyBooks.Data;
using SteadyBooks.Models;

namespace SteadyBooks.Pages.Account
{
    [Authorize]
    public class SettingsModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly ILogger<SettingsModel> _logger;

        public SettingsModel(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            ILogger<SettingsModel> logger)
        {
            _context = context;
            _userManager = userManager;
            _signInManager = signInManager;
            _logger = logger;
        }

        public PlanInfo CurrentPlan { get; set; } = new();
        public UsageStatistics UsageStats { get; set; } = new();
        public string UserEmail { get; set; } = string.Empty;
        public string FirmName { get; set; } = string.Empty;
        public string PrimaryContactEmail { get; set; } = string.Empty;
        public DateTime AccountCreatedDate { get; set; }
        public DateTime TrialEndsDate { get; set; }
        public int DaysRemainingInTrial { get; set; }

        [TempData]
        public string? SuccessMessage { get; set; }

        [TempData]
        public string? ErrorMessage { get; set; }

        public class PlanInfo
        {
            public string Name { get; set; } = "Free Trial";
            public string Description { get; set; } = "Try all features risk-free for 30 days";
            public decimal Price { get; set; } = 0;
            public int MaxDashboards { get; set; } = 3;
        }

        public class UsageStatistics
        {
            public int TotalDashboards { get; set; }
            public int ActiveDashboards { get; set; }
            public int DraftDashboards { get; set; }
            public int ArchivedDashboards { get; set; }
            public int ClientViewsLast30Days { get; set; }
            public decimal DashboardUsagePercentage { get; set; }
        }

        public async Task<IActionResult> OnGetAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound("User not found.");
            }

            try
            {
                // Load user information
                UserEmail = user.Email ?? string.Empty;
                FirmName = user.FirmName ?? string.Empty;
                PrimaryContactEmail = user.PrimaryContactEmail ?? string.Empty;
                AccountCreatedDate = user.CreatedDate;

                // Calculate trial info
                TrialEndsDate = AccountCreatedDate.AddDays(30);
                DaysRemainingInTrial = Math.Max(0, (TrialEndsDate - DateTime.UtcNow).Days);

                // Set current plan (hardcoded for MVP - will be dynamic with Stripe)
                CurrentPlan = new PlanInfo
                {
                    Name = "Free Trial",
                    Description = "Try all features risk-free for 30 days",
                    Price = 0,
                    MaxDashboards = 3
                };

                // Load usage statistics
                var dashboards = await _context.ClientDashboards
                    .Where(d => d.FirmId == user.Id)
                    .ToListAsync();

                UsageStats = new UsageStatistics
                {
                    TotalDashboards = dashboards.Count,
                    ActiveDashboards = dashboards.Count(d => d.Status == DashboardStatus.Active),
                    DraftDashboards = dashboards.Count(d => d.Status == DashboardStatus.Draft),
                    ArchivedDashboards = dashboards.Count(d => d.Status == DashboardStatus.Archived),
                    ClientViewsLast30Days = dashboards.Count(d => 
                        d.LastAccessedDate != null && 
                        d.LastAccessedDate.Value >= DateTime.UtcNow.AddDays(-30)),
                    DashboardUsagePercentage = CurrentPlan.MaxDashboards > 0 
                        ? Math.Min(100, (decimal)dashboards.Count(d => d.Status == DashboardStatus.Active) / CurrentPlan.MaxDashboards * 100)
                        : 0
                };

                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading account settings for user {UserId}", user.Id);
                ErrorMessage = "An error occurred while loading your account settings.";
                return Page();
            }
        }

        public async Task<IActionResult> OnPostDeleteAccountAsync(string confirmText)
        {
            if (confirmText != "DELETE")
            {
                ErrorMessage = "Account deletion confirmation failed. Please type 'DELETE' exactly.";
                return RedirectToPage();
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound("User not found.");
            }

            try
            {
                // Delete all dashboards and configurations (cascade delete will handle it)
                var dashboards = await _context.ClientDashboards
                    .Where(d => d.FirmId == user.Id)
                    .ToListAsync();

                _context.ClientDashboards.RemoveRange(dashboards);
                await _context.SaveChangesAsync();

                // Delete user account
                var result = await _userManager.DeleteAsync(user);
                if (result.Succeeded)
                {
                    _logger.LogWarning("User {UserId} ({Email}) deleted their account", user.Id, user.Email);
                    
                    await _signInManager.SignOutAsync();
                    
                    return RedirectToPage("/Index", new { deleted = true });
                }

                foreach (var error in result.Errors)
                {
                    _logger.LogError("Error deleting account for user {UserId}: {Error}", user.Id, error.Description);
                    ErrorMessage = error.Description;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception occurred while deleting account for user {UserId}", user.Id);
                ErrorMessage = "An error occurred while deleting your account. Please try again or contact support.";
            }

            return RedirectToPage();
        }
    }
}
