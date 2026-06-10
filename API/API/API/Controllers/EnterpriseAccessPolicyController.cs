using System.Security.Claims;
using API.Data;
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

    [HttpPost("rules")]
    public async Task<IActionResult> CreateRule([FromBody] AccessRuleRequest request)
    {
        if (!await _context.AccessLevels.AnyAsync(level => level.AccessLevelId == request.AccessLevelId))
            return BadRequest(new { message = "Access level does not exist." });

        var rule = new AccessRule
        {
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

    [HttpPost("emergency-states")]
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

    private async Task<AccessDecision> EvaluateInternalAsync(AccessEvaluationRequest request, DateTime nowUtc)
    {
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
            return BuildDecision(request, AccessDecisionResults.Deny, $"Emergency state active: {emergency.State}", nowUtc);
        }

        var holiday = await _context.HolidayCalendars.AnyAsync(holiday =>
            holiday.HolidayDate.Date == nowUtc.Date &&
            (holiday.SiteId == null || holiday.SiteId == request.SiteId));
        if (holiday && !request.AllowHolidayAccess)
        {
            return BuildDecision(request, AccessDecisionResults.Deny, "Holiday access is not allowed by request context.", nowUtc);
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
            return BuildDecision(request, AccessDecisionResults.Allow, "Temporary access grant matched.", nowUtc);
        }

        var rules = await _context.AccessRules
            .Include(rule => rule.Schedule)
            .Where(rule => rule.IsActive)
            .Where(rule => rule.SubjectType == request.SubjectType)
            .Where(rule => rule.SubjectId == null || rule.SubjectId == request.SubjectId)
            .Where(rule => rule.AccessPointId == null || rule.AccessPointId == request.AccessPointId)
            .Where(rule => rule.SecurityZoneId == null || rule.SecurityZoneId == request.SecurityZoneId)
            .Where(rule => rule.SiteId == null || rule.SiteId == request.SiteId)
            .Where(rule => rule.CredentialType == "Any" || rule.CredentialType == request.CredentialType)
            .Where(rule => rule.ValidFromUtc == null || rule.ValidFromUtc <= nowUtc)
            .Where(rule => rule.ValidToUtc == null || rule.ValidToUtc >= nowUtc)
            .ToListAsync();

        var matchedRule = rules.FirstOrDefault(rule => IsWithinSchedule(rule.Schedule, nowUtc));
        if (matchedRule == null)
        {
            return BuildDecision(request, AccessDecisionResults.Deny, "No active access rule matched.", nowUtc);
        }

        return matchedRule.AllowAccess
            ? BuildDecision(request, AccessDecisionResults.Allow, $"Access rule {matchedRule.AccessRuleId} allowed access.", nowUtc)
            : BuildDecision(request, AccessDecisionResults.Deny, $"Access rule {matchedRule.AccessRuleId} denied access.", nowUtc);
    }

    private AccessDecision BuildDecision(AccessEvaluationRequest request, string result, string reason, DateTime nowUtc) =>
        new()
        {
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
}

