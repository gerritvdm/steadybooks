namespace SteadyBooks.Models;

public class DashboardConfiguration
{
    public int Id { get; set; }
    
    // Foreign key to ClientDashboard
    public int ClientDashboardId { get; set; }
    public ClientDashboard ClientDashboard { get; set; } = default!;
    
    // Date Range Settings
    public DateRangeType DateRange { get; set; } = DateRangeType.ThisMonth;
    public DateTime? CustomStartDate { get; set; }
    public DateTime? CustomEndDate { get; set; }
    
    // Widget Toggles
    public bool ShowCashBalance { get; set; } = true;
    public bool ShowProfit { get; set; } = true;
    public bool ShowTaxesDue { get; set; } = true;
    public bool ShowOutstandingInvoices { get; set; } = true;
    
    // Account Mapping (JSON for flexibility)
    // Will store QuickBooks account IDs when QBO is connected
    public string? CashAccountMapping { get; set; } // JSON array of account IDs
    public string? TaxAccountMapping { get; set; }  // JSON array of account IDs
    
    // Display Settings
    public string? CustomTitle { get; set; }
    public string? WelcomeMessage { get; set; }
    
    // Timestamps
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    public DateTime ModifiedDate { get; set; } = DateTime.UtcNow;
}

public enum DateRangeType
{
    ThisMonth,
    LastMonth,
    YearToDate,
    Custom
}
