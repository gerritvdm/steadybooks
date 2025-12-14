using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SteadyBooks.Data;
using SteadyBooks.Models;

namespace SteadyBooks.Pages.Dashboard
{
    public class ViewModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ViewModel> _logger;

        public ViewModel(ApplicationDbContext context, ILogger<ViewModel> logger)
        {
            _context = context;
            _logger = logger;
        }

        public ClientDashboard? Dashboard { get; set; }

        public async Task<IActionResult> OnGetAsync(string accessLink)
        {
            if (string.IsNullOrEmpty(accessLink))
            {
                _logger.LogWarning("Access link is null or empty");
                return NotFound();
            }

            try
            {
                // Load dashboard with firm details (for branding)
                Dashboard = await _context.ClientDashboards
                    .Include(d => d.Firm)
                    .FirstOrDefaultAsync(d => d.AccessLink == accessLink);

                if (Dashboard == null)
                {
                    _logger.LogWarning("Dashboard not found for access link {AccessLink}", accessLink);
                    return Page(); // Will show "not found" message
                }

                // Update last accessed date
                Dashboard.LastAccessedDate = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                _logger.LogInformation("Dashboard {DashboardId} accessed via link {AccessLink}", 
                    Dashboard.Id, accessLink);

                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading dashboard with access link {AccessLink}", accessLink);
                return Page();
            }
        }
    }
}
