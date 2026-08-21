using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using API.Services.AI;
using Microsoft.Extensions.Options;

namespace API.Services.Agent;

public sealed record LlmToolCall(string Id, string Name, JsonObject Arguments);
public sealed record LlmUsage(long PromptTokens, long CompletionTokens, long CacheHitTokens, long CacheMissTokens);

public sealed class LlmResponse
{
    public string? Content { get; init; }
    public IReadOnlyList<LlmToolCall> ToolCalls { get; init; } = Array.Empty<LlmToolCall>();
    public LlmUsage Usage { get; init; } = new(0, 0, 0, 0);
    public bool IsError { get; init; }
    public string? Error { get; init; }
}

/// <summary>Client gọi chat completions (OpenAI-compatible, DeepSeek) hỗ trợ tools.</summary>
public sealed class AgentLlmClient
{
    private readonly AiProviderOptions _options;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<AgentLlmClient> _logger;

    public AgentLlmClient(IOptions<AiProviderOptions> options, IHttpClientFactory httpClientFactory, ILogger<AgentLlmClient> logger)
    {
        _options = options.Value;
        _httpClientFactory = httpClientFactory;
        _logger = logger;

        var envEndpoint = Environment.GetEnvironmentVariable("VSHIELD_AI_ENDPOINT");
        var envApiKey = Environment.GetEnvironmentVariable("VSHIELD_AI_API_KEY");
        var envProvider = Environment.GetEnvironmentVariable("VSHIELD_AI_PROVIDER");
        if (!string.IsNullOrWhiteSpace(envEndpoint)) _options.Endpoint = envEndpoint;
        if (!string.IsNullOrWhiteSpace(envApiKey)) _options.ApiKey = envApiKey;
        if (!string.IsNullOrWhiteSpace(envProvider)) _options.Provider = envProvider;
    }

    public bool IsAvailable()
    {
        return !_options.Provider.Equals("Disabled", StringComparison.OrdinalIgnoreCase)
            && !string.IsNullOrWhiteSpace(_options.Endpoint)
            && !string.IsNullOrWhiteSpace(_options.ApiKey);
    }

    public async Task<LlmResponse> CompleteAsync(
        List<object> messages,
        List<object>? tools,
        int maxTokens,
        string? toolChoice = null,
        CancellationToken cancellationToken = default)
    {
        if (!IsAvailable())
        {
            return new LlmResponse
            {
                IsError = true,
                Error = "AI chưa được cấu hình. Vui lòng thiết lập AiProvider (endpoint, key, model)."
            };
        }

        var payload = new Dictionary<string, object?>
        {
            ["model"] = _options.Model,
            ["stream"] = false,
            ["temperature"] = 0.6,
            ["max_tokens"] = maxTokens,
            ["messages"] = messages
        };
        if (tools is { Count: > 0 })
        {
            payload["tools"] = tools;
            payload["tool_choice"] = string.IsNullOrWhiteSpace(toolChoice) ? "auto" : toolChoice;
        }

        var client = _httpClientFactory.CreateClient("AiChat");
        var request = new HttpRequestMessage(HttpMethod.Post, _options.Endpoint)
        {
            Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json")
        };
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _options.ApiKey);

        HttpResponseMessage response;
        try
        {
            response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "AgentLlm: không gọi được provider");
            return new LlmResponse { IsError = true, Error = $"Không kết nối được AI provider: {ex.Message}" };
        }

        var raw = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("AgentLlm: provider lỗi {Status}: {Body}", (int)response.StatusCode, raw);
            return new LlmResponse { IsError = true, Error = $"AI provider lỗi ({(int)response.StatusCode})" };
        }

        try
        {
            using var doc = JsonDocument.Parse(raw);
            var root = doc.RootElement;

            var message = root.GetProperty("choices")[0].GetProperty("message");
            string? content = message.TryGetProperty("content", out var c) && c.ValueKind == JsonValueKind.String ? c.GetString() : null;

            var toolCalls = new List<LlmToolCall>();
            if (message.TryGetProperty("tool_calls", out var tc) && tc.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in tc.EnumerateArray())
                {
                    var id = item.GetProperty("id").GetString() ?? "";
                    var name = item.GetProperty("function").GetProperty("name").GetString() ?? "";
                    var argsText = item.GetProperty("function").GetProperty("arguments").GetString() ?? "{}";
                    JsonObject args;
                    try { args = JsonNode.Parse(argsText) as JsonObject ?? new JsonObject(); }
                    catch { args = new JsonObject(); }
                    toolCalls.Add(new LlmToolCall(id, name, args));
                }
            }

            long prompt = 0, completion = 0, cacheHit = 0, cacheMiss = 0;
            if (root.TryGetProperty("usage", out var usage))
            {
                prompt = usage.TryGetProperty("prompt_tokens", out var p) ? p.GetInt64() : 0;
                completion = usage.TryGetProperty("completion_tokens", out var co) ? co.GetInt64() : 0;
                if (usage.TryGetProperty("prompt_cache_hit_tokens", out var ch)) cacheHit = ch.GetInt64();
                if (usage.TryGetProperty("prompt_cache_miss_tokens", out var cm)) cacheMiss = cm.GetInt64();
            }

            return new LlmResponse
            {
                Content = content,
                ToolCalls = toolCalls,
                Usage = new LlmUsage(prompt, completion, cacheHit, cacheMiss)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "AgentLlm: parse response thất bại");
            return new LlmResponse { IsError = true, Error = "Phản hồi AI không đúng định dạng." };
        }
    }
}