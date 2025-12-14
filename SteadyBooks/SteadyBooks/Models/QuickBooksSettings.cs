namespace SteadyBooks.Models;

public class QuickBooksSettings
{
    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;
    public string Environment { get; set; } = "Sandbox"; // "Sandbox" or "Production"
    public string RedirectUri { get; set; } = string.Empty;
    public string Scopes { get; set; } = "com.intuit.quickbooks.accounting";
    
    // Computed properties
    public string AuthorizationEndpoint => Environment == "Production"
        ? "https://appcenter.intuit.com/connect/oauth2"
        : "https://appcenter.intuit.com/connect/oauth2";
    
    public string TokenEndpoint => Environment == "Production"
        ? "https://oauth.platform.intuit.com/oauth2/v1/tokens/bearer"
        : "https://oauth.platform.intuit.com/oauth2/v1/tokens/bearer";
    
    public string ApiBaseUrl => Environment == "Production"
        ? "https://quickbooks.api.intuit.com"
        : "https://sandbox-quickbooks.api.intuit.com";
}
