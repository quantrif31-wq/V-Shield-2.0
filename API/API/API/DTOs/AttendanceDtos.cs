using System.ComponentModel.DataAnnotations;

namespace API.DTOs;

public class ShiftUpsertRequest
{
    [Required]
    [MaxLength(120)]
    public string ShiftName { get; set; } = null!;

    [Required]
    public TimeSpan? StartTime { get; set; }

    [Required]
    public TimeSpan? EndTime { get; set; }

    [Range(0, 600)]
    public int BreakMinutes { get; set; }

    [Range(0, 180)]
    public int AllowedLateMinutes { get; set; }

    [Range(0, 180)]
    public int AllowedEarlyLeaveMinutes { get; set; }

    public bool IsActive { get; set; } = true;
}

public class WorkScheduleUpsertRequest
{
    [Required]
    public int EmployeeId { get; set; }

    [Required]
    public int ShiftId { get; set; }

    [Required]
    public DateTime? WorkDate { get; set; }

    [MaxLength(500)]
    public string? Note { get; set; }
}

public class AttendanceCheckInRequest
{
    [Required]
    public int EmployeeId { get; set; }

    [Required]
    [MaxLength(20)]
    public string Source { get; set; } = "Manual";
}

public class AttendanceCheckOutRequest
{
    [Required]
    public int EmployeeId { get; set; }

    [Required]
    [MaxLength(20)]
    public string Source { get; set; } = "Manual";
}

public class AttendanceUpdateRequest
{
    public DateTime? CheckIn { get; set; }
    public DateTime? CheckOut { get; set; }

    [MaxLength(500)]
    public string? Note { get; set; }

    [MaxLength(40)]
    public string? Status { get; set; }

    [MaxLength(20)]
    public string? Source { get; set; }
}

public class AttendanceRecalculateRequest
{
    public int? EmployeeId { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
}

public class LeaveRequestCreateRequest
{
    public int? EmployeeId { get; set; }

    [Required]
    [MaxLength(30)]
    public string LeaveType { get; set; } = null!;

    [Required]
    public DateTime? StartDate { get; set; }

    [Required]
    public DateTime? EndDate { get; set; }

    [Required]
    [MaxLength(2000)]
    public string Reason { get; set; } = null!;
}

public class LeaveRequestRejectRequest
{
    [Required]
    [MaxLength(1000)]
    public string RejectReason { get; set; } = null!;
}

