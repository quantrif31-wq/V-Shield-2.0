using API.Models;
using Microsoft.EntityFrameworkCore;

namespace API.Data;

public partial class ApplicationDbContext
{
    public DbSet<AccessCredential> AccessCredentials { get; set; }

    private static void ConfigureAccessCredentials(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AccessCredential>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.CredentialType).IsRequired().HasMaxLength(40);
            entity.Property(x => x.Status).IsRequired().HasMaxLength(20);
            entity.Property(x => x.IdentifierHash).HasMaxLength(64);
            entity.Property(x => x.IdentifierHashVersion).HasMaxLength(40);
            entity.Property(x => x.MaskedIdentifier).HasMaxLength(80);
            entity.Property(x => x.Description).HasMaxLength(500);
            entity.Property(x => x.RevocationReason).HasMaxLength(500);
            entity.Property(x => x.RowVersion).IsRowVersion();

            entity.HasIndex(x => x.EmployeeId);
            entity.HasIndex(x => x.CredentialType);
            entity.HasIndex(x => x.Status);
            entity.HasIndex(x => x.ExpiresAtUtc);
            entity.HasIndex(x => new { x.EmployeeId, x.CredentialType });
            entity.HasIndex(x => new { x.CredentialType, x.IdentifierHash })
                .IsUnique()
                .HasFilter("[IdentifierHash] IS NOT NULL");
            entity.HasIndex(x => x.EmployeeDynamicQrId)
                .IsUnique()
                .HasFilter("[EmployeeDynamicQrId] IS NOT NULL");
            entity.HasIndex(x => x.EmployeeId)
                .IsUnique()
                .HasDatabaseName("UX_AccessCredentials_ActiveFace_Employee")
                .HasFilter("[CredentialType] = 'FaceBiometric' AND [Status] = 'Active'");

            entity.HasOne(x => x.Employee).WithMany()
                .HasForeignKey(x => x.EmployeeId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.EmployeeDynamicQr).WithMany()
                .HasForeignKey(x => x.EmployeeDynamicQrId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.CreatedByUser).WithMany()
                .HasForeignKey(x => x.CreatedByUserId).OnDelete(DeleteBehavior.SetNull);
            entity.HasOne(x => x.RevokedByUser).WithMany()
                .HasForeignKey(x => x.RevokedByUserId).OnDelete(DeleteBehavior.SetNull);
        });
    }
}
