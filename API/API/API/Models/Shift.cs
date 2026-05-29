using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace API.Models;

[Table("Shifts")]
public class Shift
{
    [Key]
    public int ShiftId { get; set; }

    [Required]
    [MaxLength(120)]
    public string ShiftName { get; set; } = null!;

    public TimeSpan StartTime { get; set; }

    public TimeSpan EndTime { get; set; }

    public int BreakMinutes { get; set; }

    public int AllowedLateMinutes { get; set; }

    public int AllowedEarlyLeaveMinutes { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public virtual ICollection<WorkSchedule> WorkSchedules { get; set; } = new List<WorkSchedule>();
}

