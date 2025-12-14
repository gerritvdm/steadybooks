namespace SteadyBooks.Models;

public class ClientDashboard
{
    public int Id { get; set; }
    
    // Foreign key to ApplicationUser (the accountant/firm)
    public string FirmId { get; set; } = default!;
    public ApplicationUser Firm { get; set; } = default!;
    
    // Dashboard details
    public string DashboardName { get; set; } = default!;
    public string? ClientCompanyName { get; set; }
    
    // Secure access link (GUID-based)
    public string AccessLink { get; set; } = Guid.NewGuid().ToString();
    public string? AccessPassword { get; set; } // Hashed, optional
    
    // Status
    public bool IsActive { get; set; } = true;
    public DashboardStatus Status { get; set; } = DashboardStatus.Draft;
    
    // Timestamps
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    public DateTime ModifiedDate { get; set; } = DateTime.UtcNow;
    public DateTime? LastAccessedDate { get; set; }
    
    // Navigation properties
    public DashboardConfiguration? Configuration { get; set; }
    public QuickBooksConnection? QuickBooksConnection { get; set; }
}

public enum DashboardStatus
{
    Draft,          // Created but not configured
    Active,         // Fully configured and accessible
    NeedsAttention, // QuickBooks connection issue
    Archived        // Deactivated
}
