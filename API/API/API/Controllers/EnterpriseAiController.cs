using System.Security.Claims;
using API.Data;
using API.Middleware;
using API.Services;
using API.Services.AI;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace API.Controllers;

[ApiController]
[Route("api/enterprise/ai")]
[Authorize]
[RequireOperationalTask("monitoring")]
public class EnterpriseAiController : ControllerBase
{
    private readonly IAiRecommendationService _recommendationService;
    private readonly ApplicationDbContext _context;
    private readonly IPolicySimulationService _policySim;
    private readonly INaturalLanguageQueryService _nlQuery;
    private readonly ILogger<EnterpriseAiController> _logger;

    public EnterpriseAiController(
        IAiRecommendationService recommendationService,
        ApplicationDbContext context,
        IPolicySimulationService policySim,
        INaturalLanguageQueryService nlQuery,
        ILogger<EnterpriseAiController> logger)
    {
        _recommendationService = recommendationService;
        _context = context;
        _policySim = policySim;
        _nlQuery = nlQuery;
        _logger = logger;
    }

    /// <summary>
    /// POST /api/enterprise/ai/analyze - Phân tích AI cho một entity
    /// </summary>
    [HttpPost("analyze")]
    public async Task<IActionResult> Analyze([FromBody] AiAnalysisRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Domain))
            return BadRequest(new { message = "Vui lòng nhập Domain." });
        if (string.IsNullOrWhiteSpace(request.EntityType))
            return BadRequest(new { message = "Vui lòng nhập loại đối tượng." });
        if (string.IsNullOrWhiteSpace(request.EntityId))
            return BadRequest(new { message = "Vui lòng nhập EntityId." });

        var result = await _recommendationService.AnalyzeAsync(
            request.Domain,
            request.EntityType,
            request.EntityId,
            request.JobType ?? $"{request.Domain}-{request.EntityType}-analysis",
            request.InputData ?? new Dictionary<string, string>(),
            GetCurrentUserId(),
            request.CorrelationId);

        return Ok(result);
    }

    /// <summary>
    /// GET /api/enterprise/ai/recommendations?domain=...&entityType=...&entityId=...
    /// </summary>
    [HttpGet("recommendations")]
    public async Task<IActionResult> GetRecommendations(
        [FromQuery] string domain,
        [FromQuery] string entityType,
        [FromQuery] string entityId,
        [FromQuery] int? limit = 10)
    {
        if (string.IsNullOrWhiteSpace(domain))
            return BadRequest(new { message = "Vui lòng nhập Domain." });
        if (string.IsNullOrWhiteSpace(entityType))
            return BadRequest(new { message = "Vui lòng nhập loại đối tượng." });
        if (string.IsNullOrWhiteSpace(entityId))
            return BadRequest(new { message = "Vui lòng nhập EntityId." });

        var recommendations = await _recommendationService.GetRecommendationsAsync(
            domain, entityType, entityId, limit);

        return Ok(recommendations);
    }

    /// <summary>
    /// POST /api/enterprise/ai/recommendations/{id}/review
    /// </summary>
    [HttpPatch("recommendations/{id:long}/review")]
    public async Task<IActionResult> ReviewRecommendation(long id, [FromBody] ReviewRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Status))
            return BadRequest(new { message = "Vui lòng nhập trạng thái (Approved/Rejected/Executed)." });

        try
        {
            await _recommendationService.UpdateStatusAsync(id, request.Status, GetCurrentUserId());

            if (!string.IsNullOrWhiteSpace(request.Comment))
            {
                await _recommendationService.RecordFeedbackAsync(
                    id, GetCurrentUserId() ?? 0,
                    request.Status == "Approved" ? "Useful" : "Wrong",
                    request.Comment);
            }

            return Ok(new { message = $"Đề xuất đã được cập nhật trạng thái {request.Status} thành công." });
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { message = "Không tìm thấy đề xuất." });
        }
    }

    /// <summary>
    /// POST /api/enterprise/ai/recommendations/{id}/feedback
    /// </summary>
    [HttpPost("recommendations/{id:long}/feedback")]
    public async Task<IActionResult> SubmitFeedback(long id, [FromBody] FeedbackRequest request)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
            return Unauthorized();

        await _recommendationService.RecordFeedbackAsync(
            id, userId.Value, request.FeedbackType, request.Comment);

        return Ok(new { message = "Đã ghi nhận phản hồi." });
    }

    /// <summary>
    /// POST /api/enterprise/ai/policies/{policyVersionId}/simulate - AI mo phong chinh sach
    /// </summary>
    [HttpPost("policies/{policyVersionId:int}/simulate")]
    public async Task<IActionResult> SimulatePolicy(int policyVersionId)
    {
        try
        {
            var result = await _policySim.SimulatePolicyAsync(policyVersionId, GetCurrentUserId());
            return Ok(result);
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { message = "Không tìm thấy phiên bản chính sách." });
        }
    }

    /// <summary>
    /// POST /api/enterprise/ai/policies/{policyVersionId}/explain - AI giai thich chinh sach
    /// </summary>
    [HttpPost("policies/{policyVersionId:int}/explain")]
    public async Task<IActionResult> ExplainPolicy(int policyVersionId)
    {
        try
        {
            var result = await _policySim.ExplainPolicyAsync(policyVersionId, GetCurrentUserId());
            return Ok(result);
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { message = "Không tìm thấy phiên bản chính sách." });
        }
    }

    /// <summary>
    /// POST /api/enterprise/ai/query - Truy van bao mat bang ngon ngu tu nhien
    /// </summary>
    [HttpPost("query")]
    public async Task<IActionResult> QueryNaturalLanguage([FromBody] NlQueryRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Query))
            return BadRequest(new { message = "Vui lòng nhập truy vấn." });

        var userId = GetCurrentUserId();
        var result = await _nlQuery.QueryAsync(request.Query.Trim(), userId);

        if (result.Intent == "blocked_injection")
        {
            _logger.LogWarning("NL query blocked as injection from user {UserId}: {Query}", userId, request.Query);
            return BadRequest(new { message = "Truy vấn chứa nội dung không hợp lệ." });
        }

        return Ok(result);
    }

    /// <summary>
    /// POST /api/enterprise/ai/event-metadata/ingest - Tiep nhan su kien metadata
    /// </summary>
    [HttpPost("event-metadata/ingest")]
    public async Task<IActionResult> IngestEventMetadata([FromBody] IngestEventRequest request)
    {
        var metadata = new API.Models.AiEventMetadata
        {
            SourceType = request.SourceType,
            SourceId = request.SourceId,
            EventType = request.EventType,
            OccurredAtUtc = request.OccurredAtUtc ?? DateTime.UtcNow,
            SiteId = request.SiteId,
            ZoneId = request.ZoneId,
            CameraId = request.CameraId,
            GateId = request.GateId,
            SubjectType = request.SubjectType,
            SubjectId = request.SubjectId,
            ObjectType = request.ObjectType,
            Label = request.Label,
            Confidence = request.Confidence,
            ModelName = request.ModelName,
            ModelVersion = request.ModelVersion,
            RawMetadataJson = request.RawMetadataJson,
            CorrelationId = request.CorrelationId ?? Guid.NewGuid().ToString("N")
        };

        _context.AiEventMetadataSet.Add(metadata);
        await _context.SaveChangesAsync();
        return Ok(new { metadata.Id, metadata.CorrelationId });
    }

    /// <summary>
    /// GET /api/enterprise/ai/event-metadata/search - Tim kiem su kien metadata
    /// </summary>
    [HttpGet("event-metadata/search")]
    public async Task<IActionResult> SearchEventMetadata(
        [FromQuery] string? sourceType,
        [FromQuery] string? eventType,
        [FromQuery] int? siteId,
        [FromQuery] int? cameraId,
        [FromQuery] int? gateId,
        [FromQuery] string? subjectId,
        [FromQuery] string? correlationId,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] int limit = 50)
    {
        var query = _context.AiEventMetadataSet.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(sourceType))
            query = query.Where(m => m.SourceType == sourceType);
        if (!string.IsNullOrWhiteSpace(eventType))
            query = query.Where(m => m.EventType == eventType);
        if (siteId.HasValue)
            query = query.Where(m => m.SiteId == siteId.Value);
        if (cameraId.HasValue)
            query = query.Where(m => m.CameraId == cameraId.Value);
        if (gateId.HasValue)
            query = query.Where(m => m.GateId == gateId.Value);
        if (!string.IsNullOrWhiteSpace(subjectId))
            query = query.Where(m => m.SubjectId == subjectId);
        if (!string.IsNullOrWhiteSpace(correlationId))
            query = query.Where(m => m.CorrelationId == correlationId);
        if (from.HasValue)
            query = query.Where(m => m.OccurredAtUtc >= from.Value);
        if (to.HasValue)
            query = query.Where(m => m.OccurredAtUtc <= to.Value);

        var results = await query
            .OrderByDescending(m => m.OccurredAtUtc)
            .Take(Math.Min(limit, 200))
            .ToListAsync();

        return Ok(results);
    }

    private int? GetCurrentUserId()
    {
        var userIdClaim = User.FindFirstValue(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub);
        return int.TryParse(userIdClaim, out var userId) ? userId : null;
    }

    public sealed record NlQueryRequest(string Query);

    public sealed record IngestEventRequest(
        string SourceType,
        string? SourceId,
        string EventType,
        DateTime? OccurredAtUtc,
        int? SiteId,
        int? ZoneId,
        int? CameraId,
        int? GateId,
        string? SubjectType,
        string? SubjectId,
        string? ObjectType,
        string? Label,
        decimal? Confidence,
        string? ModelName,
        string? ModelVersion,
        string? RawMetadataJson,
        string? CorrelationId);
}

public sealed record AiAnalysisRequest(
    string Domain,
    string EntityType,
    string EntityId,
    string? JobType,
    Dictionary<string, string>? InputData,
    string? CorrelationId);

public sealed record ReviewRequest(string Status, string? Comment);

public sealed record FeedbackRequest(string FeedbackType, string? Comment);
