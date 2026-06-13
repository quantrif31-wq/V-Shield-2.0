namespace API.Services.AI;

public class AiProviderOptions
{
    public const string SectionName = "AiProvider";

    /// <summary>
    /// Endpoint URL cho AI provider. Ví dụ: https://api.openai.com/v1/chat/completions
    /// </summary>
    public string Endpoint { get; set; } = string.Empty;

    /// <summary>
    /// API key. Không lưu trong repo, dùng environment variable VSHIELD_AI_API_KEY.
    /// </summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>
    /// Tên model (ví dụ: gpt-4o, claude-3-opus).
    /// </summary>
    public string Model { get; set; } = "gpt-4o-mini";

    /// <summary>
    /// Provider name: "Disabled", "Http", "Azure", "OpenAI", "Anthropic".
    /// Mặc định "Disabled" để fallback heuristic.
    /// </summary>
    public string Provider { get; set; } = "Disabled";

    /// <summary>
    /// Timeout tính bằng giây cho mỗi request.
    /// </summary>
    public int TimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// Số lần retry tối đa.
    /// </summary>
    public int MaxRetries { get; set; } = 2;

    /// <summary>
    /// Kích thước tối đa của prompt (character).
    /// </summary>
    public int MaxPromptLength { get; set; } = 8000;

    /// <summary>
    /// Circuit breaker: số lần lỗi liên tiếp trước khi tạm ngưng.
    /// </summary>
    public int CircuitBreakerFailureCount { get; set; } = 5;

    /// <summary>
    /// Circuit breaker: thời gian nghỉ (giây) trước khi thử lại.
    /// </summary>
    public int CircuitBreakerCooldownSeconds { get; set; } = 60;

    /// <summary>
    /// Rate limit: số request tối đa mỗi phút.
    /// </summary>
    public int RateLimitPerMinute { get; set; } = 60;
}
