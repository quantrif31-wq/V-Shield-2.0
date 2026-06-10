using System.ComponentModel.DataAnnotations;

namespace API.Models;

public class SecurityEvent
{
    public long SecurityEventId { get; set; }
    [MaxLength(80)] public string SourceType { get; set; } = "Access";
    [MaxLength(80)] public string? SourceId { get; set; }
    [MaxLength(80)] public string EventType { get; set; } = string.Empty;
    [MaxLength(40)] public string Severity { get; set; } = "Info";
    public int? SiteId { get; set; }
    public int? SecurityZoneId { get; set; }
    public int? AccessPointId { get; set; }
    [MaxLength(40)] public string? SubjectType { get; set; }
    public int? SubjectId { get; set; }
    public int? VehicleId { get; set; }
    [MaxLength(80)] public string? PlateText { get; set; }
    public decimal? Confidence { get; set; }
    [MaxLength(100)] public string CorrelationId { get; set; } = Guid.NewGuid().ToString("N");
    [MaxLength(2000)] public string? Summary { get; set; }
    public DateTime OccurredAtUtc { get; set; } = DateTime.UtcNow;
}

public class EventCorrelation
{
    public long EventCorrelationId { get; set; }
    [MaxLength(100)] public string CorrelationId { get; set; } = string.Empty;
    [MaxLength(160)] public string RuleName { get; set; } = string.Empty;
    [MaxLength(40)] public string Severity { get; set; } = "Info";
    [MaxLength(2000)] public string Summary { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}

public class VideoBookmark
{
    public int VideoBookmarkId { get; set; }
    public long? SecurityEventId { get; set; }
    public int? CameraId { get; set; }
    [MaxLength(300)] public string? ArtifactReference { get; set; }
    public DateTime StartUtc { get; set; }
    public DateTime EndUtc { get; set; }
    [MaxLength(1000)] public string? Note { get; set; }
}

public class SiteMap
{
    public int SiteMapId { get; set; }
    public int? SiteId { get; set; }
    [MaxLength(160)] public string Name { get; set; } = string.Empty;
    [MaxLength(300)] public string AssetReference { get; set; } = string.Empty;
    [MaxLength(80)] public string CoordinateSystem { get; set; } = "Normalized";
    public bool IsActive { get; set; } = true;
}

public class MapDevicePlacement
{
    public int MapDevicePlacementId { get; set; }
    public int SiteMapId { get; set; }
    public int? SecurityDeviceId { get; set; }
    public int? CameraId { get; set; }
    public decimal X { get; set; }
    public decimal Y { get; set; }
    [MaxLength(80)] public string IconType { get; set; } = "Device";
    public SiteMap? SiteMap { get; set; }
    public SecurityDevice? SecurityDevice { get; set; }
    public Camera? Camera { get; set; }
}

public class AiAdjudicationItem
{
    public int AiAdjudicationItemId { get; set; }
    public long? SecurityEventId { get; set; }
    [MaxLength(80)] public string AiSource { get; set; } = "Unknown";
    [MaxLength(80)] public string ModelVersion { get; set; } = "Unknown";
    [MaxLength(40)] public string Status { get; set; } = "Pending";
    [MaxLength(40)] public string? Outcome { get; set; }
    [MaxLength(1000)] public string? ReviewNote { get; set; }
    public decimal? Confidence { get; set; }
    public int? ReviewedByUserId { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? ReviewedAtUtc { get; set; }
}

public class AiPerformanceMetric
{
    public int AiPerformanceMetricId { get; set; }
    [MaxLength(80)] public string AiSource { get; set; } = "Unknown";
    [MaxLength(80)] public string MetricName { get; set; } = string.Empty;
    public decimal MetricValue { get; set; }
    public DateTime CapturedAtUtc { get; set; } = DateTime.UtcNow;
    [MaxLength(1000)] public string? Notes { get; set; }
}

