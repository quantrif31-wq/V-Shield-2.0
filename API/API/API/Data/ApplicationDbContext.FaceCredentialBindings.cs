using API.Models;
using Microsoft.EntityFrameworkCore;

namespace API.Data;

public partial class ApplicationDbContext
{
    public DbSet<EmployeeFaceCredentialBinding> EmployeeFaceCredentialBindings { get; set; }

    private static void ConfigureFaceCredentialBindings(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<EmployeeFaceCredentialBinding>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Status).IsRequired().HasMaxLength(20);
            entity.Property(x => x.Reason).HasMaxLength(500);
            entity.Property(x => x.RowVersion).IsRowVersion();

            entity.HasIndex(x => x.Status);
            entity.HasIndex(x => x.ActivatedAtUtc);
            entity.HasIndex(x => x.RevokedAtUtc);
            entity.HasIndex(x => x.EmployeeId)
                .IsUnique()
                .HasFilter("[Status] = 'Active'")
                .HasDatabaseName("UX_EmployeeFaceCredentialBindings_ActiveEmployee");
            entity.HasIndex(x => x.AccessCredentialId)
                .IsUnique()
                .HasFilter("[Status] = 'Active'")
                .HasDatabaseName("UX_EmployeeFaceCredentialBindings_ActiveCredential");

            entity.HasOne(x => x.Employee).WithMany()
                .HasForeignKey(x => x.EmployeeId).OnDelete(DeleteBehavior.NoAction);
            entity.HasOne(x => x.AccessCredential).WithMany()
                .HasForeignKey(x => x.AccessCredentialId).OnDelete(DeleteBehavior.NoAction);
            entity.HasOne(x => x.CreatedByUser).WithMany()
                .HasForeignKey(x => x.CreatedByUserId).OnDelete(DeleteBehavior.NoAction);
            entity.HasOne(x => x.RevokedByUser).WithMany()
                .HasForeignKey(x => x.RevokedByUserId).OnDelete(DeleteBehavior.NoAction);
        });
    }
}
