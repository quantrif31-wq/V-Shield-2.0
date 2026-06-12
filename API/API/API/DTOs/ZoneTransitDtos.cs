namespace API.DTOs;

public class ZoneTransitResponse
{
    public int ZoneTransitId { get; set; }
    public int EmployeeId { get; set; }
    public string EmployeeName { get; set; } = string.Empty;
    public int SecurityZoneId { get; set; }
    public string SecurityZoneName { get; set; } = string.Empty;
    public string SecurityZoneCode { get; set; } = string.Empty;
    public string SecurityLevel { get; set; } = "Normal";
    public string? AccessPointName { get; set; }
    public string? GateName { get; set; }
    public DateTime Timestamp { get; set; }
    public string Direction { get; set; } = "IN";
    public string Source { get; set; } = "AccessLog";
    public bool IsAutoDerived { get; set; }
    public int? AccessLogId { get; set; }
}

public class ZoneTransitQueryRequest
{
    public int? EmployeeId { get; set; }
    public int? DepartmentId { get; set; }
    public int? SecurityZoneId { get; set; }
    public string? Direction { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 50;
}

public class DeriveAttendanceRequest
{
    public int? EmployeeId { get; set; }
    public DateTime? Date { get; set; }
}

public class DeriveAttendanceResult
{
    public int? AttendanceId { get; set; }
    public int EmployeeId { get; set; }
    public string EmployeeName { get; set; } = string.Empty;
    public DateTime WorkDate { get; set; }
    public DateTime? CheckIn { get; set; }
    public DateTime? CheckOut { get; set; }
    public decimal ZoneDwellTime { get; set; }
    public int ZoneTransitCount { get; set; }
    public string Status { get; set; } = "NotCheckedIn";
    public string Message { get; set; } = string.Empty;
}

public class AttendanceWithTransitsResponse
{
    public int AttendanceId { get; set; }
    public int EmployeeId { get; set; }
    public string EmployeeName { get; set; } = string.Empty;
    public int? ScheduleId { get; set; }
    public string? ShiftName { get; set; }
    public DateTime WorkDate { get; set; }
    public DateTime? CheckIn { get; set; }
    public DateTime? CheckOut { get; set; }
    public int LateMinutes { get; set; }
    public int EarlyLeaveMinutes { get; set; }
    public decimal TotalWorkingHours { get; set; }
    public decimal OvertimeHours { get; set; }
    public decimal ZoneDwellTime { get; set; }
    public int ZoneTransitCount { get; set; }
    public bool IsZoneDerived { get; set; }
    public string Status { get; set; } = "NotCheckedIn";
    public string Source { get; set; } = "Manual";
    public string? Note { get; set; }
    public List<ZoneTransitResponse> ZoneTransits { get; set; } = new();
}
