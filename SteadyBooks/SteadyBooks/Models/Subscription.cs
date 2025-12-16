namespace SteadyBooks.Models;

public class Subscription
{
    public int Id { get; set; }
    
    // User relationship
    public string UserId { get; set; } = default!;
    public ApplicationUser User { get; set; } = default!;
    
    // Stripe IDs
    public string StripeCustomerId { get; set; } = default!;
    public string StripeSubscriptionId { get; set; } = default!;
    public string StripePriceId { get; set; } = default!;
    
    // Subscription details
    public SubscriptionPlan Plan { get; set; }
    public SubscriptionStatus Status { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "usd";
    public BillingInterval Interval { get; set; }
    
    // Dates
    public DateTime CurrentPeriodStart { get; set; }
    public DateTime CurrentPeriodEnd { get; set; }
    public DateTime? CancelAt { get; set; }
    public DateTime? CanceledAt { get; set; }
    public DateTime? TrialStart { get; set; }
    public DateTime? TrialEnd { get; set; }
    
    // Timestamps
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    public DateTime ModifiedDate { get; set; } = DateTime.UtcNow;
}

public enum SubscriptionPlan
{
    FreeTrial,
    Professional,
    Business,
    Enterprise
}

public enum SubscriptionStatus
{
    Trialing,
    Active,
    PastDue,
    Canceled,
    Unpaid,
    Incomplete,
    IncompleteExpired
}

public enum BillingInterval
{
    Month,
    Year
}
