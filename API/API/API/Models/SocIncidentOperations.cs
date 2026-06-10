using System.ComponentModel.DataAnnotations;

namespace API.Models;

public class Alarm
{
    public long AlarmId { get; set; }
    public long? SecurityEventId { get; set; }
    [MaxLength(80)] public string AlarmType { get; set; } = "Generic";
    [MaxLength(40)] public string Severity { get; set; } = "Medium";
    [MaxLength(40)] public string State { get; set; } = "New";
    [MaxLength(2000)] public string Summary { get; set; } = string.Empty;
    public int? SiteId { get; set; }
    public int? AssignedToUserId { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? AcknowledgedAtUtc { get; set; }
    public DateTime? ClosedAtUtc { get; set; }
}

public class AlarmRule
{
    public int AlarmRuleId { get; set; }
    [MaxLength(160)] public string Name { get; set; } = string.Empty;
    [MaxLength(80)] public string EventType { get; set; } = string.Empty;
    [MaxLength(40)] public string Severity { get; set; } = "Medium";
    public bool IsActive { get; set; } = true;
}

public class AlarmComment
{
    public long AlarmCommentId { get; set; }
    public long AlarmId { get; set; }
    public int? UserId { get; set; }
    [MaxLength(2000)] public string Comment { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public Alarm? Alarm { get; set; }
}

public class SopTemplate
{
    public int SopTemplateId { get; set; }
    [MaxLength(160)] public string Name { get; set; } = string.Empty;
    [MaxLength(80)] public string AlarmType { get; set; } = "Generic";
    public int Version { get; set; } = 1;
    [MaxLength(4000)] public string ChecklistJson { get; set; } = "[]";
    public bool IsActive { get; set; } = true;
}

public class SopExecution
{
    public long SopExecutionId { get; set; }
    public long? AlarmId { get; set; }
    public long? IncidentId { get; set; }
    public int SopTemplateId { get; set; }
    [MaxLength(40)] public string Status { get; set; } = "InProgress";
    [MaxLength(4000)] public string CompletedStepsJson { get; set; } = "[]";
    public int? ExecutedByUserId { get; set; }
    public DateTime StartedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAtUtc { get; set; }
}

public class Incident
{
    public long IncidentId { get; set; }
    [MaxLength(160)] public string Title { get; set; } = string.Empty;
    [MaxLength(40)] public string Severity { get; set; } = "Medium";
    [MaxLength(40)] public string Status { get; set; } = "Open";
    public long? PrimaryAlarmId { get; set; }
    public int? OwnerUserId { get; set; }
    [MaxLength(2000)] public string? Outcome { get; set; }
    public DateTime OpenedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? ClosedAtUtc { get; set; }
}

public class IncidentTimelineItem
{
    public long IncidentTimelineItemId { get; set; }
    public long IncidentId { get; set; }
    [MaxLength(80)] public string ItemType { get; set; } = "Note";
    [MaxLength(2000)] public string Text { get; set; } = string.Empty;
    public int? UserId { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public Incident? Incident { get; set; }
}

public class DispatchTask
{
    public long DispatchTaskId { get; set; }
    public long? AlarmId { get; set; }
    public long? IncidentId { get; set; }
    public int? SiteId { get; set; }
    [MaxLength(160)] public string LocationText { get; set; } = string.Empty;
    [MaxLength(40)] public string Priority { get; set; } = "Medium";
    [MaxLength(40)] public string Status { get; set; } = "Open";
    public int? AssignedGuardUserId { get; set; }
    [MaxLength(2000)] public string Instructions { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAtUtc { get; set; }
}

public class ShiftHandover
{
    public long ShiftHandoverId { get; set; }
    public int? SiteId { get; set; }
    public int? FromUserId { get; set; }
    public int? ToUserId { get; set; }
    [MaxLength(4000)] public string Summary { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}

public class EmergencyMusterSnapshot
{
    public long EmergencyMusterSnapshotId { get; set; }
    public int? SiteId { get; set; }
    public int? MusterPointId { get; set; }
    public int KnownOnsite { get; set; }
    public int AccountedFor { get; set; }
    public int VisitorsOnsite { get; set; }
    public int UnaccountedFor { get; set; }
    public DateTime CapturedAtUtc { get; set; } = DateTime.UtcNow;
}

