using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using API.Data;
using API.Models;
using Microsoft.EntityFrameworkCore;

namespace API.Services.AI;

public interface IAiRecommendationService
{
    /// <summary>
    /// Tạo analysis job và recommendation cho một domain cụ thể.
    /// </summary>
    Task<AiRecommendationResult> AnalyzeAsync(
        string domain, string entityType, string entityId,
        string jobType, Dictionary<string, string> inputData,
        int? requestedByUserId, string? correlationId = null);

    /// <summary>
    /// Lấy recommendations theo domain và entity.
    /// </summary>
    Task<List<Models.AiRecommendation>> GetRecommendationsAsync(
        string domain, string entityType, string entityId,
        int? limit = 10);

    /// <summary>
    /// Ghi feedback cho một recommendation.
    /// </summary>
    Task RecordFeedbackAsync(long recommendationId, int userId, string feedbackType, string? comment = null);

    /// <summary>
    /// Cập nhật trạng thái recommendation (review/approve/reject).
    /// </summary>
    Task UpdateStatusAsync(long recommendationId, string status, int? reviewedByUserId = null);
}

public class AiRecommendationResult
{
    public long AnalysisJobId { get; set; }
    public long? RecommendationId { get; set; }
    public string Summary { get; set; } = string.Empty;
    public string Provider { get; set; } = "Disabled";
    public bool IsFallback { get; set; }
}

public class AiRecommendationService : IAiRecommendationService
{
    private readonly ApplicationDbContext _db;
    private readonly IAiGateway _gateway;
    private readonly IAiPromptTemplateService _promptService;
    private readonly IAiRedactionService _redactionService;
    private readonly ILogger<AiRecommendationService> _logger;

    public AiRecommendationService(
        ApplicationDbContext db,
        IAiGateway gateway,
        IAiPromptTemplateService promptService,
        IAiRedactionService redactionService,
        ILogger<AiRecommendationService> logger)
    {
        _db = db;
        _gateway = gateway;
        _promptService = promptService;
        _redactionService = redactionService;
        _logger = logger;
    }

    public async Task<AiRecommendationResult> AnalyzeAsync(
        string domain, string entityType, string entityId,
        string jobType, Dictionary<string, string> inputData,
        int? requestedByUserId, string? correlationId = null)
    {
        var inputSummary = JsonSerializer.Serialize(new
        {
            domain,
            entityType,
            entityId,
            inputKeys = inputData.Keys.ToList(),
            inputLengths = inputData.ToDictionary(kv => kv.Key, kv => kv.Value.Length)
        });

        // 1. Tạo AnalysisJob
        var job = new AiAnalysisJob
        {
            JobType = jobType,
            Status = "Running",
            RequestedByUserId = requestedByUserId,
            CorrelationId = correlationId ?? Guid.NewGuid().ToString("N"),
            InputSummary = inputSummary,
            StartedAtUtc = DateTime.UtcNow
        };
        _db.AiAnalysisJobs.Add(job);
        await _db.SaveChangesAsync();

        try
        {
            // 2. Build prompt từ template
            var templateKey = ResolveTemplateKey(domain, entityType);
            var promptText = _promptService.Render(templateKey, 1, inputData);
            var systemInstruction = _promptService.GetSystemInstruction(domain);

            // 3. Redact PII trước khi gửi
            var (redactedPrompt, redactedFields) = _redactionService.Redact(promptText);
            if (redactedFields.Count > 0)
            {
                _logger.LogInformation("Redacted {Count} sensitive fields for {Domain}/{EntityType}",
                    redactedFields.Count, domain, entityType);
            }

            var parameters = new Dictionary<string, string>(inputData)
            {
                ["prompt"] = redactedPrompt,
                ["system_instruction"] = systemInstruction
            };

            var inputHash = ComputeHash(redactedPrompt);

            // 4. Gọi AI Gateway
            var aiRequest = new AiModelRequest
            {
                PromptTemplateKey = templateKey,
                PromptTemplateVersion = 1,
                Parameters = parameters,
                InputHash = inputHash,
                CorrelationId = job.CorrelationId
            };

            var aiResponse = await _gateway.ExecuteAsync(aiRequest);

            // 5. Ghi ModelRun
            _db.AiModelRuns.Add(new AiModelRun
            {
                AnalysisJobId = job.Id,
                Provider = aiResponse.Provider,
                Model = aiResponse.Model,
                PromptTemplateKey = templateKey,
                PromptTemplateVersion = 1,
                InputHash = inputHash,
                OutputHash = aiResponse.OutputHash,
                LatencyMs = aiResponse.LatencyMs,
                TokenEstimate = aiResponse.TokenEstimate
            });

            // 6. Tạo recommendation
            var recommendation = new AiRecommendation
            {
                AnalysisJobId = job.Id,
                Domain = domain,
                EntityType = entityType,
                EntityId = entityId,
                Severity = ExtractSeverity(aiResponse.OutputText),
                Confidence = 0.5m,
                Title = $"[AI Phan tich] {domain}/{entityType}",
                Summary = aiResponse.OutputText.Length > 2000
                    ? aiResponse.OutputText[..2000]
                    : aiResponse.OutputText,
                ReasoningSummary = aiResponse.IsFallback
                    ? "AI provider khong kha dung. Su dung phan tich deterministic."
                    : $"Phan tich tu provider: {aiResponse.Provider}, model: {aiResponse.Model}",
                RecommendedAction = ExtractAction(aiResponse.OutputText),
                RequiresHumanApproval = true,
                RequiresStepUp = false,
                Status = "Draft"
            };

            _db.AiRecommendations.Add(recommendation);
            await _db.SaveChangesAsync();

            // 7. Cập nhật job status
            job.Status = "Completed";
            job.CompletedAtUtc = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            return new AiRecommendationResult
            {
                AnalysisJobId = job.Id,
                RecommendationId = recommendation.Id,
                Summary = recommendation.Summary,
                Provider = aiResponse.Provider,
                IsFallback = aiResponse.IsFallback
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "AI analysis failed for {Domain}/{EntityType}/{EntityId}", domain, entityType, entityId);

            job.Status = "Failed";
            job.ErrorCode = ex.GetType().Name;
            job.CompletedAtUtc = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            // Vẫn tạo recommendation fallback
            var fallbackRec = new AiRecommendation
            {
                AnalysisJobId = job.Id,
                Domain = domain,
                EntityType = entityType,
                EntityId = entityId,
                Severity = "Medium",
                Confidence = 0.1m,
                Title = $"[Loi phan tich] {domain}/{entityType}",
                Summary = $"Khong the phan tich: {ex.Message}. Vui long thu lai hoac kiem tra cau hinh AI provider.",
                ReasoningSummary = $"Loi: {ex.GetType().Name}",
                RequiresHumanApproval = false,
                Status = "Draft"
            };

            _db.AiRecommendations.Add(fallbackRec);
            await _db.SaveChangesAsync();

            return new AiRecommendationResult
            {
                AnalysisJobId = job.Id,
                RecommendationId = fallbackRec.Id,
                Summary = fallbackRec.Summary,
                Provider = "Error",
                IsFallback = true
            };
        }
    }

