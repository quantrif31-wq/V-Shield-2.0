using System.ComponentModel.DataAnnotations;

namespace API.Models;

public class AiEventMetadata
{
    public long Id { get; set; }

    [MaxLength(80)]
    public string SourceType { get; set; } = string.Empty;

    [MaxLength(120)]
    public string? SourceId { get; set; }

    [MaxLength(80)]
    public string EventType { get; set; } = string.Empty;

    public DateTime OccurredAtUtc { get; set; } = DateTime.UtcNow;

    public int? SiteId { get; set; }

    public int? ZoneId { get; set; }

    public int? CameraId { get; set; }

    public int? GateId { get; set; }

    [MaxLength(80)]
    public string? SubjectType { get; set; }

    [MaxLength(120)]
    public string? SubjectId { get; set; }

    [MaxLength(80)]
    public string? ObjectType { get; set; }

    [MaxLength(200)]
    public string? Label { get; set; }

    public decimal? Confidence { get; set; }

    [MaxLength(80)]
    public string? ModelName { get; set; }

    [MaxLength(80)]
    public string? ModelVersion { get; set; }

    [MaxLength(4000)]
    public string? RawMetadataJson { get; set; }

    [MaxLength(100)]
    public string CorrelationId { get; set; } = Guid.NewGuid().ToString("N");

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
