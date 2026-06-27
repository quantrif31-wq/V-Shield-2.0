using System.ComponentModel.DataAnnotations;

namespace API.Models;

public static class AccessDecisionResults
{
    public const string Allow = "Allow";
    public const string Deny = "Deny";
    public const string Review = "Review";
}

public class AccessSchedule
{
    public int AccessScheduleId { get; set; }
    [MaxLength(160)] public string Name { get; set; } = string.Empty;
    public TimeSpan StartTime { get; set; } = TimeSpan.Zero;
    public TimeSpan EndTime { get; set; } = new(23, 59, 59);
    [MaxLength(40)] public string DaysOfWeek { get; set; } = "Mon,Tue,Wed,Thu,Fri";
    public bool IsActive { get; set; } = true;
}

public class HolidayCalendar
{
    public int HolidayCalendarId { get; set; }
    public int? SiteId { get; set; }
    [MaxLength(160)] public string Name { get; set; } = string.Empty;
    public DateTime HolidayDate { get; set; }
    [MaxLength(500)] public string? Note { get; set; }
    public Site? Site { get; set; }
}

public class AccessLevel
{
    public int AccessLevelId { get; set; }
    [MaxLength(160)] public string Name { get; set; } = string.Empty;
    [MaxLength(50)] public string Code { get; set; } = string.Empty;
    [MaxLength(500)] public string? Description { get; set; }
    public bool RequiresApproval { get; set; }
    public bool IsActive { get; set; } = true;
}

