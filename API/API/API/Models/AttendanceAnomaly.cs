using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace API.Models;

[Table("AttendanceAnomalies")]
public class AttendanceAnomaly
{
    [Key]
    public int AnomalyId { get; set; }

    public int EmployeeId { get; set; }

    public int? AttendanceId { get; set; }

    public DateTime WorkDate { get; set; }

    [Required]
    [MaxLength(50)]
    public string AnomalyType { get; set; } = AttendanceAnomalyTypes.Unknown;

    [Required]
    [MaxLength(20)]
    public string Severity { get; set; } = AnomalySeverities.Medium;

    [Required]
    [MaxLength(500)]
    public string Description { get; set; } = string.Empty;

    [MaxLength(200)]
    public string? SupportingData { get; set; }

    [Required]
    [MaxLength(20)]
    public string Status { get; set; } = AnomalyStatuses.Open;

    [MaxLength(500)]
    public string? Resolution { get; set; }

    public int? ResolvedBy { get; set; }

    public DateTime? ResolvedAt { get; set; }

    public DateTime DetectedAt { get; set; } = DateTime.UtcNow;

    [ForeignKey(nameof(EmployeeId))]
    public virtual Employee Employee { get; set; } = null!;

    [ForeignKey(nameof(AttendanceId))]
    public virtual Attendance? Attendance { get; set; }

    [ForeignKey(nameof(ResolvedBy))]
    public virtual AppUser? ResolvedByUser { get; set; }
}

public static class AttendanceAnomalyTypes
{
    public const string BuddyPunching = "BuddyPunching";
    public const string SuspiciousTime = "SuspiciousTime";
    public const string MissingCheckOut = "MissingCheckOut";
    public const string ZoneMismatch = "ZoneMismatch";
    public const string AbsencePattern = "AbsencePattern";
    public const string DuplicateCheckIn = "DuplicateCheckIn";
    public const string Unknown = "Unknown";

    public static readonly HashSet<string> All = new(StringComparer.OrdinalIgnoreCase)
    {
        BuddyPunching, SuspiciousTime, MissingCheckOut, ZoneMismatch,
        AbsencePattern, DuplicateCheckIn, Unknown
    };
}

public static class AnomalySeverities
{
    public const string High = "cao";
    public const string Medium = "trung-binh";
    public const string Low = "thap";

    public static readonly HashSet<string> All = new(StringComparer.OrdinalIgnoreCase)
    {
        High, Medium, Low
    };
}

public static class AnomalyStatuses
{
    public const string Open = "Open";
    public const string Resolved = "Resolved";
    public const string FalsePositive = "FalsePositive";

    public static readonly HashSet<string> All = new(StringComparer.OrdinalIgnoreCase)
    {
        Open, Resolved, FalsePositive
    };
}
