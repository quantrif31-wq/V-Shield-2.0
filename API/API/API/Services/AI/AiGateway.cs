using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;

namespace API.Services.AI;

public class AiGateway : IAiGateway
{
    private readonly AiProviderOptions _options;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<AiGateway> _logger;
    private readonly Queue<DateTime> _requestTimestamps = new();
    private readonly object _rateLimitLock = new();
    private readonly object _circuitLock = new();

    private int _consecutiveFailures;
    private DateTime _circuitOpenUntil = DateTime.MinValue;

    public AiGateway(
        IOptions<AiProviderOptions> options,
        IHttpClientFactory httpClientFactory,
        ILogger<AiGateway> logger)
    {
        _options = options.Value;
        _httpClientFactory = httpClientFactory;
        _logger = logger;

        // Override from environment variables
        var envEndpoint = Environment.GetEnvironmentVariable("VSHIELD_AI_ENDPOINT");
        var envApiKey = Environment.GetEnvironmentVariable("VSHIELD_AI_API_KEY");
        var envProvider = Environment.GetEnvironmentVariable("VSHIELD_AI_PROVIDER");

        if (!string.IsNullOrWhiteSpace(envEndpoint))
            _options.Endpoint = envEndpoint;
        if (!string.IsNullOrWhiteSpace(envApiKey))
            _options.ApiKey = envApiKey;
        if (!string.IsNullOrWhiteSpace(envProvider))
            _options.Provider = envProvider;
    }

    public bool IsProviderAvailable()
    {
        if (_options.Provider.Equals("Disabled", StringComparison.OrdinalIgnoreCase))
            return false;

        lock (_circuitLock)
        {
            if (_circuitOpenUntil > DateTime.UtcNow)
                return false;
        }

        return !string.IsNullOrWhiteSpace(_options.Endpoint)
            && !string.IsNullOrWhiteSpace(_options.ApiKey);
    }

    public async Task<AiModelResponse> ExecuteAsync(AiModelRequest request, CancellationToken cancellationToken = default)
    {
        var sw = Stopwatch.StartNew();

        if (!IsProviderAvailable())
        {
            _logger.LogInformation("AI provider disabled/not configured. Using deterministic fallback for {TemplateKey}", request.PromptTemplateKey);
            return FallbackResponse(request, sw);
        }

        if (!CheckRateLimit())
        {
            _logger.LogWarning("AI rate limit exceeded. Using fallback for {TemplateKey}", request.PromptTemplateKey);
            return FallbackResponse(request, sw);
        }

        lock (_circuitLock)
        {
            if (_circuitOpenUntil > DateTime.UtcNow)
            {
                _logger.LogWarning("AI circuit breaker open until {OpenUntil}. Using fallback.", _circuitOpenUntil);
                return FallbackResponse(request, sw);
            }
        }

        try
        {
            var result = await CallProviderWithRetryAsync(request, cancellationToken);
            RecordSuccess();
            sw.Stop();
            result.LatencyMs = (int)sw.ElapsedMilliseconds;
            return result;
        }
        catch (Exception ex)
        {
            RecordFailure();
            sw.Stop();
            _logger.LogError(ex, "AI provider call failed for {TemplateKey}. Using fallback.", request.PromptTemplateKey);
            var fallback = FallbackResponse(request, sw);
            fallback.IsFallback = true;
            return fallback;
        }
    }

    private async Task<AiModelResponse> CallProviderWithRetryAsync(AiModelRequest request, CancellationToken cancellationToken)
    {
        var lastException = (Exception?)null;
        var maxRetries = Math.Max(0, _options.MaxRetries);

        for (var attempt = 0; attempt <= maxRetries; attempt++)
        {
            try
            {
                using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                cts.CancelAfter(TimeSpan.FromSeconds(_options.TimeoutSeconds));

                return await CallProviderAsync(request, cts.Token);
            }
            catch (OperationCanceledException ex) when (!cancellationToken.IsCancellationRequested)
            {
                lastException = ex;
                _logger.LogWarning("AI provider timeout (attempt {Attempt}/{MaxRetries})", attempt + 1, maxRetries + 1);
            }
            catch (HttpRequestException ex)
            {
                lastException = ex;
                _logger.LogWarning("AI provider HTTP error (attempt {Attempt}/{MaxRetries}): {Message}", attempt + 1, maxRetries + 1, ex.Message);
                if (attempt < maxRetries)
                    await Task.Delay(TimeSpan.FromSeconds(1 * Math.Pow(2, attempt)), cancellationToken);
            }
            catch (Exception ex)
            {
                lastException = ex;
                _logger.LogWarning("AI provider error (attempt {Attempt}/{MaxRetries}): {Message}", attempt + 1, maxRetries + 1, ex.Message);
            }
        }

        throw lastException ?? new InvalidOperationException("AI provider call failed after all retries.");
    }