public class AccessGroup
{
    public int AccessGroupId { get; set; }
    [MaxLength(160)] public string Name { get; set; } = string.Empty;
    [MaxLength(50)] public string Code { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}

public class AccessRule
{
    public int AccessRuleId { get; set; }
    public int? AccessPolicyVersionId { get; set; }
    public int AccessLevelId { get; set; }
    public int? AccessGroupId { get; set; }
    public int? SiteId { get; set; }
    public int? SecurityZoneId { get; set; }
    public int? AccessPointId { get; set; }
    public int? AccessScheduleId { get; set; }
    [MaxLength(40)] public string SubjectType { get; set; } = "Employee";
    public int? SubjectId { get; set; }
    [MaxLength(40)] public string CredentialType { get; set; } = "Any";
    public bool AllowAccess { get; set; } = true;
    public DateTime? ValidFromUtc { get; set; }
    public DateTime? ValidToUtc { get; set; }
    public bool IsActive { get; set; } = true;
    public AccessPolicyVersion? AccessPolicyVersion { get; set; }
    public AccessLevel? AccessLevel { get; set; }
    public AccessGroup? AccessGroup { get; set; }
    public Site? Site { get; set; }
    public SecurityZone? SecurityZone { get; set; }
    public AccessPoint? AccessPoint { get; set; }
    public AccessSchedule? Schedule { get; set; }
}

public class TemporaryAccessGrant
{
    public int TemporaryAccessGrantId { get; set; }
    [MaxLength(40)] public string SubjectType { get; set; } = "Employee";
    public int SubjectId { get; set; }
    public int? SiteId { get; set; }
    public int? SecurityZoneId { get; set; }
    public int? AccessPointId { get; set; }
    public DateTime ValidFromUtc { get; set; }
    public DateTime ValidToUtc { get; set; }
    [MaxLength(1000)] public string Reason { get; set; } = string.Empty;
    public int? ApprovedByUserId { get; set; }
    public bool IsRevoked { get; set; }
    public Site? Site { get; set; }
    public SecurityZone? SecurityZone { get; set; }
    public AccessPoint? AccessPoint { get; set; }
    public AppUser? ApprovedByUser { get; set; }
}

public class EmergencyPass
{
    public long EmergencyPassId { get; set; }
    [MaxLength(40)] public string SubjectType { get; set; } = "Person";
    [MaxLength(80)] public string? SubjectId { get; set; }
    [MaxLength(240)] public string SubjectName { get; set; } = string.Empty;
    [MaxLength(40)] public string? PlateNumber { get; set; }
    public int? SiteId { get; set; }
    public int? SecurityZoneId { get; set; }
    [MaxLength(120)] public string? LaneReference { get; set; }
    [MaxLength(160)] public string? LaneName { get; set; }
    [MaxLength(1000)] public string Reason { get; set; } = string.Empty;
    [MaxLength(40)] public string Status { get; set; } = "Active";
    [MaxLength(80)] public string CorrelationId { get; set; } = Guid.NewGuid().ToString("N");
    public int ApprovedByUserId { get; set; }
    public DateTime ValidFromUtc { get; set; } = DateTime.UtcNow;
    public DateTime ValidToUtc { get; set; }
    public long? AlarmId { get; set; }
    public long? LaneEventId { get; set; }
    public AppUser? ApprovedByUser { get; set; }
    public Site? Site { get; set; }
    public SecurityZone? SecurityZone { get; set; }
}

public class AccessPolicyVersion
{
    public int AccessPolicyVersionId { get; set; }
    [MaxLength(160)] public string Name { get; set; } = string.Empty;
    [MaxLength(40)] public string Status { get; set; } = "Draft";
    [MaxLength(1000)] public string? ChangeSummary { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? SubmittedAtUtc { get; set; }
    public DateTime? ApprovedAtUtc { get; set; }
    public DateTime? ActivatedAtUtc { get; set; }
    public DateTime? RetiredAtUtc { get; set; }
    public int? CreatedByUserId { get; set; }
    public int? ApprovedByUserId { get; set; }
    public AppUser? CreatedByUser { get; set; }
    public AppUser? ApprovedByUser { get; set; }
    public ICollection<AccessRule> Rules { get; set; } = new List<AccessRule>();
}

public class AccessDecision
{
    public long AccessDecisionId { get; set; }
    public int? AccessPolicyVersionId { get; set; }
    [MaxLength(40)] public string SubjectType { get; set; } = "Employee";
    public int? SubjectId { get; set; }
    public int? SiteId { get; set; }
    public int? SecurityZoneId { get; set; }
    public int? AccessPointId { get; set; }
    [MaxLength(40)] public string CredentialType { get; set; } = "Unknown";
    [MaxLength(20)] public string Result { get; set; } = AccessDecisionResults.Deny;
    [MaxLength(1000)] public string Reason { get; set; } = string.Empty;
    [MaxLength(40)] public string DecisionMode { get; set; } = "Enforced";
    [MaxLength(20)] public string? LegacyResult { get; set; }
    public bool ShadowMismatch { get; set; }
    public DateTime EvaluatedAtUtc { get; set; } = DateTime.UtcNow;
    public int? EvaluatedByUserId { get; set; }
    public AccessPolicyVersion? AccessPolicyVersion { get; set; }
}

public class AntiPassbackState
{
    public int AntiPassbackStateId { get; set; }
    [MaxLength(40)] public string SubjectType { get; set; } = "Employee";
    public int SubjectId { get; set; }
    public int? SecurityZoneId { get; set; }
    [MaxLength(40)] public string State { get; set; } = "Unknown";
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
    public bool IsViolated { get; set; }
    [MaxLength(500)] public string? ResetReason { get; set; }
}

public class OccupancySnapshot
{
    public long OccupancySnapshotId { get; set; }
    public int? SiteId { get; set; }
    public int? SecurityZoneId { get; set; }
    public int Count { get; set; }
    public int? MaxAllowed { get; set; }
    public DateTime CapturedAtUtc { get; set; } = DateTime.UtcNow;
}

public class DuressEvent
{
    public long DuressEventId { get; set; }
    public int? UserId { get; set; }
    public int? EmployeeId { get; set; }
    public int? AccessPointId { get; set; }
    public int? SecurityZoneId { get; set; }
    public int? SiteId { get; set; }
    [MaxLength(40)] public string CredentialType { get; set; } = "Unknown";
    [MaxLength(200)] public string? Description { get; set; }
    public bool IsAcknowledged { get; set; }
    public DateTime OccurredAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? AcknowledgedAtUtc { get; set; }
    public int? AcknowledgedByUserId { get; set; }
    public Site? Site { get; set; }
    public SecurityZone? SecurityZone { get; set; }
}

public class EmergencyState
{
    public int EmergencyStateId { get; set; }
    [MaxLength(40)] public string State { get; set; } = "Normal";
    public int? SiteId { get; set; }
    public int? SecurityZoneId { get; set; }
    public int? AccessPointId { get; set; }
    [MaxLength(1000)] public string Reason { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTime StartedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? EndedAtUtc { get; set; }
    public int? CreatedByUserId { get; set; }
}
