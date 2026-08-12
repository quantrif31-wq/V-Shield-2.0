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
public sealed class FaceAccessPolicyComparisonsController(
    ApplicationDbContext db, IFaceAccessPolicyComparisonProcessor processor) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<object>> List(
        DateTime? fromUtc, DateTime? toUtc, int? employeeId, string? cameraId,
        int? gateId, int? accessPointId, string? legacyDecision,
        string? enterpriseDecision, string? comparisonResult,
        int page = 1, int pageSize = 50, CancellationToken token = default)
    {
        if (page < 1 || pageSize is < 1 or > 200 || !Utc(fromUtc) || !Utc(toUtc) || fromUtc > toUtc)
            return BadRequest(new { message = "Khoảng thời gian UTC hoặc phân trang không hợp lệ." });
        var query = Filter(db.FaceAccessPolicyComparisons.AsNoTracking(), fromUtc, toUtc,
            employeeId, cameraId, gateId, accessPointId, legacyDecision,
            enterpriseDecision, comparisonResult);
        var total = await query.CountAsync(token);
        var items = await query.OrderByDescending(x => x.OccurredAtUtc).ThenByDescending(x => x.Id)
            .Skip((page - 1) * pageSize).Take(pageSize).Select(x => Dto(x)).ToListAsync(token);
        return Ok(new { total, page, pageSize, items });
    }

    [HttpGet("{id:long}")]
    public async Task<ActionResult<object>> Get(long id, CancellationToken token)
    {
        var item = await db.FaceAccessPolicyComparisons.AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == id, token);
        return item is null ? NotFound() : Ok(Dto(item));
    }

    [HttpGet("summary")]
    public async Task<ActionResult<object>> Summary(
        DateTime? fromUtc, DateTime? toUtc, CancellationToken token)
    {
        if (!Utc(fromUtc) || !Utc(toUtc) || fromUtc > toUtc)
            return BadRequest(new { message = "Khoảng thời gian UTC không hợp lệ." });
        var query = Filter(db.FaceAccessPolicyComparisons.AsNoTracking(),
            fromUtc, toUtc, null, null, null, null, null, null, null);
        var groups = await query.GroupBy(x => x.ComparisonResult)
            .Select(x => new { x.Key, Count = x.Count() }).ToDictionaryAsync(x => x.Key, x => x.Count, token);
        int Count(string key) => groups.GetValueOrDefault(key);
        return Ok(new {
            totalCompared = groups.Values.Sum(),
            agreeAllow = Count(PolicyComparisonResults.AgreeAllow),
            agreeDeny = Count(PolicyComparisonResults.AgreeDeny),
            legacyAllowEnterpriseDeny = Count(PolicyComparisonResults.LegacyAllowEnterpriseDeny),
            legacyDenyEnterpriseAllow = Count(PolicyComparisonResults.LegacyDenyEnterpriseAllow),
            legacyOnly = Count(PolicyComparisonResults.LegacyConfiguredEnterpriseMissing),
            enterpriseOnly = Count(PolicyComparisonResults.EnterpriseConfiguredLegacyMissing),
            bothNotConfigured = Count(PolicyComparisonResults.BothNotConfigured),
            indeterminate = Count(PolicyComparisonResults.LegacyIndeterminate) +
                            Count(PolicyComparisonResults.EnterpriseIndeterminate),
            mappingUnavailable = Count(PolicyComparisonResults.MappingUnavailable)
        });
    }

    [HttpGet("health")]
    public Task<object> Health(CancellationToken token) => processor.HealthAsync(token);

    [HttpGet("/api/FaceRecognitionEvents/{eventId:long}/policy-comparison")]
    public async Task<ActionResult<object>> ForEvent(long eventId, CancellationToken token)
    {
        var item = await db.FaceAccessPolicyComparisons.AsNoTracking()
            .SingleOrDefaultAsync(x => x.FaceRecognitionEventId == eventId, token);
        return item is null ? NotFound() : Ok(Dto(item));
    }

    private static IQueryable<FaceAccessPolicyComparison> Filter(
        IQueryable<FaceAccessPolicyComparison> query, DateTime? from, DateTime? to,
        int? employee, string? camera, int? gate, int? accessPoint,
        string? legacy, string? enterprise, string? result)
    {
        if (from.HasValue) query = query.Where(x => x.OccurredAtUtc >= from);
        if (to.HasValue) query = query.Where(x => x.OccurredAtUtc <= to);
        if (employee.HasValue) query = query.Where(x => x.EmployeeId == employee);
        if (!string.IsNullOrWhiteSpace(camera)) query = query.Where(x => x.CameraId == camera);
        if (gate.HasValue) query = query.Where(x => x.GateId == gate);
        if (accessPoint.HasValue) query = query.Where(x => x.AccessPointId == accessPoint);
        if (!string.IsNullOrWhiteSpace(legacy)) query = query.Where(x => x.LegacyDecision == legacy);
        if (!string.IsNullOrWhiteSpace(enterprise)) query = query.Where(x => x.EnterpriseDecision == enterprise);
        if (!string.IsNullOrWhiteSpace(result)) query = query.Where(x => x.ComparisonResult == result);
        return query;
    }

    private static bool Utc(DateTime? value) => !value.HasValue || value.Value.Kind == DateTimeKind.Utc;
    private static object Dto(FaceAccessPolicyComparison x) => new {
        x.Id, recognitionEventId = x.FaceRecognitionEventId, x.EmployeeId, x.CameraId,
        x.LaneId, x.GateId, x.AccessPointId, x.OccurredAtUtc, x.EvaluatedAtUtc,
        x.LegacyDecision, x.LegacyReasonCode, x.LegacyPermissionId,
        x.EnterpriseDecision, x.EnterpriseReasonCode, x.EnterprisePolicyVersionId,
        x.EnterpriseRuleId, x.EnterpriseScheduleId, x.ComparisonResult, x.MappingStatus,
        legacyFingerprintPrefix = x.LegacyInputFingerprint[..Math.Min(12, x.LegacyInputFingerprint.Length)],
        enterpriseFingerprintPrefix = x.EnterpriseInputFingerprint[..Math.Min(12, x.EnterpriseInputFingerprint.Length)],
        x.ScheduleTimeZoneId, x.EvaluationVersion
    };
}
