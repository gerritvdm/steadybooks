using Microsoft.Extensions.Options;
using SteadyBooks.Models;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace SteadyBooks.Services;

public interface IQuickBooksOAuthService
{
    string GetAuthorizationUrl(int dashboardId, string state);
    Task<QuickBooksTokenResponse> ExchangeCodeForTokensAsync(string code, string realmId);
    Task<QuickBooksTokenResponse> RefreshTokenAsync(string refreshToken);
    Task<bool> ValidateTokenAsync(string accessToken, string realmId);
}

public class QuickBooksOAuthService : IQuickBooksOAuthService
{
    private readonly QuickBooksSettings _settings;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<QuickBooksOAuthService> _logger;

    public QuickBooksOAuthService(
        IOptions<QuickBooksSettings> settings,
        IHttpClientFactory httpClientFactory,
        ILogger<QuickBooksOAuthService> logger)
    {
        _settings = settings.Value;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public string GetAuthorizationUrl(int dashboardId, string state)
    {
        var authUrl = $"{_settings.AuthorizationEndpoint}" +
            $"?client_id={Uri.EscapeDataString(_settings.ClientId)}" +
            $"&scope={Uri.EscapeDataString(_settings.Scopes)}" +
            $"&redirect_uri={Uri.EscapeDataString(_settings.RedirectUri)}" +
            $"&response_type=code" +
            $"&state={Uri.EscapeDataString($"{dashboardId}:{state}")}";

        _logger.LogInformation("Generated QuickBooks authorization URL for dashboard {DashboardId}", dashboardId);
        
        return authUrl;
    }

    public async Task<QuickBooksTokenResponse> ExchangeCodeForTokensAsync(string code, string realmId)
    {
        try
        {
            var client = _httpClientFactory.CreateClient();
            
            var authHeader = Convert.ToBase64String(
                Encoding.UTF8.GetBytes($"{_settings.ClientId}:{_settings.ClientSecret}"));
            
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", authHeader);
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            var content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                { "grant_type", "authorization_code" },
                { "code", code },
                { "redirect_uri", _settings.RedirectUri }
            });

            var response = await client.PostAsync(_settings.TokenEndpoint, content);
            var responseBody = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Token exchange failed: {StatusCode} - {Response}", 
                    response.StatusCode, responseBody);
                throw new Exception($"Token exchange failed: {response.StatusCode}");
            }

            var tokenResponse = JsonSerializer.Deserialize<QuickBooksTokenResponse>(responseBody, 
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (tokenResponse == null)
            {
                throw new Exception("Failed to deserialize token response");
            }

            _logger.LogInformation("Successfully exchanged code for tokens for RealmId {RealmId}", realmId);
            
            return tokenResponse;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exchanging authorization code for tokens");
            throw;
        }
    }

    public async Task<QuickBooksTokenResponse> RefreshTokenAsync(string refreshToken)
    {
        try
        {
            var client = _httpClientFactory.CreateClient();
            
            var authHeader = Convert.ToBase64String(
                Encoding.UTF8.GetBytes($"{_settings.ClientId}:{_settings.ClientSecret}"));
            
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", authHeader);
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            var content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                { "grant_type", "refresh_token" },
                { "refresh_token", refreshToken }
            });

            var response = await client.PostAsync(_settings.TokenEndpoint, content);
            var responseBody = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Token refresh failed: {StatusCode} - {Response}", 
                    response.StatusCode, responseBody);
                throw new Exception($"Token refresh failed: {response.StatusCode}");
            }

            var tokenResponse = JsonSerializer.Deserialize<QuickBooksTokenResponse>(responseBody,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (tokenResponse == null)
            {
                throw new Exception("Failed to deserialize token response");
            }

            _logger.LogInformation("Successfully refreshed QuickBooks tokens");
            
            return tokenResponse;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error refreshing QuickBooks token");
            throw;
        }
    }

    public async Task<bool> ValidateTokenAsync(string accessToken, string realmId)
    {
        try
        {
            var client = _httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            var url = $"{_settings.ApiBaseUrl}/v3/company/{realmId}/companyinfo/{realmId}";
            var response = await client.GetAsync(url);

            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating QuickBooks token");
            return false;
        }
    }
}

public class QuickBooksTokenResponse
{
    public string Access_Token { get; set; } = string.Empty;
    public string Refresh_Token { get; set; } = string.Empty;
    public string Token_Type { get; set; } = string.Empty;
    public int Expires_In { get; set; }
    public int X_Refresh_Token_Expires_In { get; set; }
}
