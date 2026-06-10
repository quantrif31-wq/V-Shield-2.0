using System.Security.Claims;
using API.Data;
using API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace API.Controllers;

[ApiController]
[Route("api/enterprise/situational-awareness")]
[Authorize(Roles = "Admin,BaoVe")]
public class EnterpriseSituationalAwarenessController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public EnterpriseSituationalAwarenessController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet("overview")]
    public async Task<IActionResult> GetOverview()
    {
        return Ok(new
        {
            Events = await _context.SecurityEvents.CountAsync(),
            CriticalEvents = await _context.SecurityEvents.CountAsync(e => e.Severity == "Critical"),
            Correlations = await _context.EventCorrelations.CountAsync(),
            VideoBookmarks = await _context.VideoBookmarks.CountAsync(),
            SiteMaps = await _context.SiteMaps.CountAsync(),
            AiPendingReviews = await _context.AiAdjudicationItems.CountAsync(item => item.Status == "Pending"),
            AiMetrics = await _context.AiPerformanceMetrics.CountAsync()
        });
    }

    [HttpPost("events")]
    public async Task<IActionResult> CreateSecurityEvent([FromBody] SecurityEventRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.EventType))
            return BadRequest(new { message = "EventType is required." });

        var securityEvent = new SecurityEvent
        {
            SourceType = string.IsNullOrWhiteSpace(request.SourceType) ? "Access" : request.SourceType.Trim(),
            SourceId = request.SourceId?.Trim(),
            EventType = request.EventType.Trim(),
            Severity = string.IsNullOrWhiteSpace(request.Severity) ? "Info" : request.Severity.Trim(),
            SiteId = request.SiteId,
            SecurityZoneId = request.SecurityZoneId,
            AccessPointId = request.AccessPointId,
            SubjectType = request.SubjectType?.Trim(),
            SubjectId = request.SubjectId,
            VehicleId = request.VehicleId,
            PlateText = request.PlateText?.Trim(),
            Confidence = request.Confidence,
            CorrelationId = string.IsNullOrWhiteSpace(request.CorrelationId) ? Guid.NewGuid().ToString("N") : request.CorrelationId.Trim(),
            Summary = request.Summary?.Trim(),
            OccurredAtUtc = request.OccurredAtUtc ?? DateTime.UtcNow
        };

        _context.SecurityEvents.Add(securityEvent);
        await _context.SaveChangesAsync();

        if (securityEvent.EventType.Contains("Denied", StringComparison.OrdinalIgnoreCase) ||
            securityEvent.Severity is "High" or "Critical")
        {
            _context.EventCorrelations.Add(new EventCorrelation
            {
                CorrelationId = securityEvent.CorrelationId,
                RuleName = "Security event requires review",
                Severity = securityEvent.Severity,
                Summary = securityEvent.Summary ?? securityEvent.EventType
            });
            await _context.SaveChangesAsync();
        }

        return Ok(securityEvent);
    }

    [HttpPost("correlations/run")]
    public async Task<IActionResult> RunCorrelation([FromBody] CorrelationRunRequest request)
    {
        var since = request.SinceUtc ?? DateTime.UtcNow.AddMinutes(-15);
        var grouped = await _context.SecurityEvents
            .Where(e => e.OccurredAtUtc >= since)
            .GroupBy(e => e.CorrelationId)
            .Where(group => group.Count() >= request.MinimumEvents)
            .Select(group => new
            {
                CorrelationId = group.Key,
                Count = group.Count(),
                MaxSeverity = group.Max(e => e.Severity)
            })
            .ToListAsync();

        foreach (var group in grouped)
        {
            if (!await _context.EventCorrelations.AnyAsync(c => c.CorrelationId == group.CorrelationId && c.RuleName == "Multi-signal correlation"))
            {
                _context.EventCorrelations.Add(new EventCorrelation
                {
                    CorrelationId = group.CorrelationId,
                    RuleName = "Multi-signal correlation",
                    Severity = group.MaxSeverity ?? "Info",
                    Summary = $"{group.Count} events share the same correlation id."
                });
            }
        }

        await _context.SaveChangesAsync();
        return Ok(new { CreatedOrExisting = grouped.Count });
    }

    [HttpPost("video-bookmarks")]
    public async Task<IActionResult> CreateVideoBookmark([FromBody] VideoBookmarkRequest request)
    {
        if (request.EndUtc <= request.StartUtc)
            return BadRequest(new { message = "EndUtc must be after StartUtc." });

        var bookmark = new VideoBookmark
        {
            SecurityEventId = request.SecurityEventId,
            CameraId = request.CameraId,
            ArtifactReference = request.ArtifactReference?.Trim(),
            StartUtc = request.StartUtc,
            EndUtc = request.EndUtc,
            Note = request.Note?.Trim()
        };

        _context.VideoBookmarks.Add(bookmark);
        await _context.SaveChangesAsync();
        return Ok(bookmark);
    }

    [HttpPost("maps")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> CreateSiteMap([FromBody] SiteMapRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name) || string.IsNullOrWhiteSpace(request.AssetReference))
            return BadRequest(new { message = "Name and asset reference are required." });

        var map = new SiteMap
        {
            SiteId = request.SiteId,
            Name = request.Name.Trim(),
            AssetReference = request.AssetReference.Trim(),
            CoordinateSystem = string.IsNullOrWhiteSpace(request.CoordinateSystem) ? "Normalized" : request.CoordinateSystem.Trim()
        };

        _context.SiteMaps.Add(map);
        await _context.SaveChangesAsync();
        return Ok(map);
    }

    [HttpPost("maps/{mapId:int}/placements")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> AddMapPlacement(int mapId, [FromBody] MapPlacementRequest request)
    {
        if (!await _context.SiteMaps.AnyAsync(map => map.SiteMapId == mapId))
            return NotFound(new { message = "Map not found." });

        var placement = new MapDevicePlacement
        {
            SiteMapId = mapId,
            SecurityDeviceId = request.SecurityDeviceId,
            CameraId = request.CameraId,
            X = request.X,
            Y = request.Y,
            IconType = string.IsNullOrWhiteSpace(request.IconType) ? "Device" : request.IconType.Trim()
        };

        _context.MapDevicePlacements.Add(placement);
        await _context.SaveChangesAsync();
        return Ok(placement);
    }

    [HttpPost("ai-adjudications")]
    public async Task<IActionResult> CreateAiAdjudication([FromBody] AiAdjudicationRequest request)
    {
        var item = new AiAdjudicationItem
        {
            SecurityEventId = request.SecurityEventId,
            AiSource = string.IsNullOrWhiteSpace(request.AiSource) ? "Unknown" : request.AiSource.Trim(),
            ModelVersion = string.IsNullOrWhiteSpace(request.ModelVersion) ? "Unknown" : request.ModelVersion.Trim(),
            Confidence = request.Confidence,
            Status = "Pending"
        };

        _context.AiAdjudicationItems.Add(item);
        await _context.SaveChangesAsync();
        return Ok(item);
    }

    [HttpPatch("ai-adjudications/{itemId:int}/review")]
    public async Task<IActionResult> ReviewAiAdjudication(int itemId, [FromBody] AiReviewRequest request)
    {
        var item = await _context.AiAdjudicationItems.FindAsync(itemId);
        if (item == null)
            return NotFound(new { message = "AI adjudication item not found." });

        item.Status = "Reviewed";
        item.Outcome = string.IsNullOrWhiteSpace(request.Outcome) ? "Confirmed" : request.Outcome.Trim();
        item.ReviewNote = request.ReviewNote?.Trim();
        item.ReviewedAtUtc = DateTime.UtcNow;
        item.ReviewedByUserId = GetCurrentUserId();
        await _context.SaveChangesAsync();
        return Ok(item);
    }

    [HttpPost("ai-metrics")]
    public async Task<IActionResult> RecordAiMetric([FromBody] AiMetricRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.MetricName))
            return BadRequest(new { message = "MetricName is required." });

        var metric = new AiPerformanceMetric
        {
            AiSource = string.IsNullOrWhiteSpace(request.AiSource) ? "Unknown" : request.AiSource.Trim(),
            MetricName = request.MetricName.Trim(),
            MetricValue = request.MetricValue,
            Notes = request.Notes?.Trim()
        };

        _context.AiPerformanceMetrics.Add(metric);
        await _context.SaveChangesAsync();
        return Ok(metric);
    }

    private int? GetCurrentUserId()
    {
        var userIdClaim = User.FindFirstValue(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub);
        return int.TryParse(userIdClaim, out var userId) ? userId : null;
    }

    public sealed record SecurityEventRequest(string? SourceType, string? SourceId, string EventType, string? Severity, int? SiteId, int? SecurityZoneId, int? AccessPointId, string? SubjectType, int? SubjectId, int? VehicleId, string? PlateText, decimal? Confidence, string? CorrelationId, string? Summary, DateTime? OccurredAtUtc);
    public sealed record CorrelationRunRequest(DateTime? SinceUtc, int MinimumEvents);
    public sealed record VideoBookmarkRequest(long? SecurityEventId, int? CameraId, string? ArtifactReference, DateTime StartUtc, DateTime EndUtc, string? Note);
    public sealed record SiteMapRequest(int? SiteId, string Name, string AssetReference, string? CoordinateSystem);
    public sealed record MapPlacementRequest(int? SecurityDeviceId, int? CameraId, decimal X, decimal Y, string? IconType);
    public sealed record AiAdjudicationRequest(long? SecurityEventId, string? AiSource, string? ModelVersion, decimal? Confidence);
    public sealed record AiReviewRequest(string? Outcome, string? ReviewNote);
    public sealed record AiMetricRequest(string? AiSource, string MetricName, decimal MetricValue, string? Notes);
}
