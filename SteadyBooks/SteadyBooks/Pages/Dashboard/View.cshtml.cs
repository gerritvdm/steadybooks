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
        public MockFinancialData MockData { get; set; } = new();
        public string DateRangeDisplay { get; set; } = string.Empty;

        public class MockFinancialData
        {
            public decimal CashBalance { get; set; }
            public decimal CashChange { get; set; }
            public decimal Profit { get; set; }
            public decimal ProfitChange { get; set; }
            public decimal TaxesDue { get; set; }
            public decimal OutstandingInvoices { get; set; }
            public int InvoiceCount { get; set; }
            public decimal Revenue { get; set; }
            public decimal Expenses { get; set; }
            public decimal Margin { get; set; }
            public int BankAccountCount { get; set; }
        }

        public async Task<IActionResult> OnGetAsync(string accessLink)
        {
            if (string.IsNullOrEmpty(accessLink))
            {
                _logger.LogWarning("Access link is null or empty");
                return NotFound();
            }

            try
            {
                // Load dashboard with firm details and configuration
                Dashboard = await _context.ClientDashboards
                    .Include(d => d.Firm)
                    .Include(d => d.Configuration)
                    .FirstOrDefaultAsync(d => d.AccessLink == accessLink);

                if (Dashboard == null)
                {
                    _logger.LogWarning("Dashboard not found for access link {AccessLink}", accessLink);
                    return Page(); // Will show "not found" message
                }

                // Update last accessed date
                Dashboard.LastAccessedDate = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                // Generate date range display
                DateRangeDisplay = GetDateRangeDisplay(Dashboard.Configuration?.DateRange ?? DateRangeType.ThisMonth);

                // Generate mock financial data
                MockData = GenerateMockData(Dashboard.Configuration?.DateRange ?? DateRangeType.ThisMonth);

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

        private string GetDateRangeDisplay(DateRangeType dateRange)
        {
            var now = DateTime.Now;
            return dateRange switch
            {
                DateRangeType.ThisMonth => $"{now:MMMM yyyy}",
                DateRangeType.LastMonth => $"{now.AddMonths(-1):MMMM yyyy}",
                DateRangeType.YearToDate => $"Jan - {now:MMM} {now.Year}",
                DateRangeType.Custom => "Custom Range",
                _ => $"{now:MMMM yyyy}"
            };
        }

        private MockFinancialData GenerateMockData(DateRangeType dateRange)
        {
            // Generate realistic mock data that varies by date range
            var random = new Random(Dashboard?.Id ?? 0); // Use dashboard ID as seed for consistency
            
            var baseRevenue = dateRange switch
            {
                DateRangeType.ThisMonth => 85000 + random.Next(-5000, 10000),
                DateRangeType.LastMonth => 82000 + random.Next(-5000, 10000),
                DateRangeType.YearToDate => 450000 + random.Next(-20000, 50000),
                _ => 85000
            };

            var baseExpenses = (decimal)(baseRevenue * (0.65 + random.NextDouble() * 0.1)); // 65-75% of revenue
            var profit = baseRevenue - baseExpenses;
            var margin = baseRevenue > 0 ? Math.Round((profit / baseRevenue) * 100, 1) : 0;

            return new MockFinancialData
            {
                // Cash Balance
                CashBalance = 45231 + random.Next(-5000, 15000),
                CashChange = (decimal)(5 + random.NextDouble() * 10), // 5-15% growth
                
                // Profit
                Profit = profit,
                ProfitChange = (decimal)(-5 + random.NextDouble() * 20), // -5% to +15%
                
                // Taxes Due (estimate ~25% of profit)
                TaxesDue = profit > 0 ? Math.Round(profit * 0.25m, 0) : 0,
                
                // Outstanding Invoices
                OutstandingInvoices = 12458 + random.Next(-2000, 8000),
                InvoiceCount = 5 + random.Next(0, 10),
                
                // Quick Stats
                Revenue = baseRevenue,
                Expenses = baseExpenses,
                Margin = margin,
                BankAccountCount = 2 + random.Next(0, 3)
            };
        }
    }
}
