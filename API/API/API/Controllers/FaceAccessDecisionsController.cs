using API.Data;
using API.Middleware;
using API.Models;
using API.Services.AccessPolicyComparison;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
[RequireOperationalTask("identity-mgmt")]
public sealed class FaceAccessDecisionsController(
    ApplicationDbContext db, IFaceAccessDecisionProcessor processor) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<object>> List(
        DateTime? fromUtc, DateTime? toUtc, int? employeeId, string? cameraId,
        int? gateId, int? accessPointId, string? decision,
        int page = 1, int pageSize = 50, CancellationToken token = default)
    {
        if (page < 1 || pageSize is < 1 or > 200 || !Utc(fromUtc) || !Utc(toUtc) || fromUtc > toUtc)
            return BadRequest(new { message = "Khoảng thời gian UTC hoặc phân trang không hợp lệ." });
        var query = Filter(db.FaceAccessDecisions.AsNoTracking(), fromUtc, toUtc,
            employeeId, cameraId, gateId, accessPointId, decision);
        var total = await query.CountAsync(token);
        var items = await query.OrderByDescending(x => x.OccurredAtUtc).ThenByDescending(x => x.Id)
            .Skip((page - 1) * pageSize).Take(pageSize).Select(x => Dto(x)).ToListAsync(token);
        return Ok(new { total, page, pageSize, items });
    }

    [HttpGet("{id:long}")]
    public async Task<ActionResult<object>> Get(long id, CancellationToken token)
    {
        var item = await db.FaceAccessDecisions.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id, token);
        return item is null ? NotFound() : Ok(Dto(item));
    }

    [HttpGet("summary")]
    public async Task<ActionResult<object>> Summary(DateTime? fromUtc, DateTime? toUtc,
        CancellationToken token)
    {
        if (!Utc(fromUtc) || !Utc(toUtc) || fromUtc > toUtc)
            return BadRequest(new { message = "Khoảng thời gian UTC không hợp lệ." });
        var query = Filter(db.FaceAccessDecisions.AsNoTracking(), fromUtc, toUtc,
            null, null, null, null, null);
        var groups = await query.GroupBy(x => x.Decision)
            .Select(x => new { x.Key, Count = x.Count() })
            .ToDictionaryAsync(x => x.Key, x => x.Count, token);
        return Ok(new {
            total = groups.Values.Sum(),
            allowed = groups.GetValueOrDefault(FaceAccessDecisionStatuses.Allowed),
            denied = groups.GetValueOrDefault(FaceAccessDecisionStatuses.Denied),
            reviewRequired = groups.GetValueOrDefault(FaceAccessDecisionStatuses.ReviewRequired),
            indeterminate = groups.GetValueOrDefault(FaceAccessDecisionStatuses.Indeterminate)
        });
    }

    [HttpGet("health")]
    public Task<object> Health(CancellationToken token) => processor.HealthAsync(token);

    [HttpGet("/api/FaceRecognitionEvents/{eventId:long}/access-decision")]
    public async Task<ActionResult<object>> ForEvent(long eventId, CancellationToken token)
    {
        var item = await db.FaceAccessDecisions.AsNoTracking()
            .SingleOrDefaultAsync(x => x.FaceRecognitionEventId == eventId, token);
        return item is null ? NotFound() : Ok(Dto(item));
    }

    private static IQueryable<FaceAccessDecision> Filter(IQueryable<FaceAccessDecision> query,
        DateTime? from, DateTime? to, int? employee, string? camera, int? gate,
        int? accessPoint, string? decision)
    {
        if (from.HasValue) query = query.Where(x => x.OccurredAtUtc >= from);
        if (to.HasValue) query = query.Where(x => x.OccurredAtUtc <= to);
        if (employee.HasValue) query = query.Where(x => x.EmployeeId == employee);
        if (!string.IsNullOrWhiteSpace(camera)) query = query.Where(x => x.CameraId == camera);
        if (gate.HasValue) query = query.Where(x => x.GateId == gate);
        if (accessPoint.HasValue) query = query.Where(x => x.AccessPointId == accessPoint);
        if (!string.IsNullOrWhiteSpace(decision)) query = query.Where(x => x.Decision == decision);
        return query;
    }

    private static bool Utc(DateTime? value) => !value.HasValue || value.Value.Kind == DateTimeKind.Utc;
    private static object Dto(FaceAccessDecision x) => new {
        x.Id, recognitionEventId = x.FaceRecognitionEventId,
        policyComparisonId = x.FaceAccessPolicyComparisonId,
        x.EmployeeId, x.CameraId, x.LaneId, x.GateId, x.AccessPointId,
        x.OccurredAtUtc, x.DecidedAtUtc, x.Decision, x.ReasonCode,
        x.LegacyDecision, x.LegacyReasonCode, x.EnterpriseDecision, x.EnterpriseReasonCode,
        x.LegacyPermissionId, x.EnterprisePolicyVersionId, x.EnterpriseRuleId,
        x.EnterpriseScheduleId, x.EvaluationVersion, x.ScheduleTimeZoneId,
        inputFingerprintPrefix = x.InputFingerprint[..Math.Min(12, x.InputFingerprint.Length)]
    };
}
