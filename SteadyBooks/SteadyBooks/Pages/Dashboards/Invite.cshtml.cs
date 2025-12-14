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
    public class InviteModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<InviteModel> _logger;

        public InviteModel(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            ILogger<InviteModel> logger)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
        }

        public ClientDashboard? Dashboard { get; set; }
        public string? FirmName { get; set; }
        public string? FirmEmail { get; set; }

        [BindProperty]
        public EmailInviteInput EmailInput { get; set; } = new();

        [TempData]
        public string? SuccessMessage { get; set; }

        [TempData]
        public string? ErrorMessage { get; set; }

        public class EmailInviteInput
        {
            [Required(ErrorMessage = "Client email is required")]
            [EmailAddress(ErrorMessage = "Please enter a valid email address")]
            [Display(Name = "Client Email")]
            public string RecipientEmail { get; set; } = string.Empty;

            [StringLength(100)]
            [Display(Name = "Client Name")]
            public string? RecipientName { get; set; }

            [StringLength(500)]
            [Display(Name = "Custom Message")]
            public string? CustomMessage { get; set; }
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
                Dashboard = await _context.ClientDashboards
                    .Include(d => d.Firm)
                    .FirstOrDefaultAsync(d => d.Id == id && d.FirmId == user.Id);

                if (Dashboard == null)
                {
                    _logger.LogWarning("Dashboard {DashboardId} not found for user {UserId}", id, user.Id);
                    ErrorMessage = "Dashboard not found.";
                    return RedirectToPage("/Dashboards/Index");
                }

                FirmName = Dashboard.Firm.FirmName;
                FirmEmail = Dashboard.Firm.PrimaryContactEmail;

                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading dashboard {DashboardId} for invite", id);
                ErrorMessage = "An error occurred while loading the dashboard.";
                return RedirectToPage("/Dashboards/Index");
            }
        }

        public async Task<IActionResult> OnPostSendEmailAsync(int id)
        {
            // Email functionality stub - will implement with SendGrid/SMTP later
            await OnGetAsync(id);
            
            if (!ModelState.IsValid)
            {
                return Page();
            }

            // TODO: Implement email sending
            // For now, just show a message
            _logger.LogInformation("Email invitation requested for dashboard {DashboardId} to {Email}", 
                id, EmailInput.RecipientEmail);

            SuccessMessage = "Email functionality is coming soon. Please copy the link and send it manually.";
            return RedirectToPage(new { id });
        }

        public async Task<IActionResult> OnPostRegenerateLinkAsync(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound("User not found.");
            }

            try
            {
                Dashboard = await _context.ClientDashboards
                    .FirstOrDefaultAsync(d => d.Id == id && d.FirmId == user.Id);

                if (Dashboard == null)
                {
                    _logger.LogWarning("Dashboard {DashboardId} not found for user {UserId}", id, user.Id);
                    ErrorMessage = "Dashboard not found.";
                    return RedirectToPage("/Dashboards/Index");
                }

                var oldLink = Dashboard.AccessLink;

                // Generate new unique access link
                Dashboard.AccessLink = Guid.NewGuid().ToString();
                Dashboard.ModifiedDate = DateTime.UtcNow;
                Dashboard.LastAccessedDate = null; // Reset access tracking

                await _context.SaveChangesAsync();

                _logger.LogInformation("Access link regenerated for dashboard {DashboardId}. Old: {OldLink}, New: {NewLink}", 
                    id, oldLink, Dashboard.AccessLink);

                SuccessMessage = "Access link regenerated successfully! The old link will no longer work.";
                return RedirectToPage(new { id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error regenerating link for dashboard {DashboardId}", id);
                ErrorMessage = "An error occurred while regenerating the link.";
                return RedirectToPage(new { id });
            }
        }

        public async Task<IActionResult> OnPostRevokeAccessAsync(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound("User not found.");
            }

            try
            {
                Dashboard = await _context.ClientDashboards
                    .FirstOrDefaultAsync(d => d.Id == id && d.FirmId == user.Id);

                if (Dashboard == null)
                {
                    _logger.LogWarning("Dashboard {DashboardId} not found for user {UserId}", id, user.Id);
                    ErrorMessage = "Dashboard not found.";
                    return RedirectToPage("/Dashboards/Index");
                }

                // Archive the dashboard and deactivate
                Dashboard.Status = DashboardStatus.Archived;
                Dashboard.IsActive = false;
                Dashboard.ModifiedDate = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Access revoked for dashboard {DashboardId} by user {UserId}", 
                    id, user.Id);

                SuccessMessage = $"Access revoked and dashboard '{Dashboard.DashboardName}' has been archived.";
                return RedirectToPage("/Dashboards/Index");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error revoking access for dashboard {DashboardId}", id);
                ErrorMessage = "An error occurred while revoking access.";
                return RedirectToPage(new { id });
            }
        }
    }
}
