using API.Models;
using Microsoft.EntityFrameworkCore;

namespace API.Data;

public partial class ApplicationDbContext
{
    public DbSet<PrivilegedActionSession> PrivilegedActionSessions { get; set; }

    private static void ConfigurePrivilegedOperations(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<PrivilegedActionSession>(entity =>
        {
            entity.HasKey(e => e.PrivilegedActionSessionId);
            entity.Property(e => e.Action).IsRequired().HasMaxLength(120);
            entity.Property(e => e.Status).IsRequired().HasMaxLength(80);
            entity.Property(e => e.ChallengeNonce).IsRequired().HasMaxLength(120);
            entity.Property(e => e.CorrelationId).IsRequired().HasMaxLength(80);
            entity.HasIndex(e => new { e.UserId, e.Action, e.Status, e.ExpiresAtUtc });
            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
