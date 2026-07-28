using API.Data;
using API.Middleware;
using API.Models;
using API.Services.FaceRecognition;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
[RequireOperationalTask("identity-mgmt")]
public sealed class FaceRecognitionEventsController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly IFaceRecognitionEventCollector _collector;

    public FaceRecognitionEventsController(
        ApplicationDbContext db,
        IFaceRecognitionEventCollector collector)
    {
        _db = db;
        _collector = collector;
    }

    [HttpGet]
    public async Task<ActionResult<FaceRecognitionEventPageDto>> GetAll(
        DateTime? fromUtc, DateTime? toUtc, int? employeeId, string? cameraId,
        int? laneId, string? matchStatus, int page = 1, int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        if (page < 1 || pageSize is < 1 or > 200 ||
            !IsUtc(fromUtc) || !IsUtc(toUtc) || fromUtc > toUtc)
            return BadRequest(new { message = "Invalid UTC range or pagination." });

        var query = _db.FaceRecognitionEvents.AsNoTracking()
            .Include(item => item.Employee).AsQueryable();
        if (fromUtc.HasValue) query = query.Where(item => item.OccurredAtUtc >= fromUtc.Value);
        if (toUtc.HasValue) query = query.Where(item => item.OccurredAtUtc <= toUtc.Value);
        if (employeeId.HasValue) query = query.Where(item => item.EmployeeId == employeeId);
        if (!string.IsNullOrWhiteSpace(cameraId))
            query = query.Where(item => item.CameraId == cameraId.Trim());
        if (laneId.HasValue) query = query.Where(item => item.LaneId == laneId);
        if (!string.IsNullOrWhiteSpace(matchStatus))
            query = query.Where(item => item.MatchStatus == matchStatus.Trim());

        var total = await query.CountAsync(cancellationToken);
        var events = await query.OrderByDescending(item => item.OccurredAtUtc)
            .ThenByDescending(item => item.Id)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(item => ToDto(item, item.Employee != null ? item.Employee.FullName : null, false))
            .ToListAsync(cancellationToken);
        var gapCameras = await _db.FaceRecognitionCollectorCheckpoints.AsNoTracking()
            .Where(item => item.GapDetectedAtUtc.HasValue)
            .Select(item => item.CameraId).ToListAsync(cancellationToken);
        var gapSet = gapCameras.ToHashSet(StringComparer.Ordinal);
        return new FaceRecognitionEventPageDto(total, page, pageSize,
            events.Select(item => item with { HistoryGapWarning = gapSet.Contains(item.CameraId) }).ToList());
    }

    [HttpGet("{id:long}")]
    public async Task<ActionResult<FaceRecognitionEventDto>> Get(
        long id, CancellationToken cancellationToken)
    {
        var item = await _db.FaceRecognitionEvents.AsNoTracking()
            .Include(entry => entry.Employee).SingleOrDefaultAsync(entry => entry.Id == id, cancellationToken);
        if (item is null) return NotFound();
        var gap = await _db.FaceRecognitionCollectorCheckpoints.AsNoTracking()
            .AnyAsync(entry => entry.CameraId == item.CameraId &&
                               entry.GapDetectedAtUtc.HasValue, cancellationToken);
        return ToDto(item, item.Employee?.FullName, gap);
    }

    [HttpGet("health")]
    public Task<FaceRecognitionCollectorHealth> Health(CancellationToken cancellationToken) =>
        _collector.HealthAsync(cancellationToken);

    [HttpGet("/api/Employees/{employeeId:int}/face-recognition-events")]
    public Task<ActionResult<FaceRecognitionEventPageDto>> GetForEmployee(
        int employeeId, int page = 1, int pageSize = 50,
        CancellationToken cancellationToken = default) =>
        GetAll(null, null, employeeId, null, null, null, page, pageSize, cancellationToken);

    [HttpGet("/api/FaceCameras/{cameraId}/recognition-events")]
    public Task<ActionResult<FaceRecognitionEventPageDto>> GetForCamera(
        string cameraId, int page = 1, int pageSize = 50,
        CancellationToken cancellationToken = default) =>
        GetAll(null, null, null, cameraId, null, null, page, pageSize, cancellationToken);

    private static bool IsUtc(DateTime? value) =>
        !value.HasValue || value.Value.Kind == DateTimeKind.Utc;

    private static FaceRecognitionEventDto ToDto(
        FaceRecognitionEvent item, string? employeeName, bool gap) =>
        new(item.Id, item.RuntimeEventId, item.CameraId, item.LaneId, item.EmployeeId,
            employeeName, item.EventType, item.OccurredAtUtc, item.ReceivedAtUtc,
            item.RecognitionDistance, item.ModelRegistryVersion, item.ModelFileName,
            item.ModelChecksumPrefix, item.MatchStatus,
            item.FaceCameraConfigurationId.HasValue, gap);
}

public sealed record FaceRecognitionEventDto(
    long Id, Guid RuntimeEventId, string CameraId, int? LaneId, int? EmployeeId,
    string? EmployeeName, string EventType, DateTime OccurredAtUtc, DateTime ReceivedAtUtc,
    double? RecognitionDistance, long? ModelVersion, string? ModelFileName,
    string? ModelChecksumPrefix, string MatchStatus, bool CameraManaged,
    bool HistoryGapWarning);

public sealed record FaceRecognitionEventPageDto(
    int Total, int Page, int PageSize, IReadOnlyList<FaceRecognitionEventDto> Items);
