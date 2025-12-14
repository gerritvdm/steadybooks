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
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<IndexModel> _logger;

        public IndexModel(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            ILogger<IndexModel> logger)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
        }

        public List<ClientDashboard> Dashboards { get; set; } = new();

        [BindProperty]
        public CreateDashboardInput CreateInput { get; set; } = new();

        [TempData]
        public string? SuccessMessage { get; set; }

        [TempData]
        public string? ErrorMessage { get; set; }

        public class CreateDashboardInput
        {
            [Required(ErrorMessage = "Dashboard name is required")]
            [StringLength(200, ErrorMessage = "Dashboard name cannot exceed 200 characters")]
            [Display(Name = "Dashboard Name")]
            public string DashboardName { get; set; } = string.Empty;

            [StringLength(200, ErrorMessage = "Company name cannot exceed 200 characters")]
            [Display(Name = "Client Company Name")]
            public string? ClientCompanyName { get; set; }
        }

        public async Task<IActionResult> OnGetAsync()
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    _logger.LogWarning("User not found when loading dashboards");
                    return NotFound("User not found.");
                }

                // Load all dashboards for this firm, ordered by most recent first
                Dashboards = await _context.ClientDashboards
                    .Where(d => d.FirmId == user.Id)
                    .OrderByDescending(d => d.CreatedDate)
                    .ToListAsync();

                _logger.LogInformation("Loaded {Count} dashboards for user {UserId}", Dashboards.Count, user.Id);

                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading dashboards");
                ErrorMessage = "An error occurred while loading your dashboards.";
                Dashboards = new List<ClientDashboard>();
                return Page();
            }
        }

        public async Task<IActionResult> OnPostCreateAsync()
        {
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Invalid model state when creating dashboard");
                await OnGetAsync(); // Reload dashboards
                return Page();
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                _logger.LogWarning("User not found when creating dashboard");
                return NotFound("User not found.");
            }

            try
            {
                var dashboard = new ClientDashboard
                {
                    FirmId = user.Id,
                    DashboardName = CreateInput.DashboardName,
                    ClientCompanyName = CreateInput.ClientCompanyName,
                    AccessLink = Guid.NewGuid().ToString(), // Generate unique access link
                    Status = DashboardStatus.Draft,
                    IsActive = true,
                    CreatedDate = DateTime.UtcNow,
                    ModifiedDate = DateTime.UtcNow
                };

                _context.ClientDashboards.Add(dashboard);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Dashboard {DashboardId} created by user {UserId}", dashboard.Id, user.Id);

                SuccessMessage = $"Dashboard '{dashboard.DashboardName}' created successfully!";
                return RedirectToPage();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating dashboard for user {UserId}", user.Id);
                ErrorMessage = "An error occurred while creating the dashboard. Please try again.";
                await OnGetAsync(); // Reload dashboards
                return Page();
            }
        }

        public async Task<IActionResult> OnPostArchiveAsync(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                _logger.LogWarning("User not found when archiving dashboard");
                return NotFound("User not found.");
            }

            try
            {
                var dashboard = await _context.ClientDashboards
                    .FirstOrDefaultAsync(d => d.Id == id && d.FirmId == user.Id);

                if (dashboard == null)
                {
                    _logger.LogWarning("Dashboard {DashboardId} not found for user {UserId}", id, user.Id);
                    ErrorMessage = "Dashboard not found.";
                    return RedirectToPage();
                }

                dashboard.Status = DashboardStatus.Archived;
                dashboard.IsActive = false;
                dashboard.ModifiedDate = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Dashboard {DashboardId} archived by user {UserId}", id, user.Id);

                SuccessMessage = $"Dashboard '{dashboard.DashboardName}' has been archived.";
                return RedirectToPage();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error archiving dashboard {DashboardId}", id);
                ErrorMessage = "An error occurred while archiving the dashboard.";
                return RedirectToPage();
            }
        }
    }
}
