using API.Models;
using Microsoft.EntityFrameworkCore;

namespace API.Data;

public partial class ApplicationDbContext
{
    public DbSet<SecurityEvent> SecurityEvents { get; set; }
    public DbSet<EventCorrelation> EventCorrelations { get; set; }
    public DbSet<VideoBookmark> VideoBookmarks { get; set; }
    public DbSet<SiteMap> SiteMaps { get; set; }
    public DbSet<MapDevicePlacement> MapDevicePlacements { get; set; }
    public DbSet<AiAdjudicationItem> AiAdjudicationItems { get; set; }
    public DbSet<AiPerformanceMetric> AiPerformanceMetrics { get; set; }

    private static void ConfigureSituationalAwareness(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<SecurityEvent>(entity =>
        {
            entity.HasKey(e => e.SecurityEventId);
            entity.Property(e => e.SourceType).IsRequired().HasMaxLength(80);
            entity.Property(e => e.EventType).IsRequired().HasMaxLength(80);
            entity.Property(e => e.Severity).IsRequired().HasMaxLength(40);
            entity.Property(e => e.CorrelationId).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Confidence).HasColumnType("decimal(9,6)");
            entity.Property(e => e.SiteNameSnapshot).HasMaxLength(160);
            entity.Property(e => e.SecurityZoneNameSnapshot).HasMaxLength(160);
            entity.Property(e => e.AccessPointNameSnapshot).HasMaxLength(160);
            entity.HasIndex(e => new { e.CorrelationId, e.OccurredAtUtc });
            entity.HasIndex(e => new { e.SiteId, e.SecurityZoneId, e.Severity, e.OccurredAtUtc });
        });

        modelBuilder.Entity<EventCorrelation>(entity =>
        {
            entity.HasKey(e => e.EventCorrelationId);
            entity.Property(e => e.CorrelationId).IsRequired().HasMaxLength(100);
            entity.Property(e => e.RuleName).IsRequired().HasMaxLength(160);
            entity.Property(e => e.Severity).IsRequired().HasMaxLength(40);
            entity.Property(e => e.Summary).IsRequired().HasMaxLength(2000);
            entity.HasIndex(e => e.CorrelationId);
        });

        modelBuilder.Entity<VideoBookmark>(entity =>
        {
            entity.HasKey(e => e.VideoBookmarkId);
            entity.HasIndex(e => new { e.SecurityEventId, e.CameraId });
        });

        modelBuilder.Entity<SiteMap>(entity =>
        {
            entity.HasKey(e => e.SiteMapId);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(160);
            entity.Property(e => e.AssetReference).IsRequired().HasMaxLength(300);
            entity.Property(e => e.CoordinateSystem).IsRequired().HasMaxLength(80);
        });

        modelBuilder.Entity<MapDevicePlacement>(entity =>
        {
            entity.HasKey(e => e.MapDevicePlacementId);
            entity.Property(e => e.X).HasColumnType("decimal(9,4)");
            entity.Property(e => e.Y).HasColumnType("decimal(9,4)");
            entity.Property(e => e.IconType).IsRequired().HasMaxLength(80);
            entity.HasOne(e => e.SiteMap).WithMany().HasForeignKey(e => e.SiteMapId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.SecurityDevice).WithMany().HasForeignKey(e => e.SecurityDeviceId).OnDelete(DeleteBehavior.SetNull);
            entity.HasOne(e => e.Camera).WithMany().HasForeignKey(e => e.CameraId).OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<AiAdjudicationItem>(entity =>
        {
            entity.HasKey(e => e.AiAdjudicationItemId);
            entity.Property(e => e.AiSource).IsRequired().HasMaxLength(80);
            entity.Property(e => e.ModelVersion).IsRequired().HasMaxLength(80);
            entity.Property(e => e.Status).IsRequired().HasMaxLength(40);
            entity.Property(e => e.Confidence).HasColumnType("decimal(9,6)");
            entity.HasIndex(e => new { e.AiSource, e.Status, e.CreatedAtUtc });
        });

        modelBuilder.Entity<AiPerformanceMetric>(entity =>
        {
            entity.HasKey(e => e.AiPerformanceMetricId);
            entity.Property(e => e.AiSource).IsRequired().HasMaxLength(80);
            entity.Property(e => e.MetricName).IsRequired().HasMaxLength(80);
            entity.Property(e => e.MetricValue).HasColumnType("decimal(12,4)");
            entity.HasIndex(e => new { e.AiSource, e.MetricName, e.CapturedAtUtc });
        });
    }
}
