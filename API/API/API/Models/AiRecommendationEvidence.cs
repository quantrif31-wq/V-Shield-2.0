using System.ComponentModel.DataAnnotations;

namespace API.Models;

public class AiRecommendationEvidence
{
    public long Id { get; set; }

    public long RecommendationId { get; set; }

    [MaxLength(80)]
    public string SourceType { get; set; } = string.Empty;

    [MaxLength(120)]
    public string? SourceId { get; set; }

    public DateTime? SourceTimestampUtc { get; set; }

    [MaxLength(2000)]
    public string? Snippet { get; set; }

    public decimal Weight { get; set; } = 1.0m;

    public AiRecommendation? Recommendation { get; set; }
}