    public async Task<List<Models.AiRecommendation>> GetRecommendationsAsync(
        string domain, string entityType, string entityId, int? limit = 10)
    {
        var query = _db.AiRecommendations
            .AsNoTracking()
            .Where(r => r.Domain == domain
                && r.EntityType == entityType
                && r.EntityId == entityId)
            .Include(r => r.Evidence)
            .OrderByDescending(r => r.CreatedAtUtc);

        if (limit.HasValue)
            return await query.Take(limit.Value).ToListAsync();

        return await query.ToListAsync();
    }

    public async Task RecordFeedbackAsync(long recommendationId, int userId, string feedbackType, string? comment = null)
    {
        _db.AiFeedbacks.Add(new AiFeedback
        {
            RecommendationId = recommendationId,
            UserId = userId,
            FeedbackType = feedbackType,
            Comment = comment
        });

        await _db.SaveChangesAsync();
    }

    public async Task UpdateStatusAsync(long recommendationId, string status, int? reviewedByUserId = null)
    {
        var recommendation = await _db.AiRecommendations.FindAsync(recommendationId);
        if (recommendation == null)
            throw new KeyNotFoundException($"Recommendation {recommendationId} not found.");

        recommendation.Status = status;
        recommendation.ReviewedByUserId = reviewedByUserId;
        recommendation.ReviewedAtUtc = DateTime.UtcNow;

        if (status == "Executed")
            recommendation.ExecutedAtUtc = DateTime.UtcNow;

        await _db.SaveChangesAsync();
    }

    private static string ResolveTemplateKey(string domain, string entityType)
    {
        return (domain, entityType) switch
        {
            ("soc", "incident") => "soc-incident-briefing",
            ("soc", "alarm") => "soc-incident-briefing",
            ("evidence", "case") => "evidence-analysis",
            ("evidence", "evidence_item") => "evidence-analysis",
            ("ueba", "employee") => "ueba-risk-explanation",
            ("device", "device") => "device-health-diagnosis",
            ("visitor", "visitor") => "visitor-screening",
            ("visitor", "vehicle") => "visitor-screening",
            ("policy", "policy") => "policy-explanation",
            _ => "soc-incident-briefing"
        };
    }

    private static string ExtractSeverity(string text)
    {
        if (text.Contains("Critical", StringComparison.OrdinalIgnoreCase)) return "Critical";
        if (text.Contains("High", StringComparison.OrdinalIgnoreCase)) return "High";
        if (text.Contains("Medium", StringComparison.OrdinalIgnoreCase)) return "Medium";
        if (text.Contains("Low", StringComparison.OrdinalIgnoreCase)) return "Low";
        return "Medium";
    }

    private static string? ExtractAction(string text)
    {
        // Tìm action khuyến nghị từ output
        var lines = text.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        foreach (var line in lines)
        {
            var trimmed = line.Trim();
            if (trimmed.StartsWith("- ") || trimmed.StartsWith("* "))
            {
                var action = trimmed[2..].Trim();
                if (action.Length > 20 && action.Length < 200)
                    return action;
            }
        }
        return null;
    }

    private static string ComputeHash(string input)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}
