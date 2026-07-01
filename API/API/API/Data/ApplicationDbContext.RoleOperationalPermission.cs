using API.Models;
using Microsoft.EntityFrameworkCore;

namespace API.Data;

public partial class ApplicationDbContext
{
    public DbSet<RoleOperationalPermission> RoleOperationalPermissions { get; set; }

    private static void ConfigureRoleOperationalPermissions(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<RoleOperationalPermission>(entity =>
        {
            entity.HasKey(e => e.RoleOperationalPermissionId);
            entity.Property(e => e.Role).IsRequired().HasMaxLength(32);
            entity.Property(e => e.TaskKey).IsRequired().HasMaxLength(64);
            entity.HasIndex(e => new { e.Role, e.TaskKey }).IsUnique();

            entity.HasOne(e => e.UpdatedByUser)
                .WithMany()
                .HasForeignKey(e => e.UpdatedByUserId)
                .OnDelete(DeleteBehavior.SetNull);
        });
    }
}
