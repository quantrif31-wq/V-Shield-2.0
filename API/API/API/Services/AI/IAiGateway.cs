namespace API.Services.AI;

public class AiModelRequest
{
    public string PromptTemplateKey { get; set; } = string.Empty;
    public int PromptTemplateVersion { get; set; } = 1;
    public Dictionary<string, string> Parameters { get; set; } = new();
    public string? InputHash { get; set; }
    public string? CorrelationId { get; set; }
}

public class AiModelResponse
{
    public string OutputText { get; set; } = string.Empty;
    public string? OutputHash { get; set; }
    public int? LatencyMs { get; set; }
    public int? TokenEstimate { get; set; }
    public string Provider { get; set; } = "Disabled";
    public string Model { get; set; } = "Deterministic";
    public bool IsFallback { get; set; }
}

public interface IAiGateway
{
    /// <summary>
    /// Gửi request đến AI model (hoặc fallback heuristic nếu provider bị tắt).
    /// </summary>
    Task<AiModelResponse> ExecuteAsync(AiModelRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Kiểm tra provider có sẵn sàng không.
    /// </summary>
    bool IsProviderAvailable();
}
