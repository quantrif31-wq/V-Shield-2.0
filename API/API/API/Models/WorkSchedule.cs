using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace API.Models;

[Table("WorkSchedules")]
public class WorkSchedule
{
    [Key]
    public int ScheduleId { get; set; }

    public int EmployeeId { get; set; }

    public int ShiftId { get; set; }

    public DateTime WorkDate { get; set; }

    [Required]
    [MaxLength(30)]
    public string Status { get; set; } = WorkScheduleStatuses.Scheduled;

    [MaxLength(500)]
    public string? Note { get; set; }

    public int? CreatedBy { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    [ForeignKey(nameof(EmployeeId))]
    public virtual Employee Employee { get; set; } = null!;

    [ForeignKey(nameof(ShiftId))]
    public virtual Shift Shift { get; set; } = null!;

    [ForeignKey(nameof(CreatedBy))]
    public virtual AppUser? CreatedByUser { get; set; }

    public virtual ICollection<Attendance> Attendances { get; set; } = new List<Attendance>();
}

