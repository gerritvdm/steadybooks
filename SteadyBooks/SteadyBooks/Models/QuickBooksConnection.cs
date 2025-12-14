namespace SteadyBooks.Models;

public class QuickBooksConnection
{
    public int Id { get; set; }
    
    // Foreign key to ClientDashboard
    public int ClientDashboardId { get; set; }
    public ClientDashboard ClientDashboard { get; set; } = default!;
    
    // QuickBooks Company Info
    public string RealmId { get; set; } = default!; // QuickBooks Company ID
    public string CompanyName { get; set; } = default!;
    
    // OAuth Tokens (encrypted)
    public string AccessToken { get; set; } = default!;
    public string RefreshToken { get; set; } = default!;
    public DateTime AccessTokenExpiresAt { get; set; }
    public DateTime RefreshTokenExpiresAt { get; set; }
    
    // Connection Status
    public bool IsActive { get; set; } = true;
    public ConnectionStatus Status { get; set; } = ConnectionStatus.Connected;
    public string? LastError { get; set; }
    public DateTime? LastSyncDate { get; set; }
    
    // Timestamps
    public DateTime ConnectedDate { get; set; } = DateTime.UtcNow;
    public DateTime ModifiedDate { get; set; } = DateTime.UtcNow;
}

public enum ConnectionStatus
{
    Connected,
    Expired,
    Error,
    Disconnected
}