    private async Task<AiModelResponse> CallProviderAsync(AiModelRequest request, CancellationToken cancellationToken)
    {
        var httpClient = _httpClientFactory.CreateClient("AiGateway");
        var endpoint = _options.Endpoint.TrimEnd('/');

        var payload = new
        {
            model = _options.Model,
            messages = new[]
            {
                new { role = "system", content = "You are a security analysis assistant. Respond in Vietnamese. Provide concise analysis with confidence levels and data sources. Never execute actions, only recommend." },
                new { role = "user", content = request.Parameters.GetValueOrDefault("prompt", "Analyze the security event.") }
            },
            temperature = 0.3,
            max_tokens = 1000
        };

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, endpoint)
        {
            Content = JsonContent.Create(payload)
        };

        if (!string.IsNullOrWhiteSpace(_options.ApiKey))
        {
            httpRequest.Headers.TryAddWithoutValidation("Authorization", $"Bearer {_options.ApiKey}");
        }

        if (!string.IsNullOrWhiteSpace(request.CorrelationId))
        {
            httpRequest.Headers.TryAddWithoutValidation("X-Correlation-Id", request.CorrelationId);
        }

        using var response = await httpClient.SendAsync(httpRequest, cancellationToken);
        response.EnsureSuccessStatusCode();

        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
        var outputText = ExtractTextFromResponse(responseBody);

        var outputHash = ComputeHash(outputText);

