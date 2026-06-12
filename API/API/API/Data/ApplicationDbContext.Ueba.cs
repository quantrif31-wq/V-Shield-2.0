using API.Models;
using Microsoft.EntityFrameworkCore;

namespace API.Data;

public partial class ApplicationDbContext
{
    public DbSet<UEBAProfile> UEBAProfiles { get; set; }
    public DbSet<UEBAAnomaly> UEBAAnomalies { get; set; }

    private void ConfigureUeba(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<UEBAProfile>(entity =>
        {
            entity.HasKey(e => e.ProfileId);
            entity.HasIndex(e => e.EmployeeId).IsUnique();
            entity.Property(e => e.AvgAccessPerDay).HasColumnType("decimal(8,2)");
            entity.Property(e => e.WeekendAccessRatio).HasColumnType("decimal(5,1)");
            entity.Property(e => e.BypassRate).HasColumnType("decimal(5,1)");
            entity.Property(e => e.RiskScore).HasColumnType("decimal(5,1)");
            entity.Property(e => e.LastBuiltAt).HasDefaultValueSql("(getutcdate())");

            entity.HasOne(e => e.Employee)
                .WithMany()
                .HasForeignKey(e => e.EmployeeId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_UEBAProfile_Employee");
        });

        modelBuilder.Entity<UEBAAnomaly>(entity =>
        {
            entity.HasKey(e => e.AnomalyId);
            entity.Property(e => e.AnomalyType).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Severity).IsRequired().HasMaxLength(20);
            entity.Property(e => e.Description).IsRequired().HasMaxLength(500);
            entity.Property(e => e.SupportingData).HasMaxLength(200);
            entity.Property(e => e.Status).IsRequired().HasMaxLength(20).HasDefaultValue(UEBAStatuses.Open);
            entity.Property(e => e.Resolution).HasMaxLength(500);
            entity.Property(e => e.DetectedAt).HasDefaultValueSql("(getutcdate())");
            entity.HasIndex(e => new { e.EmployeeId, e.Status });
            entity.HasIndex(e => new { e.EventTimestamp, e.AnomalyType });

            entity.HasOne(e => e.Employee)
                .WithMany()
                .HasForeignKey(e => e.EmployeeId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_UEBAAnomaly_Employee");

            entity.HasOne(e => e.AccessLog)
                .WithMany()
                .HasForeignKey(e => e.AccessLogId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("FK_UEBAAnomaly_AccessLog");

            entity.HasOne(e => e.ResolvedByUser)
                .WithMany()
                .HasForeignKey(e => e.ResolvedBy)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("FK_UEBAAnomaly_ResolvedByUser");
        });
    }
}
