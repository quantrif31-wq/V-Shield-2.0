using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace API.Models;

[Table("Attendances")]
public class Attendance
{
    [Key]
    public int AttendanceId { get; set; }

    public int EmployeeId { get; set; }

    public int? ScheduleId { get; set; }

    public DateTime WorkDate { get; set; }

    public DateTime? CheckIn { get; set; }

    public DateTime? CheckOut { get; set; }

    public int LateMinutes { get; set; }

    public int EarlyLeaveMinutes { get; set; }

    [Column(TypeName = "decimal(8,2)")]
    public decimal TotalWorkingHours { get; set; }

    [Column(TypeName = "decimal(8,2)")]
    public decimal OvertimeHours { get; set; }

    [Required]
    [MaxLength(40)]
    public string Status { get; set; } = AttendanceStatuses.NotCheckedIn;

    [Required]
    [MaxLength(20)]
    public string Source { get; set; } = AttendanceSources.Manual;

    [MaxLength(500)]
    public string? Note { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    [ForeignKey(nameof(EmployeeId))]
    public virtual Employee Employee { get; set; } = null!;

    [ForeignKey(nameof(ScheduleId))]
    public virtual WorkSchedule? Schedule { get; set; }
}

