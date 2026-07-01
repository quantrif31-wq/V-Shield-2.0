using System.Security.Claims;
using API.Data;
using API.Middleware;
using API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace API.Controllers;

[ApiController]
[Route("api/enterprise/situational-awareness")]
[Authorize]
[RequireOperationalTask("monitoring")]
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
        if (string.IsNullOrWhiteSpace(request.Name))
            return BadRequest(new { message = "Name is required." });

        if (request.SiteId.HasValue && !await _context.Sites.AnyAsync(s => s.SiteId == request.SiteId.Value))
            return BadRequest(new { message = "Site does not exist." });

        var assetReference = string.IsNullOrWhiteSpace(request.AssetReference)
            ? $"site-map:{request.SiteId?.ToString() ?? "global"}:{request.Name.Trim().ToLowerInvariant().Replace(' ', '-')}"
            : request.AssetReference.Trim();

        var map = new SiteMap
        {
            SiteId = request.SiteId,
            Name = request.Name.Trim(),
            AssetReference = assetReference,
            CoordinateSystem = string.IsNullOrWhiteSpace(request.CoordinateSystem) ? "Normalized" : request.CoordinateSystem.Trim()
        };

        _context.SiteMaps.Add(map);
        await _context.SaveChangesAsync();
        return Ok(map);
    }

    [HttpPatch("maps/{mapId:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateSiteMap(int mapId, [FromBody] SiteMapPatchRequest request)
    {
        var map = await _context.SiteMaps.FirstOrDefaultAsync(m => m.SiteMapId == mapId);
        if (map == null)
            return NotFound(new { message = "Map not found." });

        if (request.SiteId.HasValue && !await _context.Sites.AnyAsync(s => s.SiteId == request.SiteId.Value))
            return BadRequest(new { message = "Site does not exist." });

        if (!string.IsNullOrWhiteSpace(request.Name))
            map.Name = request.Name.Trim();
        if (request.SiteId.HasValue)
            map.SiteId = request.SiteId.Value;
        if (request.AssetReference != null && !string.IsNullOrWhiteSpace(request.AssetReference))
            map.AssetReference = request.AssetReference.Trim();
        if (!string.IsNullOrWhiteSpace(request.CoordinateSystem))
            map.CoordinateSystem = request.CoordinateSystem.Trim();
        if (request.IsActive.HasValue)
            map.IsActive = request.IsActive.Value;

        await _context.SaveChangesAsync();
        return Ok(map);
    }

    [HttpDelete("maps/{mapId:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteSiteMap(int mapId)
    {
        var map = await _context.SiteMaps.FirstOrDefaultAsync(m => m.SiteMapId == mapId);
        if (map == null)
            return NotFound(new { message = "Map not found." });

        _context.SiteMaps.Remove(map);
        await _context.SaveChangesAsync();
        return NoContent();
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

    [HttpPatch("maps/{mapId:int}/placements/{placementId:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateMapPlacement(int mapId, int placementId, [FromBody] MapPlacementPatchRequest request)
    {
        var placement = await _context.MapDevicePlacements.FirstOrDefaultAsync(p => p.SiteMapId == mapId && p.MapDevicePlacementId == placementId);
        if (placement == null)
            return NotFound(new { message = "Placement not found." });

        placement.SecurityDeviceId = request.SecurityDeviceId ?? placement.SecurityDeviceId;
        placement.CameraId = request.CameraId ?? placement.CameraId;
        placement.X = request.X ?? placement.X;
        placement.Y = request.Y ?? placement.Y;
        if (!string.IsNullOrWhiteSpace(request.IconType))
            placement.IconType = request.IconType.Trim();

        await _context.SaveChangesAsync();
        return Ok(placement);
    }

    [HttpDelete("maps/{mapId:int}/placements/{placementId:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteMapPlacement(int mapId, int placementId)
    {
        var placement = await _context.MapDevicePlacements.FirstOrDefaultAsync(p => p.SiteMapId == mapId && p.MapDevicePlacementId == placementId);
        if (placement == null)
            return NotFound(new { message = "Placement not found." });

        _context.MapDevicePlacements.Remove(placement);
        await _context.SaveChangesAsync();
        return NoContent();
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

    [HttpGet("events")]
    public async Task<IActionResult> GetEvents(
        [FromQuery] int? cameraId, [FromQuery] DateTime? from, [FromQuery] DateTime? to,
        [FromQuery] string? eventType, [FromQuery] string? subjectType, [FromQuery] string? plate,
        [FromQuery] decimal? minConfidence, [FromQuery] string? severity, [FromQuery] string? sourceType,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 50)
    {
        var query = _context.SecurityEvents.AsNoTracking().AsQueryable();
        if (cameraId.HasValue)
            query = query.Where(e => e.SourceId == cameraId.Value.ToString() || e.AccessPointId == cameraId.Value);
        if (from.HasValue) query = query.Where(e => e.OccurredAtUtc >= from.Value);
        if (to.HasValue) query = query.Where(e => e.OccurredAtUtc <= to.Value);
        if (!string.IsNullOrWhiteSpace(eventType)) query = query.Where(e => e.EventType.Contains(eventType.Trim()));
        if (!string.IsNullOrWhiteSpace(subjectType)) query = query.Where(e => e.SubjectType == subjectType.Trim());
        if (!string.IsNullOrWhiteSpace(plate)) query = query.Where(e => e.PlateText != null && e.PlateText.Contains(plate.Trim()));
        if (minConfidence.HasValue) query = query.Where(e => e.Confidence >= minConfidence.Value);
        if (!string.IsNullOrWhiteSpace(severity)) query = query.Where(e => e.Severity == severity.Trim());
        if (!string.IsNullOrWhiteSpace(sourceType)) query = query.Where(e => e.SourceType == sourceType.Trim());

        var total = await query.CountAsync();
        var items = await query
            .OrderByDescending(e => e.OccurredAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
        return Ok(new { total, page, pageSize, items });
    }

    [HttpGet("events/{eventId:long}")]
    public async Task<IActionResult> GetEvent(long eventId)
    {
        var ev = await _context.SecurityEvents.FindAsync(eventId);
        if (ev == null) return NotFound(new { message = "Event not found." });
        return Ok(ev);
    }

    [HttpDelete("events/{eventId:long}")]
    public async Task<IActionResult> DeleteEvent(long eventId)
    {
        var ev = await _context.SecurityEvents.FindAsync(eventId);
        if (ev == null) return NotFound(new { message = "Event not found." });
        _context.SecurityEvents.Remove(ev);
        await _context.SaveChangesAsync();
        return NoContent();
    }

    [HttpGet("video-bookmarks")]
    public async Task<IActionResult> GetVideoBookmarks([FromQuery] long? securityEventId, [FromQuery] int? cameraId)
    {
        var query = _context.VideoBookmarks.AsNoTracking().AsQueryable();
        if (securityEventId.HasValue) query = query.Where(b => b.SecurityEventId == securityEventId.Value);
        if (cameraId.HasValue) query = query.Where(b => b.CameraId == cameraId.Value);
        var bookmarks = await query.OrderByDescending(b => b.StartUtc).ToListAsync();
        return Ok(bookmarks);
    }

    [HttpDelete("video-bookmarks/{bookmarkId:int}")]
    public async Task<IActionResult> DeleteVideoBookmark(int bookmarkId)
    {
        var bm = await _context.VideoBookmarks.FindAsync(bookmarkId);
        if (bm == null) return NotFound(new { message = "Bookmark not found." });
        _context.VideoBookmarks.Remove(bm);
        await _context.SaveChangesAsync();
        return NoContent();
    }

    [HttpPost("clip-requests")]
    public async Task<IActionResult> CreateClipRequest([FromBody] ClipRequestRequest request)
    {
        if (request.EndUtc <= request.StartUtc)
            return BadRequest(new { message = "EndUtc must be after StartUtc." });

        var clip = new ClipRequest
        {
            CameraId = request.CameraId,
            SecurityEventId = request.SecurityEventId,
            StartUtc = request.StartUtc,
            EndUtc = request.EndUtc,
            RequestedBy = string.IsNullOrWhiteSpace(request.RequestedBy) ? "System" : request.RequestedBy.Trim(),
            RetentionCategory = request.RetentionCategory?.Trim(),
            Note = request.Note?.Trim(),
            Status = "Pending"
        };
        _context.ClipRequests.Add(clip);
        await _context.SaveChangesAsync();
        return Ok(clip);
    }

    [HttpGet("clip-requests")]
    public async Task<IActionResult> GetClipRequests([FromQuery] string? status)
    {
        var query = _context.ClipRequests.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(status))
            query = query.Where(c => c.Status == status.Trim());
        var clips = await query.OrderByDescending(c => c.CreatedAtUtc).ToListAsync();
        return Ok(clips);
    }

    [HttpPatch("clip-requests/{clipId:int}/approve")]
    public async Task<IActionResult> ApproveClipRequest(int clipId, [FromBody] ClipApproveRequest request)
    {
        var clip = await _context.ClipRequests.FindAsync(clipId);
        if (clip == null) return NotFound(new { message = "Clip request not found." });
        clip.Status = "Approved";
        clip.RetentionCategory = request.RetentionCategory?.Trim();
        clip.ApprovedAtUtc = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return Ok(clip);
    }

    [HttpPatch("clip-requests/{clipId:int}/export")]
    public async Task<IActionResult> ExportClipRequest(int clipId, [FromBody] ClipExportRequest request)
    {
        var clip = await _context.ClipRequests.FindAsync(clipId);
        if (clip == null) return NotFound(new { message = "Clip request not found." });
        clip.Status = "Exported";
        clip.ExportReference = request.ExportReference?.Trim();
        clip.ExportedAtUtc = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return Ok(clip);
    }

    [HttpGet("ai-adjudications")]
    public async Task<IActionResult> GetAiAdjudications(
        [FromQuery] string? status, [FromQuery] string? aiSource, [FromQuery] string? outcome,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 50)
    {
        var query = _context.AiAdjudicationItems.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(status)) query = query.Where(a => a.Status == status.Trim());
        if (!string.IsNullOrWhiteSpace(aiSource)) query = query.Where(a => a.AiSource == aiSource.Trim());
        if (!string.IsNullOrWhiteSpace(outcome)) query = query.Where(a => a.Outcome == outcome.Trim());

        var total = await query.CountAsync();
        var items = await query
            .OrderByDescending(a => a.CreatedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
        return Ok(new { total, page, pageSize, items });
    }

    [HttpGet("ai-metrics")]
    public async Task<IActionResult> GetAiMetrics(
        [FromQuery] string? aiSource, [FromQuery] string? metricName,
        [FromQuery] DateTime? from, [FromQuery] DateTime? to,
        [FromQuery] int limit = 100)
    {
        var query = _context.AiPerformanceMetrics.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(aiSource)) query = query.Where(m => m.AiSource == aiSource.Trim());
        if (!string.IsNullOrWhiteSpace(metricName)) query = query.Where(m => m.MetricName == metricName.Trim());
        if (from.HasValue) query = query.Where(m => m.CapturedAtUtc >= from.Value);
        if (to.HasValue) query = query.Where(m => m.CapturedAtUtc <= to.Value);
        var metrics = await query.OrderByDescending(m => m.CapturedAtUtc).Take(limit).ToListAsync();
        return Ok(metrics);
    }

    [HttpGet("ai-metrics/summary")]
    public async Task<IActionResult> GetAiMetricsSummary()
    {
        var totalReviewed = await _context.AiAdjudicationItems.CountAsync(a => a.Status == "Reviewed");
        var totalConfirmed = await _context.AiAdjudicationItems.CountAsync(a => a.Outcome == "Confirmed");
        var totalFalsePositive = await _context.AiAdjudicationItems.CountAsync(a => a.Outcome == "FalsePositive");
        var totalFalseNegative = await _context.AiAdjudicationItems.CountAsync(a => a.Outcome == "FalseNegative");
        var totalTraining = await _context.AiAdjudicationItems.CountAsync(a => a.Outcome == "TrainingCandidate");
        var pending = await _context.AiAdjudicationItems.CountAsync(a => a.Status == "Pending");

        var precisionProxy = totalReviewed > 0
            ? (decimal)totalConfirmed / totalReviewed * 100
            : 0;

        var recentDrift = await _context.AiPerformanceMetrics
            .Where(m => m.MetricName == "drift_score" && m.CapturedAtUtc >= DateTime.UtcNow.AddHours(-24))
            .OrderByDescending(m => m.CapturedAtUtc)
            .Select(m => new { m.MetricValue, m.CapturedAtUtc })
            .FirstOrDefaultAsync();

        return Ok(new
        {
            TotalReviewed = totalReviewed,
            TotalConfirmed = totalConfirmed,
            TotalFalsePositive = totalFalsePositive,
            TotalFalseNegative = totalFalseNegative,
            TotalTrainingCandidate = totalTraining,
            PendingReviews = pending,
            PrecisionProxy = Math.Round(precisionProxy, 1),
            RecentDriftScore = recentDrift?.MetricValue,
            DriftDetected = recentDrift?.MetricValue > 0.15m,
            DriftCapturedAt = recentDrift?.CapturedAtUtc
        });
    }

    [HttpGet("correlations")]
    public async Task<IActionResult> GetCorrelations([FromQuery] string? ruleName, [FromQuery] int limit = 50)
    {
        var query = _context.EventCorrelations.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(ruleName)) query = query.Where(c => c.RuleName.Contains(ruleName.Trim()));
        var correlations = await query.OrderByDescending(c => c.CreatedAtUtc).Take(limit).ToListAsync();
        return Ok(correlations);
    }

    [HttpGet("correlations/{correlationId:long}")]
    public async Task<IActionResult> GetCorrelationDetail(long correlationId)
    {
        var correlation = await _context.EventCorrelations.FindAsync(correlationId);
        if (correlation == null) return NotFound(new { message = "Correlation not found." });
        var events = await _context.SecurityEvents
            .Where(e => e.CorrelationId == correlation.CorrelationId)
            .OrderByDescending(e => e.OccurredAtUtc)
            .ToListAsync();
        return Ok(new { correlation, events });
    }

    [HttpGet("maps")]
    public async Task<IActionResult> GetSiteMaps([FromQuery] int? siteId)
    {
        var query = _context.SiteMaps.AsNoTracking().AsQueryable();
        if (siteId.HasValue)
            query = query.Where(m => m.SiteId == siteId.Value);

        var maps = await query.OrderBy(m => m.Name).ToListAsync();
        return Ok(maps);
    }

    [HttpGet("maps/{mapId:int}/placements")]
    public async Task<IActionResult> GetMapPlacements(int mapId)
    {
        if (!await _context.SiteMaps.AnyAsync(m => m.SiteMapId == mapId))
            return NotFound(new { message = "Map not found." });
        var placements = await _context.MapDevicePlacements
            .Include(p => p.SecurityDevice)
            .Include(p => p.Camera)
            .Where(p => p.SiteMapId == mapId)
            .OrderBy(p => p.IconType).ThenBy(p => p.Y)
            .ToListAsync();
        return Ok(placements.Select(p => new
        {
            p.MapDevicePlacementId,
            p.SiteMapId,
            p.SecurityDeviceId,
            securityDeviceName = p.SecurityDevice != null ? p.SecurityDevice.Name : null,
            p.CameraId,
            cameraName = p.Camera != null ? p.Camera.CameraName : null,
            x = p.X,
            y = p.Y,
            p.IconType
        }));
    }

    private int? GetCurrentUserId()
    {
        var userIdClaim = User.FindFirstValue(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub);
        return int.TryParse(userIdClaim, out var userId) ? userId : null;
    }

    public sealed record SecurityEventRequest(string? SourceType, string? SourceId, string EventType, string? Severity, int? SiteId, int? SecurityZoneId, int? AccessPointId, string? SubjectType, int? SubjectId, int? VehicleId, string? PlateText, decimal? Confidence, string? CorrelationId, string? Summary, DateTime? OccurredAtUtc);
    public sealed record CorrelationRunRequest(DateTime? SinceUtc, int MinimumEvents);
    public sealed record VideoBookmarkRequest(long? SecurityEventId, int? CameraId, string? ArtifactReference, DateTime StartUtc, DateTime EndUtc, string? Note);
    public sealed record SiteMapRequest(int? SiteId, string Name, string? AssetReference, string? CoordinateSystem);
    public sealed record SiteMapPatchRequest(int? SiteId, string? Name, string? AssetReference, string? CoordinateSystem, bool? IsActive);
    public sealed record MapPlacementRequest(int? SecurityDeviceId, int? CameraId, decimal X, decimal Y, string? IconType);
    public sealed record MapPlacementPatchRequest(int? SecurityDeviceId, int? CameraId, decimal? X, decimal? Y, string? IconType);
    public sealed record AiAdjudicationRequest(long? SecurityEventId, string? AiSource, string? ModelVersion, decimal? Confidence);
    public sealed record AiReviewRequest(string? Outcome, string? ReviewNote);
    public sealed record AiMetricRequest(string? AiSource, string MetricName, decimal MetricValue, string? Notes);
    public sealed record ClipRequestRequest(int? CameraId, long? SecurityEventId, DateTime StartUtc, DateTime EndUtc, string? RequestedBy, string? RetentionCategory, string? Note);
    public sealed record ClipApproveRequest(string? RetentionCategory);
    public sealed record ClipExportRequest(string? ExportReference);
}
