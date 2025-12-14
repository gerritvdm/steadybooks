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
        });
    }
}