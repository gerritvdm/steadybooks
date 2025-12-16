using Microsoft.AspNetCore.Identity;

namespace SteadyBooks.Models;

public class ApplicationUser : IdentityUser
{
    public string? FirmName { get; set; }
    public string? PrimaryContactEmail { get; set; }
    
    // Branding fields
    public string? LogoPath { get; set; }
    public string? BrandColor { get; set; } = "#667eea"; // Default gradient color
    public string? ContactPhone { get; set; }
    public string? FooterMessage { get; set; }
    
    // Stripe fields
    public string? StripeCustomerId { get; set; }
    public SubscriptionPlan CurrentPlan { get; set; } = SubscriptionPlan.FreeTrial;
    public DateTime? TrialEndsAt { get; set; }
    
    // Account management
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    public bool IsActive { get; set; } = true;
    
    // Navigation property
    public Subscription? Subscription { get; set; }
}
