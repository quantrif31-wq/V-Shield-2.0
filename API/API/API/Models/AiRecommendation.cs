using System.ComponentModel.DataAnnotations;

namespace API.Models;

public class AiRecommendation
{
    public long Id { get; set; }

    public long AnalysisJobId { get; set; }

    [MaxLength(80)]
    public string Domain { get; set; } = string.Empty;

    [MaxLength(80)]
    public string EntityType { get; set; } = string.Empty;

    [MaxLength(120)]
    public string EntityId { get; set; } = string.Empty;

    [MaxLength(40)]
    public string Severity { get; set; } = "Medium";

    public decimal Confidence { get; set; }

    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [MaxLength(2000)]
    public string Summary { get; set; } = string.Empty;

    [MaxLength(4000)]
    public string? ReasoningSummary { get; set; }

    [MaxLength(500)]
    public string? RecommendedAction { get; set; }

    public bool RequiresHumanApproval { get; set; } = true;

    public bool RequiresStepUp { get; set; }

    [MaxLength(40)]
    public string Status { get; set; } = "Draft";

    public int? ReviewedByUserId { get; set; }

    public DateTime? ReviewedAtUtc { get; set; }

    public DateTime? ExecutedAtUtc { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public AiAnalysisJob? AnalysisJob { get; set; }
    public ICollection<AiRecommendationEvidence> Evidence { get; set; } = new List<AiRecommendationEvidence>();
}
