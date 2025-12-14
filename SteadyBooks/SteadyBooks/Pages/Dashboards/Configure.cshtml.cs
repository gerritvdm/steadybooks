using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SteadyBooks.Data;
using SteadyBooks.Models;
using System.ComponentModel.DataAnnotations;

namespace SteadyBooks.Pages.Dashboards
{
    [Authorize]
    public class ConfigureModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<ConfigureModel> _logger;

        public ConfigureModel(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            ILogger<ConfigureModel> logger)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
        }

        public ClientDashboard? Dashboard { get; set; }

        [BindProperty]
        public ConfigurationInput Input { get; set; } = new();

        [TempData]
        public string? SuccessMessage { get; set; }

        [TempData]
        public string? ErrorMessage { get; set; }

        public class ConfigurationInput
        {
            [Required]
            [StringLength(200)]
            [Display(Name = "Dashboard Name")]
            public string DashboardName { get; set; } = string.Empty;

            [StringLength(200)]
            [Display(Name = "Client Company Name")]
            public string? ClientCompanyName { get; set; }

            [Required]
            [Display(Name = "Date Range")]
            public DateRangeType DateRange { get; set; } = DateRangeType.ThisMonth;

            [Display(Name = "Show Cash Balance")]
            public bool ShowCashBalance { get; set; } = true;

            [Display(Name = "Show Profit")]
            public bool ShowProfit { get; set; } = true;

            [Display(Name = "Show Taxes Due")]
            public bool ShowTaxesDue { get; set; } = true;

            [Display(Name = "Show Outstanding Invoices")]
            public bool ShowOutstandingInvoices { get; set; } = true;

            [StringLength(200)]
            [Display(Name = "Custom Title")]
            public string? CustomTitle { get; set; }

            [StringLength(500)]
            [Display(Name = "Welcome Message")]
            public string? WelcomeMessage { get; set; }
        }

        public async Task<IActionResult> OnGetAsync(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound("User not found.");
            }

            try
            {
                // Load dashboard with configuration
                Dashboard = await _context.ClientDashboards
                    .Include(d => d.Configuration)
                    .FirstOrDefaultAsync(d => d.Id == id && d.FirmId == user.Id);

                if (Dashboard == null)
                {
                    _logger.LogWarning("Dashboard {DashboardId} not found for user {UserId}", id, user.Id);
                    ErrorMessage = "Dashboard not found.";
                    return RedirectToPage("/Dashboards/Index");
                }

                // Populate input model from dashboard and configuration
                Input = new ConfigurationInput
                {
                    DashboardName = Dashboard.DashboardName,
                    ClientCompanyName = Dashboard.ClientCompanyName
                };

                if (Dashboard.Configuration != null)
                {
                    Input.DateRange = Dashboard.Configuration.DateRange;
                    Input.ShowCashBalance = Dashboard.Configuration.ShowCashBalance;
                    Input.ShowProfit = Dashboard.Configuration.ShowProfit;
                    Input.ShowTaxesDue = Dashboard.Configuration.ShowTaxesDue;
                    Input.ShowOutstandingInvoices = Dashboard.Configuration.ShowOutstandingInvoices;
                    Input.CustomTitle = Dashboard.Configuration.CustomTitle;
                    Input.WelcomeMessage = Dashboard.Configuration.WelcomeMessage;
                }

                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading dashboard {DashboardId} for configuration", id);
                ErrorMessage = "An error occurred while loading the dashboard.";
                return RedirectToPage("/Dashboards/Index");
            }
        }

        public async Task<IActionResult> OnPostAsync(int id)
        {
            if (!ModelState.IsValid)
            {
                await OnGetAsync(id);
                return Page();
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound("User not found.");
            }

            try
            {
                // Load dashboard with configuration
                Dashboard = await _context.ClientDashboards
                    .Include(d => d.Configuration)
                    .FirstOrDefaultAsync(d => d.Id == id && d.FirmId == user.Id);

                if (Dashboard == null)
                {
                    _logger.LogWarning("Dashboard {DashboardId} not found for user {UserId}", id, user.Id);
                    ErrorMessage = "Dashboard not found.";
                    return RedirectToPage("/Dashboards/Index");
                }

                // Update dashboard basic info
                Dashboard.DashboardName = Input.DashboardName;
                Dashboard.ClientCompanyName = Input.ClientCompanyName;
                Dashboard.ModifiedDate = DateTime.UtcNow;

                // Update or create configuration
                if (Dashboard.Configuration == null)
                {
                    Dashboard.Configuration = new DashboardConfiguration
                    {
                        ClientDashboardId = Dashboard.Id,
                        CreatedDate = DateTime.UtcNow
                    };
                    _context.DashboardConfigurations.Add(Dashboard.Configuration);
                }

                // Update configuration settings
                Dashboard.Configuration.DateRange = Input.DateRange;
                Dashboard.Configuration.ShowCashBalance = Input.ShowCashBalance;
                Dashboard.Configuration.ShowProfit = Input.ShowProfit;
                Dashboard.Configuration.ShowTaxesDue = Input.ShowTaxesDue;
                Dashboard.Configuration.ShowOutstandingInvoices = Input.ShowOutstandingInvoices;
                Dashboard.Configuration.CustomTitle = Input.CustomTitle;
                Dashboard.Configuration.WelcomeMessage = Input.WelcomeMessage;
                Dashboard.Configuration.ModifiedDate = DateTime.UtcNow;

                // Update dashboard status to Active if it was Draft
                if (Dashboard.Status == DashboardStatus.Draft)
                {
                    Dashboard.Status = DashboardStatus.Active;
                }

                await _context.SaveChangesAsync();

                _logger.LogInformation("Dashboard {DashboardId} configuration updated by user {UserId}", 
                    Dashboard.Id, user.Id);

                SuccessMessage = "Dashboard configuration saved successfully!";
                return RedirectToPage(new { id = Dashboard.Id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving configuration for dashboard {DashboardId}", id);
                ErrorMessage = "An error occurred while saving the configuration. Please try again.";
                await OnGetAsync(id);
                return Page();
            }
        }
    }
}
