using API.Models;
using Microsoft.EntityFrameworkCore;

namespace API.Data;

public partial class ApplicationDbContext
{
    public DbSet<ImportExportHistory> ImportExportHistories { get; set; }

    private static void ConfigureImportExport(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ImportExportHistory>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.OperationType).IsRequired().HasMaxLength(20);
            entity.Property(e => e.EntityType).IsRequired().HasMaxLength(50);
            entity.Property(e => e.FileName).HasMaxLength(255);
            entity.Property(e => e.FileFormat).HasMaxLength(10);
            entity.Property(e => e.Status).IsRequired().HasMaxLength(20).HasDefaultValue("Pending");
            entity.Property(e => e.PerformedAt).HasDefaultValueSql("(getutcdate())");
            entity.HasIndex(e => new { e.EntityType, e.OperationType });
            entity.HasIndex(e => e.PerformedAt);
            entity.HasOne(e => e.PerformedBy)
                .WithMany()
                .HasForeignKey(e => e.PerformedById)
                .OnDelete(DeleteBehavior.SetNull);
        });
    }
}
