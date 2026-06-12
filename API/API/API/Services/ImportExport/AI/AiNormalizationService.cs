using System.Text;
using System.Text.Json;
using API.DTOs;

namespace API.Services.ImportExport.AI;

public class AiNormalizationService : IAiNormalizationService
{
    private readonly Validation.SynonymRegistry _registry;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AiNormalizationService> _logger;

    public AiNormalizationService(
        Validation.SynonymRegistry registry,
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ILogger<AiNormalizationService> logger)
    {
        _registry = registry;
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
        _logger = logger;
    }

    public bool IsAvailable()
    {
        var apiKey = Environment.GetEnvironmentVariable("VSHIELD_AI_API_KEY")
                     ?? _configuration["AiImport:Llm:ApiKey"];
        return !string.IsNullOrEmpty(apiKey);
    }

    public async Task<AiProcessingResult> NormalizeAsync(
        FileParseResult data,
        IEntityImportHandler handler,
        List<SynonymIssue> detectedIssues,
        CancellationToken ct = default)
    {
        var changes = new List<SynonymChangeLog>();
        var normalizedRows = new List<Dictionary<string, object?>>();

        var llmResult = await TryLlmNormalizationAsync(data, handler, detectedIssues, ct);
        if (llmResult != null)
            return llmResult;

        return await RuleBasedNormalizeAsync(data, handler, detectedIssues, changes, normalizedRows);
    }

    public async Task<FileParseResult> ParseOcrTextAsync(
        string rawText,
        IEntityImportHandler handler,
        CancellationToken ct = default)
    {
        var llmResult = await TryLlmOcrParseAsync(rawText, handler, ct);
        if (llmResult != null)
            return llmResult;

        return FallbackOcrParse(rawText, handler);
    }

