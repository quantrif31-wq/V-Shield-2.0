using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace API.Models;

[Table("ZoneTransits")]
public class ZoneTransit
{
    [Key]
    public int ZoneTransitId { get; set; }

    public int EmployeeId { get; set; }

    public int SecurityZoneId { get; set; }

    public int? AccessPointId { get; set; }

    public int? AccessLogId { get; set; }

    public DateTime Timestamp { get; set; }

    [Required]
    [MaxLength(10)]
    public string Direction { get; set; } = "IN";

    [Required]
    [MaxLength(30)]
    public string Source { get; set; } = ZoneTransitSources.AccessLog;

    public bool IsAutoDerived { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [ForeignKey(nameof(EmployeeId))]
    public virtual Employee Employee { get; set; } = null!;

    [ForeignKey(nameof(SecurityZoneId))]
    public virtual SecurityZone SecurityZone { get; set; } = null!;

    [ForeignKey(nameof(AccessPointId))]
    public virtual AccessPoint? AccessPoint { get; set; }

    [ForeignKey(nameof(AccessLogId))]
    public virtual AccessLog? AccessLog { get; set; }

    public virtual ICollection<Attendance> Attendances { get; set; } = new List<Attendance>();
}

public static class ZoneTransitSources
{
    public const string AccessLog = "AccessLog";
    public const string Qr = "QR";
    public const string FaceAi = "FaceAI";
    public const string Manual = "Manual";
    public const string AutoDerived = "AutoDerived";

    public static readonly HashSet<string> All = new(StringComparer.OrdinalIgnoreCase)
    {
        AccessLog, Qr, FaceAi, Manual, AutoDerived
    };
}
