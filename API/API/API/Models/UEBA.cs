using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace API.Models;

[Table("UEBAProfiles")]
public class UEBAProfile
{
    [Key]
    public int ProfileId { get; set; }

    public int EmployeeId { get; set; }

    public int TotalAccessCount { get; set; }

    public int DaysSinceLastAccess { get; set; }

    public double AvgAccessPerDay { get; set; }

    public int TypicalStartHour { get; set; }

    public int TypicalEndHour { get; set; }

    public double WeekendAccessRatio { get; set; }

    public double InOutRatio { get; set; }

    public double BypassRate { get; set; }

    public double RiskScore { get; set; }

    public DateTime LastBuiltAt { get; set; } = DateTime.UtcNow;

    public string? CommonGatesJson { get; set; }

    public string? UnusualHoursJson { get; set; }

    [ForeignKey(nameof(EmployeeId))]
    public virtual Employee Employee { get; set; } = null!;
}

[Table("UEBAAnomalies")]
public class UEBAAnomaly
{
    [Key]
    public int AnomalyId { get; set; }

    public int EmployeeId { get; set; }

    public int? AccessLogId { get; set; }

    public DateTime DetectedAt { get; set; } = DateTime.UtcNow;

    public DateTime EventTimestamp { get; set; }

    [Required]
    [MaxLength(50)]
    public string AnomalyType { get; set; } = UEBAAnomalyTypes.Unknown;

    [Required]
    [MaxLength(20)]
    public string Severity { get; set; } = UEBASeverities.Medium;

    [Required]
    [MaxLength(500)]
    public string Description { get; set; } = string.Empty;

    [MaxLength(200)]
    public string? SupportingData { get; set; }

    [Required]
    [MaxLength(20)]
    public string Status { get; set; } = UEBAStatuses.Open;

    [MaxLength(500)]
    public string? Resolution { get; set; }

    public int? ResolvedBy { get; set; }

    public DateTime? ResolvedAt { get; set; }

    [ForeignKey(nameof(EmployeeId))]
    public virtual Employee Employee { get; set; } = null!;

    [ForeignKey(nameof(AccessLogId))]
    public virtual AccessLog? AccessLog { get; set; }

    [ForeignKey(nameof(ResolvedBy))]
    public virtual AppUser? ResolvedByUser { get; set; }
}

public static class UEBAAnomalyTypes
{
    public const string UnusualTime = "UnusualTime";
    public const string UnusualGate = "UnusualGate";
    public const string UnusualFrequency = "UnusualFrequency";
    public const string OutOfHours = "OutOfHours";
    public const string RapidSuccession = "RapidSuccession";
    public const string BypassPattern = "BypassPattern";
    public const string FirstTimeAccess = "FirstTimeAccess";
    public const string Unknown = "Unknown";

    public static readonly HashSet<string> All = new(StringComparer.OrdinalIgnoreCase)
    {
        UnusualTime, UnusualGate, UnusualFrequency, OutOfHours,
        RapidSuccession, BypassPattern, FirstTimeAccess, Unknown
    };
}

public static class UEBASeverities
{
    public const string High = "cao";
    public const string Medium = "trung-binh";
    public const string Low = "thap";

    public static readonly HashSet<string> All = new(StringComparer.OrdinalIgnoreCase)
    { High, Medium, Low };
}

public static class UEBAStatuses
{
    public const string Open = "Open";
    public const string Resolved = "Resolved";
    public const string FalsePositive = "FalsePositive";

    public static readonly HashSet<string> All = new(StringComparer.OrdinalIgnoreCase)
    { Open, Resolved, FalsePositive };
}
