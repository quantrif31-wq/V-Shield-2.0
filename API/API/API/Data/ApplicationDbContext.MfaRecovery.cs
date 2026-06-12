using API.Models;
using Microsoft.EntityFrameworkCore;

namespace API.Data;

public partial class ApplicationDbContext
{
    public DbSet<MfaRecoveryCode> MfaRecoveryCodes { get; set; }

    private static void ConfigureMfaRecovery(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<MfaRecoveryCode>(entity =>
        {
            entity.HasKey(e => e.MfaRecoveryCodeId);
            entity.Property(e => e.CodeHash).IsRequired().HasMaxLength(128);
            entity.HasIndex(e => e.CodeHash).IsUnique();
            entity.HasIndex(e => new { e.UserId, e.UsedAtUtc, e.ExpiresAtUtc });
            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
