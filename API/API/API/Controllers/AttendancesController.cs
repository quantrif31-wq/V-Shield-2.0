using API.Data;
using API.DTOs;
using API.Models;
using API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace API.Controllers;

[ApiController]
[Route("api/attendances")]
[Authorize]
public class AttendancesController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly IAttendancePermissionService _permissionService;
    private readonly IAttendanceCalculationService _calculationService;
    private readonly IZoneTransitService _zoneTransitService;
    private readonly IAttendanceZoneService _attendanceZoneService;
    private readonly IAttendanceAnomalyService _anomalyService;

    public AttendancesController(
        ApplicationDbContext context,
        IAttendancePermissionService permissionService,
        IAttendanceCalculationService calculationService,
        IZoneTransitService zoneTransitService,
        IAttendanceZoneService attendanceZoneService,
        IAttendanceAnomalyService anomalyService)
    {
        _context = context;
        _permissionService = permissionService;
        _calculationService = calculationService;
        _zoneTransitService = zoneTransitService;
        _attendanceZoneService = attendanceZoneService;
        _anomalyService = anomalyService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] int? employeeId,
        [FromQuery] int? departmentId,
        [FromQuery] string? status,
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate)
    {
        var query = _context.Attendances
            .AsNoTracking()
            .Include(x => x.Employee).ThenInclude(e => e.Department)
            .Include(x => x.Schedule).ThenInclude(s => s!.Shift)
            .AsQueryable();

        query = await ApplyScopeAsync(query);

        if (employeeId.HasValue) query = query.Where(x => x.EmployeeId == employeeId.Value);
        if (departmentId.HasValue) query = query.Where(x => x.Employee.DepartmentId == departmentId.Value);
        if (!string.IsNullOrWhiteSpace(status)) query = query.Where(x => x.Status == status.Trim());
        if (fromDate.HasValue) query = query.Where(x => x.WorkDate >= fromDate.Value.Date);
        if (toDate.HasValue) query = query.Where(x => x.WorkDate <= toDate.Value.Date);

        var data = await query
            .OrderByDescending(x => x.WorkDate)
            .ThenByDescending(x => x.CheckIn)
            .Select(x => new
            {
                x.AttendanceId,
                x.EmployeeId,
                employeeName = x.Employee.FullName,
                departmentId = x.Employee.DepartmentId,
                departmentName = x.Employee.Department != null ? x.Employee.Department.Name : null,
                x.ScheduleId,
                shiftName = x.Schedule != null ? x.Schedule.Shift.ShiftName : null,
                shiftStartTime = x.Schedule != null ? x.Schedule.Shift.StartTime : (TimeSpan?)null,
                shiftEndTime = x.Schedule != null ? x.Schedule.Shift.EndTime : (TimeSpan?)null,
                x.WorkDate,
                x.CheckIn,
                x.CheckOut,
                x.LateMinutes,
                x.EarlyLeaveMinutes,
                x.TotalWorkingHours,
                x.OvertimeHours,
                x.Status,
                x.Source,
                x.Note,
                x.CreatedAt,
                x.UpdatedAt
            })
            .ToListAsync();

        return Ok(data);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var attendance = await _context.Attendances
            .AsNoTracking()
            .Include(x => x.Employee).ThenInclude(e => e.Department)
            .Include(x => x.Schedule).ThenInclude(s => s!.Shift)
            .FirstOrDefaultAsync(x => x.AttendanceId == id);

        if (attendance == null)
            return NotFound(new { message = $"Khong tim thay cham cong ID {id}" });

        if (!await CanViewEmployeeAsync(attendance.EmployeeId))
            return Forbid();

        return Ok(new
        {
            attendance.AttendanceId,
            attendance.EmployeeId,
            employeeName = attendance.Employee.FullName,
            departmentId = attendance.Employee.DepartmentId,
            departmentName = attendance.Employee.Department?.Name,
            attendance.ScheduleId,
            shiftName = attendance.Schedule != null ? attendance.Schedule.Shift.ShiftName : null,
            shiftStartTime = attendance.Schedule != null ? attendance.Schedule.Shift.StartTime : (TimeSpan?)null,
            shiftEndTime = attendance.Schedule != null ? attendance.Schedule.Shift.EndTime : (TimeSpan?)null,
            attendance.WorkDate,
            attendance.CheckIn,
            attendance.CheckOut,
            attendance.LateMinutes,
            attendance.EarlyLeaveMinutes,
            attendance.TotalWorkingHours,
            attendance.OvertimeHours,
            attendance.Status,
            attendance.Source,
            attendance.Note,
            attendance.CreatedAt,
            attendance.UpdatedAt
        });
    }

    [HttpGet("employee/{employeeId:int}")]
    public async Task<IActionResult> GetByEmployee(int employeeId)
    {
        if (!await CanViewEmployeeAsync(employeeId))
            return Forbid();

        var data = await _context.Attendances
            .AsNoTracking()
            .Include(x => x.Schedule).ThenInclude(s => s!.Shift)
            .Where(x => x.EmployeeId == employeeId)
            .OrderByDescending(x => x.WorkDate)
            .Select(x => new
            {
                x.AttendanceId,
                x.EmployeeId,
                x.ScheduleId,
                shiftName = x.Schedule != null ? x.Schedule.Shift.ShiftName : null,
                x.WorkDate,
                x.CheckIn,
                x.CheckOut,
                x.LateMinutes,
                x.EarlyLeaveMinutes,
                x.TotalWorkingHours,
                x.OvertimeHours,
                x.Status,
                x.Source,
                x.Note,
                x.CreatedAt,
                x.UpdatedAt
            })
            .ToListAsync();

        return Ok(data);
    }

    [HttpGet("zone-transits")]
    public async Task<IActionResult> GetZoneTransits(
        [FromQuery] int? employeeId,
        [FromQuery] int? departmentId,
        [FromQuery] int? securityZoneId,
        [FromQuery] string? direction,
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        var data = await _zoneTransitService.QueryTransitsAsync(employeeId, departmentId, securityZoneId, direction, fromDate, toDate, page, pageSize);

        var result = data.Select(t => new ZoneTransitResponse
        {
            ZoneTransitId = t.ZoneTransitId,
            EmployeeId = t.EmployeeId,
            EmployeeName = t.Employee.FullName,
            SecurityZoneId = t.SecurityZoneId,
            SecurityZoneName = t.SecurityZone.Name,
            SecurityZoneCode = t.SecurityZone.Code,
            SecurityLevel = t.SecurityZone.SecurityLevel,
            AccessPointName = t.AccessPoint?.Name,
            GateName = t.AccessLog?.Gate?.GateName ?? t.AccessLog?.GateNameSnapshot,
            Timestamp = t.Timestamp,
            Direction = t.Direction,
            Source = t.Source,
            IsAutoDerived = t.IsAutoDerived,
            AccessLogId = t.AccessLogId
        }).ToList();

        return Ok(result);
    }

    [HttpGet("{id:int}/transits")]
    public async Task<IActionResult> GetAttendanceTransits(int id)
    {
        var attendance = await _context.Attendances
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.AttendanceId == id);

        if (attendance == null)
            return NotFound(new { message = $"Khong tim thay cham cong ID {id}" });

        if (!await CanViewEmployeeAsync(attendance.EmployeeId))
            return Forbid();

        var transits = await _zoneTransitService.GetTransitsAsync(attendance.EmployeeId, attendance.WorkDate);

        var result = transits.Select(t => new ZoneTransitResponse
        {
            ZoneTransitId = t.ZoneTransitId,
            EmployeeId = t.EmployeeId,
            EmployeeName = t.Employee.FullName,
            SecurityZoneId = t.SecurityZoneId,
            SecurityZoneName = t.SecurityZone.Name,
            SecurityZoneCode = t.SecurityZone.Code,
            SecurityLevel = t.SecurityZone.SecurityLevel,
            AccessPointName = t.AccessPoint?.Name,
            GateName = t.AccessLog?.Gate?.GateName ?? t.AccessLog?.GateNameSnapshot,
            Timestamp = t.Timestamp,
            Direction = t.Direction,
            Source = t.Source,
            IsAutoDerived = t.IsAutoDerived,
            AccessLogId = t.AccessLogId
        }).ToList();

        return Ok(result);
    }

    [HttpPost("derive")]
    public async Task<IActionResult> DeriveAttendance([FromBody] DeriveAttendanceRequest? request)
    {
        if (!await EnsureCanManageAsync()) return Forbid();

        var employeeId = request?.EmployeeId;
        var date = request?.Date?.Date ?? DateTime.Today;

        if (employeeId.HasValue)
        {
            var result = await _attendanceZoneService.DeriveAttendanceAsync(employeeId.Value, date);
            return Ok(result);
        }

        var batchResult = await _attendanceZoneService.DeriveBatchAsync(date, date, null);
        return Ok(batchResult);
    }

    [HttpPost("derive-batch")]
    public async Task<IActionResult> DeriveBatch([FromBody] AttendanceRecalculateRequest? request)
    {
        if (!await EnsureCanManageAsync()) return Forbid();

        var fromDate = request?.FromDate?.Date ?? DateTime.Today.AddDays(-7);
        var toDate = request?.ToDate?.Date ?? DateTime.Today;

        if (toDate < fromDate)
            return BadRequest(new { message = "toDate khong duoc nho hon fromDate." });

        var result = await _attendanceZoneService.DeriveBatchAsync(fromDate, toDate, request?.EmployeeId);
        return Ok(result);
    }

    [HttpPost("check-in")]
    public async Task<IActionResult> CheckIn([FromBody] AttendanceCheckInRequest request)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        if (!AttendanceSources.All.Contains(request.Source))
            return BadRequest(new { message = "Nguon cham cong khong hop le." });

        if (!await CanOperateAttendanceAsync(request.EmployeeId))
            return Forbid();

        var employee = await _context.Employees
            .FirstOrDefaultAsync(e => e.EmployeeId == request.EmployeeId);
        if (employee == null) return NotFound(new { message = "Nhan vien khong ton tai." });
        if (employee.Status != true) return BadRequest(new { message = "Nhan vien da ngung hoat dong." });

        var now = DateTime.Now;
        var workDate = now.Date;

        var schedule = await _context.WorkSchedules
            .Include(s => s.Shift)
            .Where(s => s.EmployeeId == request.EmployeeId &&
                        s.WorkDate == workDate &&
                        s.Status != WorkScheduleStatuses.Cancelled &&
                        s.Status != WorkScheduleStatuses.Leave)
            .OrderBy(s => s.Shift.StartTime)
            .FirstOrDefaultAsync();

        Attendance? attendance;
        if (schedule != null)
        {
            attendance = await _context.Attendances
                .FirstOrDefaultAsync(a =>
                    a.EmployeeId == request.EmployeeId &&
                    a.WorkDate == workDate &&
                    a.ScheduleId == schedule.ScheduleId);
        }
        else
        {
            attendance = await _context.Attendances
                .FirstOrDefaultAsync(a =>
                    a.EmployeeId == request.EmployeeId &&
                    a.WorkDate == workDate &&
                    a.ScheduleId == null);
        }

        if (attendance != null && attendance.CheckIn.HasValue)
            return BadRequest(new { message = "Nhan vien da check-in trong ngay." });

        if (attendance == null)
        {
            attendance = new Attendance
            {
                EmployeeId = request.EmployeeId,
                ScheduleId = schedule?.ScheduleId,
                WorkDate = workDate,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _context.Attendances.Add(attendance);
        }

        attendance.CheckIn = now;
        attendance.Source = request.Source;
        attendance.UpdatedAt = DateTime.UtcNow;

        if (schedule?.Shift != null)
        {
            var calc = _calculationService.Calculate(workDate, attendance.CheckIn, null, schedule.Shift);
            attendance.LateMinutes = calc.LateMinutes;
            attendance.Status = calc.Status;
        }
        else
        {
            attendance.Status = AttendanceStatuses.OutOfSchedule;
            attendance.LateMinutes = 0;
        }

        await _context.SaveChangesAsync();
        return await GetById(attendance.AttendanceId);
    }

    [HttpPost("check-out")]
    public async Task<IActionResult> CheckOut([FromBody] AttendanceCheckOutRequest request)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        if (!AttendanceSources.All.Contains(request.Source))
            return BadRequest(new { message = "Nguon cham cong khong hop le." });

        if (!await CanOperateAttendanceAsync(request.EmployeeId))
            return Forbid();

        var now = DateTime.Now;
        var workDate = now.Date;

        var attendance = await _context.Attendances
            .Include(x => x.Schedule).ThenInclude(s => s!.Shift)
            .Where(a => a.EmployeeId == request.EmployeeId &&
                        a.WorkDate == workDate &&
                        a.CheckIn != null)
            .OrderByDescending(a => a.AttendanceId)
            .FirstOrDefaultAsync();

        if (attendance == null)
            return BadRequest(new { message = "Chua co du lieu check-in trong ngay." });
        if (attendance.CheckOut.HasValue)
            return BadRequest(new { message = "Ban ghi nay da check-out." });
        if (attendance.CheckIn.HasValue && now < attendance.CheckIn.Value)
            return BadRequest(new { message = "Check-out khong duoc som hon check-in." });

        attendance.CheckOut = now;
        attendance.Source = request.Source;
        attendance.UpdatedAt = DateTime.UtcNow;

        if (attendance.Schedule?.Shift != null)
        {
            var calc = _calculationService.Calculate(workDate, attendance.CheckIn, attendance.CheckOut, attendance.Schedule.Shift);
            attendance.LateMinutes = calc.LateMinutes;
            attendance.EarlyLeaveMinutes = calc.EarlyLeaveMinutes;
            attendance.TotalWorkingHours = calc.TotalWorkingHours;
            attendance.OvertimeHours = calc.OvertimeHours;
            attendance.Status = calc.Status;

            if (!string.Equals(attendance.Schedule.Status, WorkScheduleStatuses.Leave, StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(attendance.Schedule.Status, WorkScheduleStatuses.Cancelled, StringComparison.OrdinalIgnoreCase))
            {
                attendance.Schedule.Status = WorkScheduleStatuses.Worked;
                attendance.Schedule.UpdatedAt = DateTime.UtcNow;
            }
        }
        else
        {
            var calc = _calculationService.Calculate(workDate, attendance.CheckIn, attendance.CheckOut, null);
            attendance.LateMinutes = 0;
            attendance.EarlyLeaveMinutes = 0;
            attendance.TotalWorkingHours = calc.TotalWorkingHours;
            attendance.OvertimeHours = 0;
            attendance.Status = AttendanceStatuses.OutOfSchedule;
        }

        await _context.SaveChangesAsync();
        return await GetById(attendance.AttendanceId);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] AttendanceUpdateRequest request)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        if (!await EnsureCanManageAsync()) return Forbid();

        var attendance = await _context.Attendances
            .Include(x => x.Schedule).ThenInclude(s => s!.Shift)
            .FirstOrDefaultAsync(x => x.AttendanceId == id);
        if (attendance == null)
            return NotFound(new { message = $"Khong tim thay cham cong ID {id}" });

        if (!await _permissionService.CanManageEmployeeAsync(User, attendance.EmployeeId))
            return Forbid();

        if (!string.IsNullOrWhiteSpace(request.Source))
        {
            if (!AttendanceSources.All.Contains(request.Source))
                return BadRequest(new { message = "Nguon cham cong khong hop le." });
            attendance.Source = request.Source;
        }

        if (!string.IsNullOrWhiteSpace(request.Status))
        {
            if (!AttendanceStatuses.All.Contains(request.Status))
                return BadRequest(new { message = "Trang thai cham cong khong hop le." });
            attendance.Status = request.Status;
        }

        if (request.CheckIn.HasValue)
            attendance.CheckIn = request.CheckIn.Value;

        if (request.CheckOut.HasValue)
            attendance.CheckOut = request.CheckOut.Value;

        if (attendance.CheckIn.HasValue && attendance.CheckOut.HasValue && attendance.CheckOut < attendance.CheckIn)
            return BadRequest(new { message = "Check-out khong duoc som hon check-in." });

        attendance.Note = request.Note?.Trim();
        attendance.UpdatedAt = DateTime.UtcNow;

        var calc = _calculationService.Calculate(
            attendance.WorkDate,
            attendance.CheckIn,
            attendance.CheckOut,
            attendance.Schedule?.Shift);

        attendance.LateMinutes = calc.LateMinutes;
        attendance.EarlyLeaveMinutes = calc.EarlyLeaveMinutes;
        attendance.TotalWorkingHours = calc.TotalWorkingHours;
        attendance.OvertimeHours = calc.OvertimeHours;

        if (string.IsNullOrWhiteSpace(request.Status))
            attendance.Status = calc.Status;

        await _context.SaveChangesAsync();
        return await GetById(attendance.AttendanceId);
    }

    [HttpPost("recalculate")]
    public async Task<IActionResult> Recalculate([FromBody] AttendanceRecalculateRequest? request)
    {
        if (!await EnsureCanManageAsync()) return Forbid();

        var fromDate = request?.FromDate?.Date ?? DateTime.Today;
        var toDate = request?.ToDate?.Date ?? fromDate;
        if (toDate < fromDate)
            return BadRequest(new { message = "toDate khong duoc nho hon fromDate." });

        var scheduleQuery = _context.WorkSchedules
            .Include(s => s.Shift)
            .Where(s => s.WorkDate >= fromDate && s.WorkDate <= toDate);

        if (request?.EmployeeId.HasValue == true)
            scheduleQuery = scheduleQuery.Where(s => s.EmployeeId == request.EmployeeId.Value);

        scheduleQuery = await ApplyManageScopeToSchedulesAsync(scheduleQuery);

        var schedules = await scheduleQuery.ToListAsync();

        var attendanceQuery = _context.Attendances
            .Where(a => a.WorkDate >= fromDate && a.WorkDate <= toDate);

        if (request?.EmployeeId.HasValue == true)
            attendanceQuery = attendanceQuery.Where(a => a.EmployeeId == request.EmployeeId.Value);

        attendanceQuery = await ApplyManageScopeToAttendancesAsync(attendanceQuery);
        var attendances = await attendanceQuery.ToListAsync();

        var approvedLeaveQuery = _context.LeaveRequests
            .Where(l => l.Status == LeaveRequestStatuses.Approved &&
                        l.StartDate <= toDate &&
                        l.EndDate >= fromDate);

        if (request?.EmployeeId.HasValue == true)
            approvedLeaveQuery = approvedLeaveQuery.Where(l => l.EmployeeId == request.EmployeeId.Value);

        approvedLeaveQuery = await ApplyManageScopeToLeavesAsync(approvedLeaveQuery);
        var approvedLeaves = await approvedLeaveQuery.ToListAsync();

        var createdCount = 0;
        var updatedCount = 0;
        var today = DateTime.Today;

        foreach (var schedule in schedules)
        {
            if (string.Equals(schedule.Status, WorkScheduleStatuses.Cancelled, StringComparison.OrdinalIgnoreCase))
                continue;

            var inApprovedLeave = approvedLeaves.Any(l =>
                l.EmployeeId == schedule.EmployeeId &&
                l.StartDate.Date <= schedule.WorkDate.Date &&
                l.EndDate.Date >= schedule.WorkDate.Date);

            var relatedAttendance = attendances.FirstOrDefault(a => a.ScheduleId == schedule.ScheduleId);

            if (inApprovedLeave)
            {
                schedule.Status = WorkScheduleStatuses.Leave;
                schedule.UpdatedAt = DateTime.UtcNow;

                if (relatedAttendance == null)
                {
                    relatedAttendance = new Attendance
                    {
                        EmployeeId = schedule.EmployeeId,
                        ScheduleId = schedule.ScheduleId,
                        WorkDate = schedule.WorkDate,
                        Status = AttendanceStatuses.Leave,
                        Source = AttendanceSources.Manual,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };
                    _context.Attendances.Add(relatedAttendance);
                    attendances.Add(relatedAttendance);
                    createdCount++;
                }
                else
                {
                    relatedAttendance.Status = AttendanceStatuses.Leave;
                    relatedAttendance.UpdatedAt = DateTime.UtcNow;
                    updatedCount++;
                }

                continue;
            }

            if (relatedAttendance == null)
            {
                relatedAttendance = new Attendance
                {
                    EmployeeId = schedule.EmployeeId,
                    ScheduleId = schedule.ScheduleId,
                    WorkDate = schedule.WorkDate,
                    Status = AttendanceStatuses.Absent,
                    Source = AttendanceSources.Manual,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                _context.Attendances.Add(relatedAttendance);
                attendances.Add(relatedAttendance);
                schedule.Status = WorkScheduleStatuses.Absent;
                schedule.UpdatedAt = DateTime.UtcNow;
                createdCount++;
                continue;
            }

            if (!relatedAttendance.CheckIn.HasValue)
            {
                relatedAttendance.Status = AttendanceStatuses.Absent;
                relatedAttendance.UpdatedAt = DateTime.UtcNow;
                schedule.Status = WorkScheduleStatuses.Absent;
                schedule.UpdatedAt = DateTime.UtcNow;
                updatedCount++;
                continue;
            }

            if (relatedAttendance.CheckIn.HasValue && !relatedAttendance.CheckOut.HasValue && schedule.WorkDate.Date < today)
            {
                relatedAttendance.Status = AttendanceStatuses.ForgotCheckout;
                relatedAttendance.UpdatedAt = DateTime.UtcNow;
                updatedCount++;
                continue;
            }

            var calc = _calculationService.Calculate(schedule.WorkDate, relatedAttendance.CheckIn, relatedAttendance.CheckOut, schedule.Shift);
            relatedAttendance.LateMinutes = calc.LateMinutes;
            relatedAttendance.EarlyLeaveMinutes = calc.EarlyLeaveMinutes;
            relatedAttendance.TotalWorkingHours = calc.TotalWorkingHours;
            relatedAttendance.OvertimeHours = calc.OvertimeHours;
            relatedAttendance.Status = calc.Status;
            relatedAttendance.UpdatedAt = DateTime.UtcNow;

            if (relatedAttendance.CheckOut.HasValue)
                schedule.Status = WorkScheduleStatuses.Worked;
            else
                schedule.Status = WorkScheduleStatuses.Scheduled;

            schedule.UpdatedAt = DateTime.UtcNow;
            updatedCount++;
        }

        foreach (var attendance in attendances.Where(a => a.ScheduleId == null))
        {
            if (!attendance.CheckIn.HasValue)
            {
                attendance.Status = AttendanceStatuses.NotCheckedIn;
            }
            else if (!attendance.CheckOut.HasValue && attendance.WorkDate.Date < today)
            {
                attendance.Status = AttendanceStatuses.ForgotCheckout;
            }
            else if (attendance.CheckOut.HasValue)
            {
                var calc = _calculationService.Calculate(attendance.WorkDate, attendance.CheckIn, attendance.CheckOut, null);
                attendance.TotalWorkingHours = calc.TotalWorkingHours;
                attendance.Status = AttendanceStatuses.OutOfSchedule;
            }
            attendance.UpdatedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();

        return Ok(new
        {
            message = "Da tinh lai du lieu cham cong.",
            fromDate,
            toDate,
            created = createdCount,
            updated = updatedCount
        });
    }

    private async Task<IQueryable<Attendance>> ApplyScopeAsync(IQueryable<Attendance> query)
    {
        if (_permissionService.IsAdmin(User) || _permissionService.IsSecurity(User))
            return query;

        if (await _permissionService.IsManagerAsync(User))
        {
            var deptId = await _permissionService.GetUserDepartmentIdAsync(User);
            if (!deptId.HasValue) return query.Where(_ => false);
            return query.Where(a => a.Employee.DepartmentId == deptId.Value);
        }

        var currentEmployeeId = _permissionService.GetCurrentEmployeeId(User);
        if (!currentEmployeeId.HasValue) return query.Where(_ => false);
        return query.Where(a => a.EmployeeId == currentEmployeeId.Value);
    }

    private async Task<IQueryable<WorkSchedule>> ApplyManageScopeToSchedulesAsync(IQueryable<WorkSchedule> query)
    {
        if (_permissionService.IsAdmin(User)) return query;

        var deptId = await _permissionService.GetUserDepartmentIdAsync(User);
        if (!deptId.HasValue) return query.Where(_ => false);

        return query.Where(s => s.Employee.DepartmentId == deptId.Value);
    }

    private async Task<IQueryable<Attendance>> ApplyManageScopeToAttendancesAsync(IQueryable<Attendance> query)
    {
        if (_permissionService.IsAdmin(User)) return query;

        var deptId = await _permissionService.GetUserDepartmentIdAsync(User);
        if (!deptId.HasValue) return query.Where(_ => false);

        return query.Where(a => a.Employee.DepartmentId == deptId.Value);
    }

    private async Task<IQueryable<LeaveRequest>> ApplyManageScopeToLeavesAsync(IQueryable<LeaveRequest> query)
    {
        if (_permissionService.IsAdmin(User)) return query;

        var deptId = await _permissionService.GetUserDepartmentIdAsync(User);
        if (!deptId.HasValue) return query.Where(_ => false);

        return query.Where(l => l.Employee.DepartmentId == deptId.Value);
    }

    [HttpGet("anomalies")]
    [Authorize(Roles = "Admin,BaoVe,QuanLy")]
    public async Task<IActionResult> GetAnomalies(
        [FromQuery] int? employeeId,
        [FromQuery] string? type,
        [FromQuery] string? severity,
        [FromQuery] string? status,
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate,
        [FromQuery] int maxResults = 50)
    {
        var results = await _anomalyService.GetAnomaliesAsync(
            employeeId, type, severity, status, fromDate, toDate, maxResults);
        return Ok(results);
    }

    [HttpPost("anomalies/detect")]
    [Authorize(Roles = "Admin,BaoVe,QuanLy")]
    public async Task<IActionResult> DetectAnomalies(
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate)
    {
        var anomalies = await _anomalyService.DetectAnomaliesAsync(fromDate, toDate);
        return Ok(new { detected = anomalies.Count, anomalies });
    }

    [HttpPost("anomalies/{id:int}/resolve")]
    [Authorize(Roles = "Admin,BaoVe,QuanLy")]
    public async Task<IActionResult> ResolveAnomaly(int id, [FromBody] AnomalyResolveRequest request)
    {
        var currentUserId = _permissionService.GetCurrentEmployeeId(User);
        if (currentUserId == null || currentUserId.Value == 0)
            return Unauthorized();

        await _anomalyService.ResolveAnomalyAsync(id, request.Resolution, currentUserId.Value);
        return Ok(new { message = "Anomaly resolved." });
    }

    [HttpPost("anomalies/{id:int}/false-positive")]
    [Authorize(Roles = "Admin,BaoVe,QuanLy")]
    public async Task<IActionResult> MarkFalsePositive(int id)
    {
        var currentUserId = _permissionService.GetCurrentEmployeeId(User);
        if (currentUserId == null || currentUserId.Value == 0)
            return Unauthorized();

        await _anomalyService.MarkFalsePositiveAsync(id, currentUserId.Value);
        return Ok(new { message = "Anomaly marked as false positive." });
    }

    [HttpGet("anomalies/predict-absences/{employeeId:int}")]
    [Authorize(Roles = "Admin,BaoVe,QuanLy")]
    public async Task<IActionResult> PredictAbsences(int employeeId, [FromQuery] int lookAheadDays = 7)
    {
        if (!await CanViewEmployeeAsync(employeeId))
            return Forbid();

        var predictions = await _anomalyService.PredictAbsencesAsync(employeeId, lookAheadDays);
        return Ok(new { employeeId, lookAheadDays, predictions });
    }

    private async Task<bool> EnsureCanManageAsync() =>
        _permissionService.IsAdmin(User) || await _permissionService.IsManagerAsync(User);

    private async Task<bool> CanViewEmployeeAsync(int employeeId)
    {
        if (_permissionService.IsAdmin(User) || _permissionService.IsSecurity(User))
            return true;

        if (await _permissionService.IsManagerAsync(User))
            return await _permissionService.CanManageEmployeeAsync(User, employeeId);

        return _permissionService.GetCurrentEmployeeId(User) == employeeId;
    }

    private async Task<bool> CanOperateAttendanceAsync(int employeeId)
    {
        if (_permissionService.IsSecurity(User))
            return false;

        if (_permissionService.IsAdmin(User)) return true;
        if (await _permissionService.IsManagerAsync(User))
            return await _permissionService.CanManageEmployeeAsync(User, employeeId);

        return _permissionService.GetCurrentEmployeeId(User) == employeeId;
    }
}

public class AnomalyResolveRequest
{
    public string Resolution { get; set; } = string.Empty;
}
