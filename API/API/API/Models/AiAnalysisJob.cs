using System.ComponentModel.DataAnnotations;

namespace API.Models;

public class AiAnalysisJob
{
    public long Id { get; set; }

    [MaxLength(80)]
    public string JobType { get; set; } = string.Empty;

    [MaxLength(40)]
    public string Status { get; set; } = "Pending";

    public int? RequestedByUserId { get; set; }

    [MaxLength(100)]
    public string CorrelationId { get; set; } = Guid.NewGuid().ToString("N");

    [MaxLength(2000)]
    public string? InputSummary { get; set; }

    public DateTime? StartedAtUtc { get; set; }

    public DateTime? CompletedAtUtc { get; set; }

    [MaxLength(500)]
    public string? ErrorCode { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public ICollection<AiModelRun> ModelRuns { get; set; } = new List<AiModelRun>();
    public ICollection<AiRecommendation> Recommendations { get; set; } = new List<AiRecommendation>();
}
