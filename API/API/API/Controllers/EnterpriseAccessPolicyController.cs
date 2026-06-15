using System.Security.Claims;
using API.Data;
using API.Middleware;
using API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace API.Controllers;

[ApiController]
[Route("api/enterprise/access-policy")]
[Authorize(Roles = "Admin")]
public class EnterpriseAccessPolicyController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public EnterpriseAccessPolicyController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet("overview")]
    public async Task<IActionResult> GetOverview()
    {
        return Ok(new
        {
            Schedules = await _context.AccessSchedules.CountAsync(),
            HolidayCalendarItems = await _context.HolidayCalendars.CountAsync(),
            AccessLevels = await _context.AccessLevels.CountAsync(),
            AccessGroups = await _context.AccessGroups.CountAsync(),
            AccessRules = await _context.AccessRules.CountAsync(),
            TemporaryGrants = await _context.TemporaryAccessGrants.CountAsync(),
            PolicyVersions = await _context.AccessPolicyVersions.CountAsync(),
            ActivePolicyVersions = await _context.AccessPolicyVersions.CountAsync(version => version.Status == "Active"),
            PendingApprovalPolicyVersions = await _context.AccessPolicyVersions.CountAsync(version => version.Status == "PendingApproval"),
            EmergencyStates = await _context.EmergencyStates.CountAsync(e => e.IsActive),
            AntiPassbackStates = await _context.AntiPassbackStates.CountAsync(),
            OccupancySnapshots = await _context.OccupancySnapshots.CountAsync(),
            Decisions = await _context.AccessDecisions.CountAsync()
        });
    }

    [HttpPost("schedules")]
    public async Task<IActionResult> CreateSchedule([FromBody] ScheduleRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            return BadRequest(new { message = "Name is required." });

        var schedule = new AccessSchedule
        {
            Name = request.Name.Trim(),
            StartTime = request.StartTime,
            EndTime = request.EndTime,
            DaysOfWeek = string.IsNullOrWhiteSpace(request.DaysOfWeek) ? "Mon,Tue,Wed,Thu,Fri" : request.DaysOfWeek.Trim(),
            IsActive = request.IsActive
        };

        _context.AccessSchedules.Add(schedule);
        await _context.SaveChangesAsync();
        return Ok(schedule);
    }

    [HttpPost("holiday-calendar")]
    public async Task<IActionResult> AddHoliday([FromBody] HolidayRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            return BadRequest(new { message = "Name is required." });

        var holiday = new HolidayCalendar
        {
            SiteId = request.SiteId,
            Name = request.Name.Trim(),
            HolidayDate = request.HolidayDate.Date,
            Note = request.Note?.Trim()
        };

        _context.HolidayCalendars.Add(holiday);
        await _context.SaveChangesAsync();
        return Ok(holiday);
    }

    [HttpPost("access-levels")]
    public async Task<IActionResult> CreateAccessLevel([FromBody] AccessLevelRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name) || string.IsNullOrWhiteSpace(request.Code))
            return BadRequest(new { message = "Name and code are required." });

        var level = new AccessLevel
        {
            Name = request.Name.Trim(),
            Code = request.Code.Trim(),
            Description = request.Description?.Trim(),
            RequiresApproval = request.RequiresApproval
        };

        _context.AccessLevels.Add(level);
        await _context.SaveChangesAsync();
        return Ok(level);
    }

    [HttpPost("access-groups")]
    public async Task<IActionResult> CreateAccessGroup([FromBody] AccessGroupRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name) || string.IsNullOrWhiteSpace(request.Code))
            return BadRequest(new { message = "Name and code are required." });

        var group = new AccessGroup
        {
            Name = request.Name.Trim(),
            Code = request.Code.Trim()
        };

        _context.AccessGroups.Add(group);
        await _context.SaveChangesAsync();
        return Ok(group);
    }

    [HttpGet("rules")]
    public async Task<IActionResult> GetRules()
    {
        var rules = await _context.AccessRules
            .OrderByDescending(r => r.AccessRuleId)
            .Take(200)
            .ToListAsync();
        return Ok(rules);
    }

    [HttpPost("rules")]
    public async Task<IActionResult> CreateRule([FromBody] AccessRuleRequest request)
    {
        if (!await _context.AccessLevels.AnyAsync(level => level.AccessLevelId == request.AccessLevelId))
            return BadRequest(new { message = "Access level does not exist." });

        var rule = new AccessRule
        {
            AccessPolicyVersionId = request.AccessPolicyVersionId,
            AccessLevelId = request.AccessLevelId,
            AccessGroupId = request.AccessGroupId,
            SiteId = request.SiteId,
            SecurityZoneId = request.SecurityZoneId,
            AccessPointId = request.AccessPointId,
            AccessScheduleId = request.AccessScheduleId,
            SubjectType = string.IsNullOrWhiteSpace(request.SubjectType) ? "Employee" : request.SubjectType.Trim(),
            SubjectId = request.SubjectId,
            CredentialType = string.IsNullOrWhiteSpace(request.CredentialType) ? "Any" : request.CredentialType.Trim(),
            AllowAccess = request.AllowAccess,
            ValidFromUtc = request.ValidFromUtc,
            ValidToUtc = request.ValidToUtc,
            IsActive = request.IsActive
        };

        _context.AccessRules.Add(rule);
        await _context.SaveChangesAsync();
        return Ok(rule);
    }

    [HttpGet("policy-versions")]
    public async Task<IActionResult> GetPolicyVersions()
    {
        var versions = await _context.AccessPolicyVersions
            .OrderByDescending(version => version.CreatedAtUtc)
            .Select(version => new
            {
                version.AccessPolicyVersionId,
                version.Name,
                version.Status,
                version.ChangeSummary,
                version.CreatedAtUtc,
                version.SubmittedAtUtc,
                version.ApprovedAtUtc,
                version.ActivatedAtUtc,
                version.RetiredAtUtc,
                Rules = _context.AccessRules.Count(rule => rule.AccessPolicyVersionId == version.AccessPolicyVersionId)
            })
            .ToListAsync();

        return Ok(versions);
    }

    [HttpPost("policy-versions")]
    public async Task<IActionResult> CreatePolicyVersion([FromBody] PolicyVersionRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            return BadRequest(new { message = "Name is required." });

        var version = new AccessPolicyVersion
        {
            Name = request.Name.Trim(),
            Status = "Draft",
            ChangeSummary = request.ChangeSummary?.Trim(),
            CreatedByUserId = GetCurrentUserId()
        };

        _context.AccessPolicyVersions.Add(version);
        await _context.SaveChangesAsync();
        return Ok(version);
    }

    [HttpPatch("policy-versions/{policyVersionId:int}/submit")]
    public async Task<IActionResult> SubmitPolicyVersion(int policyVersionId)
    {
        var version = await _context.AccessPolicyVersions.FindAsync(policyVersionId);
        if (version == null)
            return NotFound(new { message = "Policy version not found." });
        if (version.Status != "Draft")
            return BadRequest(new { message = "Only Draft policy versions can be submitted." });

        version.Status = "PendingApproval";
        version.SubmittedAtUtc = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return Ok(version);
    }

    [HttpPatch("policy-versions/{policyVersionId:int}/approve")]
    [RequireStepUp(PrivilegedActions.AccessPolicyEmergency)]
    public async Task<IActionResult> ApprovePolicyVersion(int policyVersionId, [FromBody] PolicyApprovalRequest request)
    {
        var version = await _context.AccessPolicyVersions.FindAsync(policyVersionId);
        if (version == null)
            return NotFound(new { message = "Policy version not found." });
        if (version.Status != "PendingApproval")
            return BadRequest(new { message = "Only PendingApproval policy versions can be approved." });

        version.Status = "Approved";
        version.ApprovedAtUtc = DateTime.UtcNow;
        version.ApprovedByUserId = GetCurrentUserId();
        if (!string.IsNullOrWhiteSpace(request.Note))
            version.ChangeSummary = string.IsNullOrWhiteSpace(version.ChangeSummary)
                ? request.Note.Trim()
                : $"{version.ChangeSummary}\nApproval: {request.Note.Trim()}";

        await _context.SaveChangesAsync();
        return Ok(version);
    }

    [HttpPatch("policy-versions/{policyVersionId:int}/activate")]
    [RequireStepUp(PrivilegedActions.AccessPolicyEmergency)]
    public async Task<IActionResult> ActivatePolicyVersion(int policyVersionId)
    {
        var version = await _context.AccessPolicyVersions.FindAsync(policyVersionId);
        if (version == null)
            return NotFound(new { message = "Policy version not found." });
        if (version.Status != "Approved")
            return BadRequest(new { message = "Only Approved policy versions can be activated." });

        var activeVersions = await _context.AccessPolicyVersions
            .Where(item => item.Status == "Active")
            .ToListAsync();
        foreach (var active in activeVersions)
        {
            active.Status = "Retired";
            active.RetiredAtUtc = DateTime.UtcNow;
        }

        version.Status = "Active";
        version.ActivatedAtUtc = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return Ok(version);
    }

    [HttpPatch("policy-versions/{policyVersionId:int}/retire")]
    [RequireStepUp(PrivilegedActions.AccessPolicyEmergency)]
    public async Task<IActionResult> RetirePolicyVersion(int policyVersionId)
    {
        var version = await _context.AccessPolicyVersions.FindAsync(policyVersionId);
        if (version == null)
            return NotFound(new { message = "Policy version not found." });

        version.Status = "Retired";
        version.RetiredAtUtc = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return Ok(version);
    }

    [HttpPost("temporary-grants")]
    public async Task<IActionResult> CreateTemporaryGrant([FromBody] TemporaryGrantRequest request)
    {
        if (request.SubjectId <= 0)
            return BadRequest(new { message = "SubjectId is required." });
        if (request.ValidToUtc <= request.ValidFromUtc)
            return BadRequest(new { message = "ValidToUtc must be after ValidFromUtc." });

        var grant = new TemporaryAccessGrant
        {
            SubjectType = string.IsNullOrWhiteSpace(request.SubjectType) ? "Employee" : request.SubjectType.Trim(),
            SubjectId = request.SubjectId,
            SiteId = request.SiteId,
            SecurityZoneId = request.SecurityZoneId,
            AccessPointId = request.AccessPointId,
            ValidFromUtc = request.ValidFromUtc,
            ValidToUtc = request.ValidToUtc,
            Reason = string.IsNullOrWhiteSpace(request.Reason) ? "Temporary grant" : request.Reason.Trim(),
            ApprovedByUserId = GetCurrentUserId()
        };

        _context.TemporaryAccessGrants.Add(grant);
        await _context.SaveChangesAsync();
        return Ok(grant);
    }

    [HttpGet("emergency-states")]
    public async Task<IActionResult> GetEmergencyStates([FromQuery] bool? active)
    {
        var query = _context.EmergencyStates.AsQueryable();
        if (active == true)
            query = query.Where(e => e.IsActive);
        var states = await query.OrderByDescending(e => e.StartedAtUtc).Take(50).ToListAsync();
        return Ok(states);
    }

    [HttpPost("emergency-states")]
    [RequireStepUp(PrivilegedActions.AccessPolicyEmergency)]
    public async Task<IActionResult> CreateEmergencyState([FromBody] EmergencyStateRequest request)
    {
        var state = new EmergencyState
        {
            State = string.IsNullOrWhiteSpace(request.State) ? "Normal" : request.State.Trim(),
            SiteId = request.SiteId,
            SecurityZoneId = request.SecurityZoneId,
            AccessPointId = request.AccessPointId,
            Reason = string.IsNullOrWhiteSpace(request.Reason) ? "Emergency state update" : request.Reason.Trim(),
            IsActive = true,
            CreatedByUserId = GetCurrentUserId()
        };

        _context.EmergencyStates.Add(state);
        await _context.SaveChangesAsync();
        return Ok(state);
    }

    [HttpPost("anti-passback/reset")]
    [RequireStepUp(PrivilegedActions.AccessPolicyEmergency)]
    public async Task<IActionResult> ResetAntiPassback([FromBody] AntiPassbackResetRequest request)
    {
        var state = await _context.AntiPassbackStates.FirstOrDefaultAsync(item =>
            item.SubjectType == request.SubjectType &&
            item.SubjectId == request.SubjectId &&
            item.SecurityZoneId == request.SecurityZoneId);

        if (state == null)
        {
            state = new AntiPassbackState
            {
                SubjectType = string.IsNullOrWhiteSpace(request.SubjectType) ? "Employee" : request.SubjectType.Trim(),
                SubjectId = request.SubjectId,
                SecurityZoneId = request.SecurityZoneId
            };
            _context.AntiPassbackStates.Add(state);
        }

        state.State = "Unknown";
        state.IsViolated = false;
        state.ResetReason = request.Reason?.Trim();
        state.UpdatedAtUtc = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return Ok(state);
    }

    [HttpPost("occupancy")]
    public async Task<IActionResult> RecordOccupancy([FromBody] OccupancyRequest request)
    {
        var snapshot = new OccupancySnapshot
        {
            SiteId = request.SiteId,
            SecurityZoneId = request.SecurityZoneId,
            Count = request.Count,
            MaxAllowed = request.MaxAllowed
        };

        _context.OccupancySnapshots.Add(snapshot);
        await _context.SaveChangesAsync();
        return Ok(snapshot);
    }

    [HttpPost("evaluate")]
    public async Task<IActionResult> Evaluate([FromBody] AccessEvaluationRequest request)
    {
        var nowUtc = request.EvaluatedAtUtc ?? DateTime.UtcNow;
        var result = await EvaluateInternalAsync(request, nowUtc);

        _context.AccessDecisions.Add(result);
        await _context.SaveChangesAsync();

        return Ok(result);
    }

    [HttpPost("simulate")]
    public async Task<IActionResult> Simulate([FromBody] AccessEvaluationRequest request)
    {
        var nowUtc = request.EvaluatedAtUtc ?? DateTime.UtcNow;
        var result = await EvaluateInternalAsync(request, nowUtc);
        result.DecisionMode = "Simulation";
        return Ok(result);
    }

    [HttpPost("shadow-compare")]
    public async Task<IActionResult> ShadowCompare([FromBody] AccessShadowCompareRequest request)
    {
        var nowUtc = request.EvaluatedAtUtc ?? DateTime.UtcNow;
        var evaluation = new AccessEvaluationRequest(
            request.SubjectType,
            request.SubjectId,
            request.SiteId,
            request.SecurityZoneId,
            request.AccessPointId,
            request.CredentialType,
            request.AllowHolidayAccess,
            request.EvaluatedAtUtc);
        var result = await EvaluateInternalAsync(evaluation, nowUtc);
        result.DecisionMode = "Shadow";
        result.LegacyResult = request.LegacyResult?.Trim();
        result.ShadowMismatch = !string.IsNullOrWhiteSpace(result.LegacyResult) &&
                                !string.Equals(result.Result, result.LegacyResult, StringComparison.OrdinalIgnoreCase);

        _context.AccessDecisions.Add(result);
        if (result.ShadowMismatch)
        {
            var correlationId = Guid.NewGuid().ToString("N");
            _context.SecurityEvents.Add(new SecurityEvent
            {
                SourceType = "AccessPolicyShadow",
                SourceId = result.AccessPolicyVersionId?.ToString(),
                EventType = "AccessPolicyShadowMismatch",
                Severity = "Medium",
                SiteId = result.SiteId,
                SecurityZoneId = result.SecurityZoneId,
                AccessPointId = result.AccessPointId,
                SubjectType = result.SubjectType,
                SubjectId = result.SubjectId,
                CorrelationId = correlationId,
                Summary = $"Legacy result {result.LegacyResult} differed from policy result {result.Result}. {request.LegacyReason}".Trim(),
                OccurredAtUtc = nowUtc
            });
            _context.EventCorrelations.Add(new EventCorrelation
            {
                CorrelationId = correlationId,
                RuleName = "AccessPolicyShadowMismatch",
                Severity = "Medium",
                Summary = $"Shadow policy mismatch for {result.SubjectType}:{result.SubjectId}."
            });
        }

        await _context.SaveChangesAsync();
        return Ok(result);
    }

    [HttpGet("duress-events")]
    public async Task<IActionResult> GetDuressEvents([FromQuery] bool? unacknowledged)
    {
        var query = _context.DuressEvents.AsQueryable();
        if (unacknowledged == true)
            query = query.Where(e => !e.IsAcknowledged);
        var events = await query.OrderByDescending(e => e.OccurredAtUtc).Take(50).ToListAsync();
        return Ok(events);
    }

    [HttpPost("duress-events")]
    public async Task<IActionResult> RecordDuressEvent([FromBody] DuressEventRequest request)
    {
        var duress = new DuressEvent
        {
            UserId = request.UserId,
            EmployeeId = request.EmployeeId,
            AccessPointId = request.AccessPointId,
            SiteId = request.SiteId,
            CredentialType = request.CredentialType ?? "Unknown",
            Description = request.Description?.Trim(),
            OccurredAtUtc = DateTime.UtcNow
        };
        _context.DuressEvents.Add(duress);

        _context.Alarms.Add(new Alarm
        {
            AlarmType = "Duress",
            Severity = "Critical",
            State = "New",
            Summary = $"Duress credential used at access point {request.AccessPointId}. User: {request.UserId}",
            SiteId = request.SiteId
        });

        await _context.SaveChangesAsync();
        return Ok(duress);
    }

    [HttpPost("duress-events/{eventId:long}/acknowledge")]
    [RequireStepUp(PrivilegedActions.AccessPolicyEmergency)]
    public async Task<IActionResult> AcknowledgeDuressEvent(long eventId)
    {
        var duress = await _context.DuressEvents.FindAsync(eventId);
        if (duress == null) return NotFound();
        duress.IsAcknowledged = true;
        duress.AcknowledgedAtUtc = DateTime.UtcNow;
        duress.AcknowledgedByUserId = GetCurrentUserId();
        await _context.SaveChangesAsync();
        return Ok(duress);
    }

    private async Task<AccessDecision> EvaluateInternalAsync(AccessEvaluationRequest request, DateTime nowUtc)
    {
        var activePolicyVersionId = await _context.AccessPolicyVersions
            .Where(version => version.Status == "Active")
            .OrderByDescending(version => version.ActivatedAtUtc ?? version.CreatedAtUtc)
            .Select(version => (int?)version.AccessPolicyVersionId)
            .FirstOrDefaultAsync();

        var emergency = await _context.EmergencyStates
            .Where(state => state.IsActive)
            .Where(state =>
                (state.AccessPointId == null || state.AccessPointId == request.AccessPointId) &&
                (state.SecurityZoneId == null || state.SecurityZoneId == request.SecurityZoneId) &&
                (state.SiteId == null || state.SiteId == request.SiteId))
            .OrderByDescending(state => state.StartedAtUtc)
            .FirstOrDefaultAsync();

        if (emergency != null &&
            emergency.State is "FullLockdown" or "PartialLockdown" or "Evacuation" or "ShelterInPlace" &&
            !string.Equals(request.CredentialType, "EmergencyOverride", StringComparison.OrdinalIgnoreCase))
        {
            return BuildDecision(request, activePolicyVersionId, AccessDecisionResults.Deny, $"Emergency state active: {emergency.State}", nowUtc);
        }

        var holiday = await _context.HolidayCalendars.AnyAsync(holiday =>
            holiday.HolidayDate.Date == nowUtc.Date &&
            (holiday.SiteId == null || holiday.SiteId == request.SiteId));
        if (holiday && !request.AllowHolidayAccess)
        {
            return BuildDecision(request, activePolicyVersionId, AccessDecisionResults.Deny, "Holiday access is not allowed by request context.", nowUtc);
        }

        var rulesQuery = _context.AccessRules
            .Include(rule => rule.Schedule)
            .Where(rule => rule.IsActive)
            .Where(rule => rule.SubjectType == request.SubjectType)
            .Where(rule => rule.SubjectId == null || rule.SubjectId == request.SubjectId)
            .Where(rule => rule.AccessPointId == null || rule.AccessPointId == request.AccessPointId)
            .Where(rule => rule.SecurityZoneId == null || rule.SecurityZoneId == request.SecurityZoneId)
            .Where(rule => rule.SiteId == null || rule.SiteId == request.SiteId)
            .Where(rule => rule.CredentialType == "Any" || rule.CredentialType == request.CredentialType)
            .Where(rule => rule.ValidFromUtc == null || rule.ValidFromUtc <= nowUtc)
            .Where(rule => rule.ValidToUtc == null || rule.ValidToUtc >= nowUtc);

        if (activePolicyVersionId.HasValue)
        {
            rulesQuery = rulesQuery.Where(rule =>
                rule.AccessPolicyVersionId == null ||
                rule.AccessPolicyVersionId == activePolicyVersionId.Value);
        }

        var rules = await rulesQuery
            .OrderByDescending(rule => rule.AccessPolicyVersionId == activePolicyVersionId)
            .ThenBy(rule => rule.AllowAccess)
            .ThenBy(rule => rule.AccessRuleId)
            .ToListAsync();

        var scheduledRules = rules
            .Where(rule => IsWithinSchedule(rule.Schedule, nowUtc))
            .ToList();
        var denyRule = scheduledRules.FirstOrDefault(rule => !rule.AllowAccess);
        if (denyRule != null)
        {
            return BuildDecision(
                request,
                activePolicyVersionId,
                AccessDecisionResults.Deny,
                $"Explicit deny rule {denyRule.AccessRuleId} denied access.",
                nowUtc);
        }

        var temporaryGrant = await _context.TemporaryAccessGrants.AnyAsync(grant =>
            !grant.IsRevoked &&
            grant.SubjectType == request.SubjectType &&
            grant.SubjectId == request.SubjectId &&
            grant.ValidFromUtc <= nowUtc &&
            grant.ValidToUtc >= nowUtc &&
            (grant.AccessPointId == null || grant.AccessPointId == request.AccessPointId) &&
            (grant.SecurityZoneId == null || grant.SecurityZoneId == request.SecurityZoneId) &&
            (grant.SiteId == null || grant.SiteId == request.SiteId));

        if (temporaryGrant)
        {
            return BuildDecision(request, activePolicyVersionId, AccessDecisionResults.Allow, "Temporary access grant matched.", nowUtc);
        }

        var allowRule = scheduledRules.FirstOrDefault(rule => rule.AllowAccess);
        if (allowRule == null)
        {
            return BuildDecision(request, activePolicyVersionId, AccessDecisionResults.Deny, "No active access rule matched.", nowUtc);
        }

        return BuildDecision(
            request,
            activePolicyVersionId,
            AccessDecisionResults.Allow,
            $"Access rule {allowRule.AccessRuleId} allowed access.",
            nowUtc);
    }

    private AccessDecision BuildDecision(AccessEvaluationRequest request, int? policyVersionId, string result, string reason, DateTime nowUtc) =>
        new()
        {
            AccessPolicyVersionId = policyVersionId,
            SubjectType = request.SubjectType,
            SubjectId = request.SubjectId,
            SiteId = request.SiteId,
            SecurityZoneId = request.SecurityZoneId,
            AccessPointId = request.AccessPointId,
            CredentialType = request.CredentialType,
            Result = result,
            Reason = reason,
            EvaluatedAtUtc = nowUtc,
            EvaluatedByUserId = GetCurrentUserId()
        };

    private static bool IsWithinSchedule(AccessSchedule? schedule, DateTime nowUtc)
    {
        if (schedule == null)
            return true;
        if (!schedule.IsActive)
            return false;

        var day = nowUtc.DayOfWeek.ToString()[..3];
        var days = schedule.DaysOfWeek.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (!days.Any(item => string.Equals(item, day, StringComparison.OrdinalIgnoreCase)))
            return false;

        var time = nowUtc.TimeOfDay;
        if (schedule.StartTime <= schedule.EndTime)
            return time >= schedule.StartTime && time <= schedule.EndTime;

        return time >= schedule.StartTime || time <= schedule.EndTime;
    }

    private int? GetCurrentUserId()
    {
        var userIdClaim = User.FindFirstValue(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub);
        return int.TryParse(userIdClaim, out var userId) ? userId : null;
    }

    public sealed record ScheduleRequest(string Name, TimeSpan StartTime, TimeSpan EndTime, string? DaysOfWeek, bool IsActive);
    public sealed record HolidayRequest(int? SiteId, string Name, DateTime HolidayDate, string? Note);
    public sealed record AccessLevelRequest(string Name, string Code, string? Description, bool RequiresApproval);
    public sealed record AccessGroupRequest(string Name, string Code);
    public sealed record AccessRuleRequest(
        int? AccessPolicyVersionId,
        int AccessLevelId,
        int? AccessGroupId,
        int? SiteId,
        int? SecurityZoneId,
        int? AccessPointId,
        int? AccessScheduleId,
        string? SubjectType,
        int? SubjectId,
        string? CredentialType,
        bool AllowAccess,
        DateTime? ValidFromUtc,
        DateTime? ValidToUtc,
        bool IsActive);
    public sealed record TemporaryGrantRequest(
        string? SubjectType,
        int SubjectId,
        int? SiteId,
        int? SecurityZoneId,
        int? AccessPointId,
        DateTime ValidFromUtc,
        DateTime ValidToUtc,
        string? Reason);
    public sealed record EmergencyStateRequest(string? State, int? SiteId, int? SecurityZoneId, int? AccessPointId, string? Reason);
    public sealed record AntiPassbackResetRequest(string SubjectType, int SubjectId, int? SecurityZoneId, string? Reason);
    public sealed record OccupancyRequest(int? SiteId, int? SecurityZoneId, int Count, int? MaxAllowed);
    public sealed record AccessEvaluationRequest(
        string SubjectType,
        int? SubjectId,
        int? SiteId,
        int? SecurityZoneId,
        int? AccessPointId,
        string CredentialType,
        bool AllowHolidayAccess,
        DateTime? EvaluatedAtUtc);
    public sealed record AccessShadowCompareRequest(
        string SubjectType,
        int? SubjectId,
        int? SiteId,
        int? SecurityZoneId,
        int? AccessPointId,
        string CredentialType,
        bool AllowHolidayAccess,
        DateTime? EvaluatedAtUtc,
        string? LegacyResult,
        string? LegacyReason);
    public sealed record PolicyVersionRequest(string Name, string? ChangeSummary);
    public sealed record PolicyApprovalRequest(string? Note);
    public sealed record DuressEventRequest(int? UserId, int? EmployeeId, int? AccessPointId, int? SiteId, string? CredentialType, string? Description);
}
