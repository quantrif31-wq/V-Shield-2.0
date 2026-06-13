using System.ComponentModel.DataAnnotations;

namespace API.Models;

public class AiModelRun
{
    public long Id { get; set; }

    public long AnalysisJobId { get; set; }

    [MaxLength(80)]
    public string Provider { get; set; } = "Disabled";

    [MaxLength(80)]
    public string Model { get; set; } = "Deterministic";

    [MaxLength(120)]
    public string PromptTemplateKey { get; set; } = string.Empty;

    public int PromptTemplateVersion { get; set; } = 1;

    [MaxLength(128)]
    public string? InputHash { get; set; }

    [MaxLength(128)]
    public string? OutputHash { get; set; }

    public int? LatencyMs { get; set; }

    public int? TokenEstimate { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public AiAnalysisJob? AnalysisJob { get; set; }
}
