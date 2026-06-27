using API.Models;
using Microsoft.EntityFrameworkCore;

namespace API.Data;

public partial class ApplicationDbContext
{
    public DbSet<GuardZoneAuthority> GuardZoneAuthorities { get; set; }

    private static void ConfigureGuardZoneAuthority(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<GuardZoneAuthority>(entity =>
        {
            entity.HasKey(e => e.GuardZoneAuthorityId);
            entity.Property(e => e.AuthorityLevel).IsRequired().HasMaxLength(40);
            entity.Property(e => e.Note).HasMaxLength(500);

            entity.HasIndex(e => new { e.UserId, e.SecurityZoneId });
            entity.HasIndex(e => new { e.UserId, e.ValidTo });

            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_GuardZoneAuthority_User");

            entity.HasOne(e => e.SecurityZone)
                .WithMany()
                .HasForeignKey(e => e.SecurityZoneId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_GuardZoneAuthority_SecurityZone");

            entity.HasOne(e => e.GrantedByUser)
                .WithMany()
                .HasForeignKey(e => e.GrantedByUserId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("FK_GuardZoneAuthority_GrantedByUser");
        });
    }
}