    private async Task<AiProcessingResult?> TryLlmNormalizationAsync(
        FileParseResult data,
        IEntityImportHandler handler,
        List<SynonymIssue> detectedIssues,
        CancellationToken ct)
    {
        if (!IsAvailable()) return null;

        try
        {
            var fields = handler.GetTemplateFields();
            var prompt = BuildNormalizationPrompt(data, fields, detectedIssues);
            var response = await CallLlmAsync(prompt, ct);

            if (response == null) return null;

            var llmResult = JsonSerializer.Deserialize<LlmNormalizationResponse>(response,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (llmResult?.Rows == null) return null;

            var result = new AiProcessingResult
            {
                Status = "success",
                NormalizedData = new FileParseResult
                {
                    Headers = data.Headers.ToList(),
                    Rows = llmResult.Rows,
                },
                Changes = llmResult.Changes ?? [],
            };

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogWarning("LLM normalization failed: {Message}, falling back to rules", ex.Message);
            return null;
        }
    }

    private async Task<FileParseResult?> TryLlmOcrParseAsync(
        string rawText,
        IEntityImportHandler handler,
        CancellationToken ct)
    {
        if (!IsAvailable()) return null;

        try
        {
            var fields = handler.GetTemplateFields();
            var prompt = BuildOcrPrompt(rawText, fields);
            var response = await CallLlmAsync(prompt, ct);
            if (response == null) return null;

            var llmResult = JsonSerializer.Deserialize<LlmOcrResponse>(response,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (llmResult?.Rows == null) return null;

            var headers = llmResult.Rows.Count > 0
                ? llmResult.Rows[0].Keys.ToList()
                : fields.Select(f => f.FieldName).ToList();

            return new FileParseResult { Headers = headers, Rows = llmResult.Rows };
        }
        catch (Exception ex)
        {
            _logger.LogWarning("LLM OCR parse failed: {Message}", ex.Message);
            return null;
        }
    }

    private Task<AiProcessingResult> RuleBasedNormalizeAsync(
        FileParseResult data,
        IEntityImportHandler handler,
        List<SynonymIssue> detectedIssues,
        List<SynonymChangeLog> changes,
        List<Dictionary<string, object?>> normalizedRows)
    {
        var handlerObj = handler;
        var fields = handlerObj.GetTemplateFields();
        var fieldNames = fields.Select(f => f.FieldName).ToList();

        var headerMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var h in data.Headers)
        {
            var match = fieldNames.FirstOrDefault(f =>
                string.Equals(f, h, StringComparison.OrdinalIgnoreCase));
            if (match != null)
                headerMap[h] = match;
            else
            {
                var std = _registry.FindStandard(h);
                if (std != null && fieldNames.Contains(std, StringComparer.OrdinalIgnoreCase))
                    headerMap[h] = std;
                else
                    headerMap[h] = h;
            }
        }

        foreach (var row in data.Rows)
        {
            var normalizedRow = new Dictionary<string, object?>();
            foreach (var (originalCol, standardCol) in headerMap)
            {
                if (!row.TryGetValue(originalCol, out var val))
                    continue;

                var strVal = val?.ToString();
                if (string.IsNullOrEmpty(strVal))
                {
                    normalizedRow[standardCol] = val;
                    continue;
                }

                var field = fields.FirstOrDefault(f =>
                    string.Equals(f.FieldName, standardCol, StringComparison.OrdinalIgnoreCase));
                if (field == null)
                {
                    normalizedRow[standardCol] = val;
                    continue;
                }

                var originalStr = strVal;

                if (field.AllowedValues is { Count: > 0 })
                {
                    var exact = field.AllowedValues.FirstOrDefault(av =>
                        string.Equals(av, strVal, StringComparison.OrdinalIgnoreCase));
                    if (exact != null)
                    {
                        if (exact != strVal)
                            changes.Add(new SynonymChangeLog { Reason = "case", OriginalValue = strVal, NormalizedValue = exact });
                        normalizedRow[standardCol] = exact;
                        continue;
                    }

                    var std = _registry.FindStandard(strVal);
                    if (std != null)
                    {
                        var matchedAllowed = field.AllowedValues.FirstOrDefault(av =>
                            string.Equals(av, std, StringComparison.OrdinalIgnoreCase));
                        if (matchedAllowed != null)
                        {
                            changes.Add(new SynonymChangeLog { Reason = "synonym", OriginalValue = originalStr, NormalizedValue = matchedAllowed });
                            normalizedRow[standardCol] = matchedAllowed;
                            continue;
                        }
                    }
                }

                if (field.DataType == "bool")
                {
                    var boolStd = _registry.FindStandard(strVal);
                    if (boolStd is "true" or "false")
                    {
                        if (boolStd != strVal)
                            changes.Add(new SynonymChangeLog { Reason = "boolean_synonym", OriginalValue = originalStr, NormalizedValue = boolStd });
                        normalizedRow[standardCol] = boolStd;
                        continue;
                    }
                }

                var fieldStd = _registry.FindStandard(strVal);
                if (fieldStd != null && fieldStd != strVal)
                {
                    changes.Add(new SynonymChangeLog { Reason = "synonym", OriginalValue = originalStr, NormalizedValue = fieldStd });
                    normalizedRow[standardCol] = fieldStd;
                    continue;
                }

                normalizedRow[standardCol] = val;
            }

            normalizedRows.Add(normalizedRow);
        }

        return Task.FromResult(new AiProcessingResult
        {
            Status = "success",
            NormalizedData = new FileParseResult
            {
                Headers = data.Headers.Select(h => headerMap.GetValueOrDefault(h, h)).ToList(),
                Rows = normalizedRows,
            },
            Changes = changes,
        });
    }

    private FileParseResult FallbackOcrParse(string rawText, IEntityImportHandler handler)
    {
        var result = new FileParseResult();
        var lines = rawText.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        if (lines.Length == 0) return result;

        var headers = lines[0].Split(['\t', ','], StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        result.Headers = headers.ToList();

        for (int i = 1; i < lines.Length; i++)
        {
            var fields = lines[i].Split(['\t', ','], StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
            if (fields.Length == 0) continue;

            var row = new Dictionary<string, object?>();
            for (int j = 0; j < Math.Min(fields.Length, headers.Length); j++)
                row[headers[j]] = fields[j];
            result.Rows.Add(row);
        }

        return result;
    }

    private string BuildNormalizationPrompt(FileParseResult data, List<TemplateFieldInfo> fields, List<SynonymIssue> issues)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Bạn là trợ lý chuẩn hóa dữ liệu. Nhiệm vụ: chuẩn hóa dữ liệu theo schema sau:");
        sb.AppendLine();
        sb.AppendLine("SCHEMA:");
        foreach (var f in fields)
        {
            sb.AppendLine($"  - {f.FieldName} ({f.DataType}){(f.IsRequired ? " [REQUIRED]" : "")}{(f.AllowedValues is { Count: > 0 } ? $" Allowed: {string.Join(", ", f.AllowedValues)}" : "")}");
        }
        sb.AppendLine();

        if (issues.Count > 0)
        {
            sb.AppendLine("CÁC VẤN ĐỀ PHÁT HIỆN (cần chuẩn hóa):");
            foreach (var grp in issues.GroupBy(i => i.Category))
            {
                sb.AppendLine($"  - {grp.Key}: {grp.Count()} issues");
                foreach (var i in grp.Take(5))
                    sb.AppendLine($"    Dòng {i.Row}, cột '{i.Column}': '{i.OriginalValue}' → '{i.SuggestedValue}'");
            }
            sb.AppendLine();
        }

        sb.AppendLine("DỮ LIỆU GỐC (dạng JSON list of objects):");
        sb.AppendLine(JsonSerializer.Serialize(data.Rows.Take(50)));
        sb.AppendLine();

        sb.AppendLine("YÊU CẦU: Trả về JSON với format:");
        sb.AppendLine("{ \"rows\": [<dữ liệu đã chuẩn hóa>], \"changes\": [{\"row\": N, \"column\": \"...\", \"originalValue\": \"...\", \"normalizedValue\": \"...\", \"reason\": \"...\"}] }");
        sb.AppendLine("Chỉ trả về JSON, không kèm giải thích.");

        return sb.ToString();
    }

    private string BuildOcrPrompt(string rawText, List<TemplateFieldInfo> fields)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Bạn là trợ lý trích xuất dữ liệu từ văn bản OCR. Nhiệm vụ: parse văn bản sau thành cấu trúc JSON theo schema:");
        sb.AppendLine();
        sb.AppendLine("SCHEMA (các cột cần extract):");
        foreach (var f in fields)
            sb.AppendLine($"  - {f.FieldName} ({f.DataType}){(f.IsRequired ? " [REQUIRED]" : "")}{(f.AllowedValues is { Count: > 0 } ? $" Allowed: {string.Join(", ", f.AllowedValues)}" : "")}");
        sb.AppendLine();
        sb.AppendLine("VĂN BẢN GỐC:");
        sb.AppendLine(rawText.Length > 10000 ? rawText[..10000] + "..." : rawText);
        sb.AppendLine();
        sb.AppendLine("YÊU CẦU: Trả về JSON list of objects theo schema. Chuẩn hóa giá trị đúng theo Allowed values. Chỉ trả về JSON array, không kèm giải thích.");

        return sb.ToString();
    }

    private async Task<string?> CallLlmAsync(string prompt, CancellationToken ct)
    {
        var apiKey = Environment.GetEnvironmentVariable("VSHIELD_AI_API_KEY")
                     ?? _configuration["AiImport:Llm:ApiKey"];
        var endpoint = _configuration["AiImport:Llm:Endpoint"] ?? "https://api.openai.com/v1/chat/completions";
        var model = _configuration["AiImport:Llm:Model"] ?? "gpt-4o-mini";

        if (string.IsNullOrEmpty(apiKey)) return null;

        try
        {
            var client = _httpClientFactory.CreateClient("AiLlm");
            var requestBody = new
            {
                model,
                messages = new[] {
                    new { role = "system", content = "Bạn là chuyên gia xử lý dữ liệu. Chỉ trả về JSON." },
                    new { role = "user", content = prompt }
                },
                temperature = 0.1,
                max_tokens = 4096,
            };

            var json = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var request = new HttpRequestMessage(HttpMethod.Post, endpoint);
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);
            request.Content = content;

            using var response = await client.SendAsync(request, ct);
            response.EnsureSuccessStatusCode();

            var responseJson = await response.Content.ReadAsStringAsync(ct);
            var llmResponse = JsonSerializer.Deserialize<LlmApiResponse>(responseJson,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            return llmResponse?.Choices?.FirstOrDefault()?.Message?.Content;
        }
        catch (Exception ex)
        {
            _logger.LogError("LLM API call failed: {Message}", ex.Message);
            return null;
        }
    }

    private class LlmNormalizationResponse
    {
        public List<Dictionary<string, object?>> Rows { get; set; } = [];
        public List<SynonymChangeLog> Changes { get; set; } = [];
    }

    private class LlmOcrResponse
    {
        public List<Dictionary<string, object?>> Rows { get; set; } = [];
    }

    private class LlmApiResponse
    {
        public List<LlmChoice>? Choices { get; set; }
    }

    private class LlmChoice
    {
        public LlmMessage? Message { get; set; }
    }

    private class LlmMessage
    {
        public string? Content { get; set; }
    }
}
