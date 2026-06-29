using API.Models;
using Microsoft.EntityFrameworkCore;

namespace API.Data;

public partial class ApplicationDbContext
{
    public DbSet<UserOperationalScope> UserOperationalScopes { get; set; }

    private static void ConfigureUserOperationalScope(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<UserOperationalScope>(entity =>
        {
            entity.HasKey(e => e.UserOperationalScopeId);
            entity.Property(e => e.TaskKey).IsRequired().HasMaxLength(64);
            entity.Property(e => e.Note).HasMaxLength(500);

            entity.HasIndex(e => new { e.UserId, e.TaskKey });
            entity.HasIndex(e => new { e.UserId, e.SiteId, e.GateId, e.LaneId, e.SecurityZoneId });

            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.CreatedByUser)
                .WithMany()
                .HasForeignKey(e => e.CreatedByUserId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(e => e.Site)
                .WithMany()
                .HasForeignKey(e => e.SiteId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.Gate)
                .WithMany()
                .HasForeignKey(e => e.GateId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.Lane)
                .WithMany()
                .HasForeignKey(e => e.LaneId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.SecurityZone)
                .WithMany()
                .HasForeignKey(e => e.SecurityZoneId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