        return new AiModelResponse
        {
            OutputText = outputText,
            OutputHash = outputHash,
            Provider = _options.Provider,
            Model = _options.Model,
            TokenEstimate = EstimateTokens(outputText)
        };
    }

    private AiModelResponse FallbackResponse(AiModelRequest request, Stopwatch sw)
    {
        var inputHash = request.InputHash ?? ComputeHash(
            string.Join("|", request.Parameters.Values));

        var outputText = request.PromptTemplateKey switch
        {
            "soc-incident-briefing" => GenerateSocFallback(request.Parameters),
            "evidence-analysis" => GenerateEvidenceFallback(request.Parameters),
            "ueba-risk-explanation" => GenerateUebaFallback(request.Parameters),
            "device-health-diagnosis" => GenerateDeviceFallback(request.Parameters),
            "visitor-screening" => GenerateVisitorFallback(request.Parameters),
            "policy-explanation" => GeneratePolicyFallback(request.Parameters),
            _ => $"[Phan tich deterministic] Khong co AI provider. Du lieu nhan duoc: {string.Join("; ", request.Parameters.Values.Take(3))}"
        };

        sw.Stop();

        return new AiModelResponse
        {
            OutputText = outputText,
            OutputHash = ComputeHash(outputText),
            LatencyMs = (int)sw.ElapsedMilliseconds,
            Provider = "Disabled",
            Model = "Deterministic",
            IsFallback = true
        };
    }

    private static string GenerateSocFallback(Dictionary<string, string> parameters)
    {
        var alarmSummary = parameters.GetValueOrDefault("alarm_summary", "Khong co thong tin");
        var severity = parameters.GetValueOrDefault("severity", "Medium");
        return $"[Phan tich deterministic] Alarm: {alarmSummary} (Muc do: {severity}). Khuyen nghi: Kiem tra va xu ly theo quy trinh SOC tieu chuan. Canh bao: Can xac nhan bang chung tu camera va nhan su tai cho.";
    }

    private static string GenerateEvidenceFallback(Dictionary<string, string> parameters)
    {
        var evidenceType = parameters.GetValueOrDefault("evidence_type", "Unspecified");
        return $"[Phan tich deterministic] Bang chung loai: {evidenceType}. Khuyen nghi: Kiem tra tinh toan ven hash, xac nhan custody chain, va dam bao legal hold truoc khi xuat. Chua phat hien bat thuong tu du lieu co san.";
    }

    private static string GenerateUebaFallback(Dictionary<string, string> parameters)
    {
        var riskScore = parameters.GetValueOrDefault("risk_score", "0");
        return $"[Phan tich deterministic] Diem rui ro: {riskScore}/100. Khuyen nghi: So sanh voi baseline dong nghiep. Xem xet cac yeu to nhu gio truy cap, cong su dung, va tan suat.";
    }

    private static string GenerateDeviceFallback(Dictionary<string, string> parameters)
    {
        var deviceName = parameters.GetValueOrDefault("device_name", "Unnamed");
        var status = parameters.GetValueOrDefault("status", "Unknown");
        return $"[Phan tich deterministic] Thiet bi: {deviceName}. Trang thai: {status}. Khuyen nghi: Kiem tra heartbeat, latency, va restart count. Neu offline qua 5 phut, can kiem tra cable va nguon dien.";
    }

    private static string GenerateVisitorFallback(Dictionary<string, string> parameters)
    {
        var visitorName = parameters.GetValueOrDefault("visitor_name", "Unnamed");
        return $"[Phan tich deterministic] Khach: {visitorName}. Khuyen nghi: Kiem tra watchlist, host approval, va muc dich tham. Rui ro thap neu khong co match watchlist.";
    }

    private static string GeneratePolicyFallback(Dictionary<string, string> parameters)
    {
        var policyName = parameters.GetValueOrDefault("policy_name", "Unnamed");
        return $"[Phan tich deterministic] Chinh sach: {policyName}. Khuyen nghi: Chay simulate truoc khi activate. Kiem tra conflict voi policy hien tai.";
    }

    private static string ExtractTextFromResponse(string responseBody)
    {
        try
        {
            using var doc = JsonDocument.Parse(responseBody);
            // OpenAI format
            if (doc.RootElement.TryGetProperty("choices", out var choices) && choices.GetArrayLength() > 0)
            {
                var firstChoice = choices[0];
                if (firstChoice.TryGetProperty("message", out var message) &&
                    message.TryGetProperty("content", out var content))
                {
                    return content.GetString() ?? string.Empty;
                }
                if (firstChoice.TryGetProperty("text", out var text))
                {
                    return text.GetString() ?? string.Empty;
                }
            }
            // Anthropic format
            if (doc.RootElement.TryGetProperty("content", out var contentArray) && contentArray.GetArrayLength() > 0)
            {
                var firstContent = contentArray[0];
                if (firstContent.TryGetProperty("text", out var text))
                {
                    return text.GetString() ?? string.Empty;
                }
            }
            // Generic
            if (doc.RootElement.TryGetProperty("response", out var response))
            {
                return response.GetString() ?? string.Empty;
            }
            if (doc.RootElement.TryGetProperty("output", out var output))
            {
                return output.GetString() ?? string.Empty;
            }

            return responseBody.Length > 2000 ? responseBody.Substring(0, 2000) : responseBody;
        }
        catch (JsonException)
        {
            return responseBody.Length > 2000 ? responseBody.Substring(0, 2000) : responseBody;
        }
    }

    private bool CheckRateLimit()
    {
        var now = DateTime.UtcNow;
        var windowStart = now.AddMinutes(-1);

        lock (_rateLimitLock)
        {
            while (_requestTimestamps.Count > 0 && _requestTimestamps.Peek() < windowStart)
            {
                _requestTimestamps.Dequeue();
            }

            if (_requestTimestamps.Count >= _options.RateLimitPerMinute)
                return false;

            _requestTimestamps.Enqueue(now);
            return true;
        }
    }

    private void RecordSuccess()
    {
        lock (_circuitLock)
        {
            _consecutiveFailures = 0;
        }
    }

    private void RecordFailure()
    {
        lock (_circuitLock)
        {
            _consecutiveFailures++;
            if (_consecutiveFailures >= _options.CircuitBreakerFailureCount)
            {
                _circuitOpenUntil = DateTime.UtcNow.AddSeconds(_options.CircuitBreakerCooldownSeconds);
                _logger.LogWarning("AI circuit breaker opened until {OpenUntil} after {Failures} consecutive failures",
                    _circuitOpenUntil, _consecutiveFailures);
            }
        }
    }

    private static string ComputeHash(string input)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    private static int EstimateTokens(string text)
    {
        // Rough estimate: ~4 chars per token
        return text.Length / 4;
    }

    // No unmanaged resources to dispose.
}
