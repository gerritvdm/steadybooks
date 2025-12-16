using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SteadyBooks.Models;

namespace SteadyBooks.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<ClientDashboard> ClientDashboards { get; set; } = default!;
    public DbSet<DashboardConfiguration> DashboardConfigurations { get; set; } = default!;
    public DbSet<QuickBooksConnection> QuickBooksConnections { get; set; } = default!;
    public DbSet<Subscription> Subscriptions { get; set; } = default!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure ClientDashboard
        modelBuilder.Entity<ClientDashboard>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.DashboardName).IsRequired().HasMaxLength(200);
            entity.Property(e => e.ClientCompanyName).HasMaxLength(200);
            entity.Property(e => e.AccessLink).IsRequired().HasMaxLength(100);
            entity.HasIndex(e => e.AccessLink).IsUnique();
            
            // Relationship to ApplicationUser (Firm)
            entity.HasOne(e => e.Firm)
                .WithMany()
                .HasForeignKey(e => e.FirmId)
                .OnDelete(DeleteBehavior.Cascade);
            
            // Relationship to DashboardConfiguration (one-to-one)
            entity.HasOne(e => e.Configuration)
                .WithOne(c => c.ClientDashboard)
                .HasForeignKey<DashboardConfiguration>(c => c.ClientDashboardId)
                .OnDelete(DeleteBehavior.Cascade);
            
            // Relationship to QuickBooksConnection (one-to-one)
            entity.HasOne(e => e.QuickBooksConnection)
                .WithOne(q => q.ClientDashboard)
                .HasForeignKey<QuickBooksConnection>(q => q.ClientDashboardId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Configure DashboardConfiguration
        modelBuilder.Entity<DashboardConfiguration>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.CustomTitle).HasMaxLength(200);
            entity.Property(e => e.WelcomeMessage).HasMaxLength(500);
        });

        // Configure QuickBooksConnection
        modelBuilder.Entity<QuickBooksConnection>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.RealmId).IsRequired().HasMaxLength(50);
            entity.Property(e => e.CompanyName).IsRequired().HasMaxLength(200);
            entity.Property(e => e.AccessToken).IsRequired();
            entity.Property(e => e.RefreshToken).IsRequired();
            entity.HasIndex(e => e.RealmId);
        });

        // Configure Subscription
        modelBuilder.Entity<Subscription>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.StripeCustomerId).IsRequired().HasMaxLength(100);
            entity.Property(e => e.StripeSubscriptionId).IsRequired().HasMaxLength(100);
            entity.Property(e => e.StripePriceId).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Currency).IsRequired().HasMaxLength(3);
            entity.HasIndex(e => e.StripeCustomerId);
            entity.HasIndex(e => e.StripeSubscriptionId).IsUnique();
            
            // Relationship to ApplicationUser (one-to-one)
            entity.HasOne(e => e.User)
                .WithOne(u => u.Subscription)
                .HasForeignKey<Subscription>(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}