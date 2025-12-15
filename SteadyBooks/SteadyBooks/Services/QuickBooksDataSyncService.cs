using Microsoft.EntityFrameworkCore;
using SteadyBooks.Data;
using SteadyBooks.Models;

namespace SteadyBooks.Services;

public interface IQuickBooksDataSyncService
{
    Task<DashboardFinancialData?> SyncDashboardDataAsync(int dashboardId);
    Task<bool> RefreshTokenIfNeededAsync(QuickBooksConnection connection);
}

public class QuickBooksDataSyncService : IQuickBooksDataSyncService
{
    private readonly ApplicationDbContext _context;
    private readonly IQuickBooksApiService _apiService;
    private readonly IQuickBooksOAuthService _oauthService;
    private readonly ILogger<QuickBooksDataSyncService> _logger;

    public QuickBooksDataSyncService(
        ApplicationDbContext context,
        IQuickBooksApiService apiService,
        IQuickBooksOAuthService oauthService,
        ILogger<QuickBooksDataSyncService> logger)
    {
        _context = context;
        _apiService = apiService;
        _oauthService = oauthService;
        _logger = logger;
    }

    public async Task<DashboardFinancialData?> SyncDashboardDataAsync(int dashboardId)
    {
        try
        {
            var dashboard = await _context.ClientDashboards
                .Include(d => d.Configuration)
                .Include(d => d.QuickBooksConnection)
                .FirstOrDefaultAsync(d => d.Id == dashboardId);

            if (dashboard == null)
            {
                _logger.LogWarning("Dashboard {DashboardId} not found", dashboardId);
                return null;
            }

            if (dashboard.QuickBooksConnection == null || !dashboard.QuickBooksConnection.IsActive)
            {
                _logger.LogWarning("Dashboard {DashboardId} has no active QuickBooks connection", dashboardId);
                return null;
            }

            var connection = dashboard.QuickBooksConnection;

            // Refresh token if needed
            await RefreshTokenIfNeededAsync(connection);

            var config = dashboard.Configuration ?? new DashboardConfiguration();
            var dateRange = GetDateRange(config.DateRange, config.CustomStartDate, config.CustomEndDate);

            _logger.LogInformation("Syncing data for dashboard {DashboardId}, date range: {Start} to {End}", 
                dashboardId, dateRange.Start, dateRange.End);

            // Fetch all financial data in parallel
            var cashTask = config.ShowCashBalance 
                ? _apiService.GetCashBalanceAsync(connection.AccessToken, connection.RealmId, dateRange.End)
                : Task.FromResult(0m);

            var plTask = config.ShowProfit 
                ? _apiService.GetProfitLossAsync(connection.AccessToken, connection.RealmId, dateRange.Start, dateRange.End)
                : Task.FromResult(new ProfitLossData());

            var taxTask = config.ShowTaxesDue 
                ? _apiService.GetTaxLiabilityAsync(connection.AccessToken, connection.RealmId)
                : Task.FromResult(0m);

            var invoicesTask = config.ShowOutstandingInvoices 
                ? _apiService.GetOutstandingInvoicesAsync(connection.AccessToken, connection.RealmId)
                : Task.FromResult(0m);

            await Task.WhenAll(cashTask, plTask, taxTask, invoicesTask);

            var plData = await plTask;

            var financialData = new DashboardFinancialData
            {
                CashBalance = await cashTask,
                Revenue = plData.Revenue,
                Expenses = plData.Expenses,
                Profit = plData.Profit,
                TaxesDue = await taxTask,
                OutstandingInvoices = await invoicesTask,
                SyncDate = DateTime.UtcNow
            };

            // Update last sync date
            connection.LastSyncDate = DateTime.UtcNow;
            connection.Status = ConnectionStatus.Connected;
            connection.LastError = null;
            await _context.SaveChangesAsync();

            _logger.LogInformation("Successfully synced data for dashboard {DashboardId}", dashboardId);

            return financialData;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error syncing data for dashboard {DashboardId}", dashboardId);
            
            // Update connection status
            var connection = await _context.QuickBooksConnections
                .FirstOrDefaultAsync(c => c.ClientDashboardId == dashboardId);
            
            if (connection != null)
            {
                connection.Status = ConnectionStatus.Error;
                connection.LastError = ex.Message;
                await _context.SaveChangesAsync();
            }

            return null;
        }
    }

    public async Task<bool> RefreshTokenIfNeededAsync(QuickBooksConnection connection)
    {
        // Check if access token is expired or expiring soon (within 5 minutes)
        if (connection.AccessTokenExpiresAt > DateTime.UtcNow.AddMinutes(5))
        {
            return true; // Token still valid
        }

        try
        {
            _logger.LogInformation("Refreshing access token for connection {ConnectionId}", connection.Id);

            var tokenResponse = await _oauthService.RefreshTokenAsync(connection.RefreshToken);

            connection.AccessToken = tokenResponse.Access_Token;
            connection.RefreshToken = tokenResponse.Refresh_Token;
            connection.AccessTokenExpiresAt = DateTime.UtcNow.AddSeconds(tokenResponse.Expires_In);
            connection.RefreshTokenExpiresAt = DateTime.UtcNow.AddSeconds(tokenResponse.X_Refresh_Token_Expires_In);
            connection.ModifiedDate = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Successfully refreshed access token for connection {ConnectionId}", connection.Id);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error refreshing token for connection {ConnectionId}", connection.Id);
            
            connection.Status = ConnectionStatus.Expired;
            connection.LastError = "Token refresh failed. Please reconnect.";
            await _context.SaveChangesAsync();

            return false;
        }
    }

    private (DateTime Start, DateTime End) GetDateRange(DateRangeType dateRange, DateTime? customStart, DateTime? customEnd)
    {
        var now = DateTime.Now;
        
        return dateRange switch
        {
            DateRangeType.ThisMonth => (new DateTime(now.Year, now.Month, 1), now),
            DateRangeType.LastMonth => (new DateTime(now.Year, now.Month, 1).AddMonths(-1), new DateTime(now.Year, now.Month, 1).AddDays(-1)),
            DateRangeType.YearToDate => (new DateTime(now.Year, 1, 1), now),
            DateRangeType.Custom when customStart.HasValue && customEnd.HasValue => (customStart.Value, customEnd.Value),
            _ => (new DateTime(now.Year, now.Month, 1), now)
        };
    }
}

public class DashboardFinancialData
{
    public decimal CashBalance { get; set; }
    public decimal Revenue { get; set; }
    public decimal Expenses { get; set; }
    public decimal Profit { get; set; }
    public decimal TaxesDue { get; set; }
    public decimal OutstandingInvoices { get; set; }
    public DateTime SyncDate { get; set; }
    
    public decimal Margin => Revenue > 0 ? Math.Round((Profit / Revenue) * 100, 1) : 0;
    public decimal ProfitChange => 0; // Calculate this by comparing with previous period
    public decimal CashChange => 0; // Calculate this by comparing with previous period
}
