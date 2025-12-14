using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SteadyBooks.Data;
using SteadyBooks.Models;
using SteadyBooks.Services;
using System.ComponentModel.DataAnnotations;

namespace SteadyBooks.Pages.Dashboards
{
    [Authorize]
    public class ConfigureModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IQuickBooksOAuthService _oauthService;
        private readonly ILogger<ConfigureModel> _logger;

        public ConfigureModel(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            IQuickBooksOAuthService oauthService,
            ILogger<ConfigureModel> logger)
        {
            _context = context;
            _userManager = userManager;
            _oauthService = oauthService;
            _logger = logger;
        }

        public ClientDashboard? Dashboard { get; set; }
        public QuickBooksConnection? QuickBooksConnection { get; set; }

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
                // Load dashboard with configuration and QuickBooks connection
                Dashboard = await _context.ClientDashboards
                    .Include(d => d.Configuration)
                    .Include(d => d.QuickBooksConnection)
                    .FirstOrDefaultAsync(d => d.Id == id && d.FirmId == user.Id);

                if (Dashboard == null)
                {
                    _logger.LogWarning("Dashboard {DashboardId} not found for user {UserId}", id, user.Id);
                    ErrorMessage = "Dashboard not found.";
                    return RedirectToPage("/Dashboards/Index");
                }

                QuickBooksConnection = Dashboard.QuickBooksConnection;

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

        public async Task<IActionResult> OnPostConnectQuickBooksAsync(int id)
        {
            _logger.LogInformation("=== OnPostConnectQuickBooksAsync called with id={DashboardId} ===", id);
            
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                _logger.LogWarning("User not found");
                return NotFound("User not found.");
            }

            _logger.LogInformation("User found: {UserId}", user.Id);

            try
            {
                var dashboard = await _context.ClientDashboards
                    .FirstOrDefaultAsync(d => d.Id == id && d.FirmId == user.Id);

                if (dashboard == null)
                {
                    _logger.LogWarning("Dashboard {DashboardId} not found for user {UserId}", id, user.Id);
                    ErrorMessage = "Dashboard not found.";
                    return RedirectToPage("/Dashboards/Index");
                }

                _logger.LogInformation("Dashboard found: {DashboardId}", dashboard.Id);

                // Generate random state for OAuth security
                var state = Guid.NewGuid().ToString();
                
                // Get authorization URL
                var authUrl = _oauthService.GetAuthorizationUrl(id, state);

                _logger.LogInformation("Generated auth URL: {AuthUrl}", authUrl);
                _logger.LogInformation("Redirecting to QuickBooks OAuth for dashboard {DashboardId}", id);

                // Redirect to QuickBooks authorization
                return Redirect(authUrl);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initiating QuickBooks connection for dashboard {DashboardId}", id);
                ErrorMessage = "An error occurred while connecting to QuickBooks. Please try again.";
                return RedirectToPage(new { id });
            }
        }

        public async Task<IActionResult> OnPostDisconnectQuickBooksAsync(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound("User not found.");
            }

            try
            {
                var dashboard = await _context.ClientDashboards
                    .Include(d => d.QuickBooksConnection)
                    .FirstOrDefaultAsync(d => d.Id == id && d.FirmId == user.Id);

                if (dashboard == null)
                {
                    _logger.LogWarning("Dashboard {DashboardId} not found for user {UserId}", id, user.Id);
                    ErrorMessage = "Dashboard not found.";
                    return RedirectToPage("/Dashboards/Index");
                }

                if (dashboard.QuickBooksConnection != null)
                {
                    _context.QuickBooksConnections.Remove(dashboard.QuickBooksConnection);
                    await _context.SaveChangesAsync();

                    _logger.LogInformation("QuickBooks disconnected from dashboard {DashboardId}", id);
                    SuccessMessage = "QuickBooks has been disconnected. Dashboard will now use mock data.";
                }

                return RedirectToPage(new { id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error disconnecting QuickBooks from dashboard {DashboardId}", id);
                ErrorMessage = "An error occurred while disconnecting QuickBooks.";
                return RedirectToPage(new { id });
            }
        }
    }
}
