using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SteadyBooks.Data;
using SteadyBooks.Models;
using SteadyBooks.Services;
using System.Net.Http.Headers;
using System.Text.Json;

namespace SteadyBooks.Pages.QuickBooks;

public class CallbackModel : PageModel
{
    private readonly ApplicationDbContext _context;
    private readonly IQuickBooksOAuthService _oauthService;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<CallbackModel> _logger;

    public CallbackModel(
        ApplicationDbContext context,
        IQuickBooksOAuthService oauthService,
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ILogger<CallbackModel> logger)
    {
        _context = context;
        _oauthService = oauthService;
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
        _logger = logger;
    }

    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public string? CompanyName { get; set; }
    public int DashboardId { get; set; }

    public async Task<IActionResult> OnGetAsync(string? code, string? state, string? realmId, string? error)
    {
        try
        {
            // Check for OAuth error
            if (!string.IsNullOrEmpty(error))
            {
                _logger.LogWarning("OAuth error received: {Error}", error);
                Success = false;
                ErrorMessage = error == "access_denied" 
                    ? "You denied access to QuickBooks. Please try again and authorize the connection."
                    : $"OAuth error: {error}";
                return Page();
            }

            // Validate required parameters
            if (string.IsNullOrEmpty(code) || string.IsNullOrEmpty(state) || string.IsNullOrEmpty(realmId))
            {
                _logger.LogWarning("Missing required OAuth parameters");
                Success = false;
                ErrorMessage = "Missing required connection parameters. Please try again.";
                return Page();
            }

            // Parse state (format: "dashboardId:randomState")
            var stateParts = state.Split(':');
            if (stateParts.Length != 2 || !int.TryParse(stateParts[0], out int dashboardId))
            {
                _logger.LogWarning("Invalid state parameter: {State}", state);
                Success = false;
                ErrorMessage = "Invalid connection state. Please try again.";
                return Page();
            }

            DashboardId = dashboardId;

            // Load dashboard
            var dashboard = await _context.ClientDashboards
                .Include(d => d.QuickBooksConnection)
                .FirstOrDefaultAsync(d => d.Id == dashboardId);

            if (dashboard == null)
            {
                _logger.LogWarning("Dashboard {DashboardId} not found", dashboardId);
                Success = false;
                ErrorMessage = "Dashboard not found.";
                return Page();
            }

            // Exchange code for tokens
            var tokenResponse = await _oauthService.ExchangeCodeForTokensAsync(code, realmId);

            // Get company info
            CompanyName = await GetCompanyNameAsync(tokenResponse.Access_Token, realmId);

            // Save or update connection
            if (dashboard.QuickBooksConnection == null)
            {
                dashboard.QuickBooksConnection = new QuickBooksConnection
                {
                    ClientDashboardId = dashboardId,
                    RealmId = realmId,
                    CompanyName = CompanyName ?? "Unknown Company",
                    AccessToken = tokenResponse.Access_Token,
                    RefreshToken = tokenResponse.Refresh_Token,
                    AccessTokenExpiresAt = DateTime.UtcNow.AddSeconds(tokenResponse.Expires_In),
                    RefreshTokenExpiresAt = DateTime.UtcNow.AddSeconds(tokenResponse.X_Refresh_Token_Expires_In),
                    IsActive = true,
                    Status = ConnectionStatus.Connected,
                    ConnectedDate = DateTime.UtcNow,
                    ModifiedDate = DateTime.UtcNow
                };
                _context.QuickBooksConnections.Add(dashboard.QuickBooksConnection);
            }
            else
            {
                dashboard.QuickBooksConnection.AccessToken = tokenResponse.Access_Token;
                dashboard.QuickBooksConnection.RefreshToken = tokenResponse.Refresh_Token;
                dashboard.QuickBooksConnection.AccessTokenExpiresAt = DateTime.UtcNow.AddSeconds(tokenResponse.Expires_In);
                dashboard.QuickBooksConnection.RefreshTokenExpiresAt = DateTime.UtcNow.AddSeconds(tokenResponse.X_Refresh_Token_Expires_In);
                dashboard.QuickBooksConnection.CompanyName = CompanyName ?? dashboard.QuickBooksConnection.CompanyName;
                dashboard.QuickBooksConnection.IsActive = true;
                dashboard.QuickBooksConnection.Status = ConnectionStatus.Connected;
                dashboard.QuickBooksConnection.LastError = null;
                dashboard.QuickBooksConnection.ModifiedDate = DateTime.UtcNow;
            }

            // Update dashboard status
            if (dashboard.Status == DashboardStatus.Draft || dashboard.Status == DashboardStatus.NeedsAttention)
            {
                dashboard.Status = DashboardStatus.Active;
            }
            dashboard.ModifiedDate = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation("QuickBooks connected successfully for dashboard {DashboardId}, Company: {CompanyName}, RealmId: {RealmId}",
                dashboardId, CompanyName, realmId);

            Success = true;
            return Page();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing QuickBooks OAuth callback");
            Success = false;
            ErrorMessage = "An error occurred while connecting to QuickBooks. Please try again.";
            return Page();
        }
    }

    private async Task<string?> GetCompanyNameAsync(string accessToken, string realmId)
    {
        try
        {
            var client = _httpClientFactory.CreateClient();
            var apiBaseUrl = _configuration["QuickBooks:ApiBaseUrl"];
            
            _logger.LogInformation("Fetching company info from: {Url}", $"{apiBaseUrl}/v3/company/{realmId}/companyinfo/{realmId}");
            
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            var url = $"{apiBaseUrl}/v3/company/{realmId}/companyinfo/{realmId}";
            var response = await client.GetAsync(url);

            _logger.LogInformation("Company info response status: {StatusCode}", response.StatusCode);

            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                _logger.LogInformation("Company info response: {Json}", json);
                
                var doc = JsonDocument.Parse(json);
                
                if (doc.RootElement.TryGetProperty("CompanyInfo", out var companyInfo))
                {
                    if (companyInfo.TryGetProperty("CompanyName", out var companyName))
                    {
                        var name = companyName.GetString();
                        _logger.LogInformation("Extracted company name: {CompanyName}", name);
                        return name;
                    }
                }
                
                _logger.LogWarning("Could not find CompanyInfo.CompanyName in response");
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("Failed to fetch company info. Status: {Status}, Response: {Response}", 
                    response.StatusCode, errorContent);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching company name from QuickBooks");
        }

        return null;
    }
}
