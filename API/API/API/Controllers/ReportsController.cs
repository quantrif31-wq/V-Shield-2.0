using API.Data;
using API.Middleware;
using API.Models;
using API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace API.Controllers;

[ApiController]
[Route("api/reports")]
[Authorize]
[RequireOperationalTask("reports")]
public class ReportsController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly IAttendancePermissionService _permissionService;

    public ReportsController(ApplicationDbContext context, IAttendancePermissionService permissionService)
    {
        _context = context;
        _permissionService = permissionService;
    }

    [HttpGet("attendance/daily")]
    public async Task<IActionResult> GetDailyAttendance([FromQuery] DateTime? date)
    {
        if (_permissionService.IsSecurity(User))
            return Forbid();

        var targetDate = (date ?? DateTime.Today).Date;

        var scheduleQuery = _context.WorkSchedules.AsNoTracking().Where(s =>
            s.WorkDate == targetDate &&
            s.Status != WorkScheduleStatuses.Cancelled);
        scheduleQuery = await ApplyScheduleScopeAsync(scheduleQuery);

        var schedules = await scheduleQuery.ToListAsync();
        var scheduledEmployeeIds = schedules.Select(s => s.EmployeeId).Distinct().ToHashSet();

        var attendanceQuery = _context.Attendances
            .AsNoTracking()
            .Where(a => a.WorkDate == targetDate && scheduledEmployeeIds.Contains(a.EmployeeId));
        attendanceQuery = await ApplyAttendanceScopeAsync(attendanceQuery);

        var attendances = await attendanceQuery.ToListAsync();

        var checkedInEmployeeIds = attendances
            .Where(a => a.CheckIn.HasValue && a.Status != AttendanceStatuses.Leave)
            .Select(a => a.EmployeeId)
            .Distinct()
            .ToHashSet();

        var lateEmployeeIds = attendances
            .Where(a => a.LateMinutes > 0)
            .Select(a => a.EmployeeId)
            .Distinct()
            .ToHashSet();

        var pendingLeaveQuery = _context.LeaveRequests
            .AsNoTracking()
            .Where(l => l.Status == LeaveRequestStatuses.Pending);
        pendingLeaveQuery = await ApplyLeaveScopeAsync(pendingLeaveQuery);
        var pendingLeaveCount = await pendingLeaveQuery.CountAsync();

        return Ok(new
        {
            date = targetDate,
            scheduledEmployees = scheduledEmployeeIds.Count,
            checkedInEmployees = checkedInEmployeeIds.Count,
            notCheckedInEmployees = Math.Max(0, scheduledEmployeeIds.Count - checkedInEmployeeIds.Count),
            lateEmployees = lateEmployeeIds.Count,
            totalOvertimeHours = Math.Round(attendances.Sum(a => a.OvertimeHours), 2),
            pendingLeaveRequests = pendingLeaveCount
        });
    }

    [HttpGet("attendance/monthly")]
    public async Task<IActionResult> GetMonthlyAttendance([FromQuery] int? month, [FromQuery] int? year)
    {
        if (_permissionService.IsSecurity(User))
            return Forbid();

        var now = DateTime.Today;
        var targetMonth = month ?? now.Month;
        var targetYear = year ?? now.Year;
        var fromDate = new DateTime(targetYear, targetMonth, 1);
        var toDate = fromDate.AddMonths(1).AddDays(-1);

        var employeeQuery = _context.Employees
            .AsNoTracking()
            .Include(e => e.Department)
            .Where(e => e.Status == true);
        employeeQuery = await ApplyEmployeeScopeAsync(employeeQuery);
        var employees = await employeeQuery.ToListAsync();
        var employeeIds = employees.Select(e => e.EmployeeId).ToList();

        var schedules = await _context.WorkSchedules
            .AsNoTracking()
            .Where(s => employeeIds.Contains(s.EmployeeId) && s.WorkDate >= fromDate && s.WorkDate <= toDate)
            .ToListAsync();

        var attendances = await _context.Attendances
            .AsNoTracking()
            .Where(a => employeeIds.Contains(a.EmployeeId) && a.WorkDate >= fromDate && a.WorkDate <= toDate)
            .ToListAsync();

        var rows = employees.Select(employee =>
        {
            var employeeSchedules = schedules.Where(s => s.EmployeeId == employee.EmployeeId).ToList();
            var employeeAttendances = attendances.Where(a => a.EmployeeId == employee.EmployeeId).ToList();

            var workDays = employeeAttendances.Count(a =>
                a.CheckOut.HasValue &&
                a.Status != AttendanceStatuses.Absent &&
                a.Status != AttendanceStatuses.Leave);
            var leaveDays = employeeSchedules.Count(s => s.Status == WorkScheduleStatuses.Leave) +
                            employeeAttendances.Count(a => a.Status == AttendanceStatuses.Leave);
            var absentDays = employeeSchedules.Count(s => s.Status == WorkScheduleStatuses.Absent) +
                             employeeAttendances.Count(a => a.Status == AttendanceStatuses.Absent);

            return new
            {
                employee.EmployeeId,
                employeeName = employee.FullName,
                departmentId = employee.DepartmentId,
                departmentName = employee.Department?.Name,
                workDays,
                leaveDays,
                absentDays,
                lateCount = employeeAttendances.Count(a => a.LateMinutes > 0),
                earlyLeaveCount = employeeAttendances.Count(a => a.EarlyLeaveMinutes > 0),
                totalWorkingHours = Math.Round(employeeAttendances.Sum(a => a.TotalWorkingHours), 2),
                overtimeHours = Math.Round(employeeAttendances.Sum(a => a.OvertimeHours), 2)
            };
        }).ToList();

        return Ok(new
        {
            month = targetMonth,
            year = targetYear,
            fromDate,
            toDate,
            items = rows
        });
    }

    [HttpGet("attendance/department")]
    public async Task<IActionResult> GetDepartmentAttendance([FromQuery] DateTime? fromDate, [FromQuery] DateTime? toDate)
    {
        if (_permissionService.IsSecurity(User))
            return Forbid();

        var start = (fromDate ?? DateTime.Today.AddDays(-30)).Date;
        var end = (toDate ?? DateTime.Today).Date;
        if (end < start)
            return BadRequest(new { message = "toDate khong duoc nho hon fromDate." });

        var query = _context.Attendances
            .AsNoTracking()
            .Include(a => a.Employee).ThenInclude(e => e.Department)
            .Where(a => a.WorkDate >= start && a.WorkDate <= end);
        query = await ApplyAttendanceScopeAsync(query);

        var data = await query
            .GroupBy(a => new { a.Employee.DepartmentId, DepartmentName = a.Employee.Department != null ? a.Employee.Department.Name : "Chua gan phong ban" })
            .Select(g => new
            {
                departmentId = g.Key.DepartmentId,
                departmentName = g.Key.DepartmentName,
                attendanceRecords = g.Count(),
                lateCount = g.Count(x => x.LateMinutes > 0),
                earlyLeaveCount = g.Count(x => x.EarlyLeaveMinutes > 0),
                absentCount = g.Count(x => x.Status == AttendanceStatuses.Absent),
                leaveCount = g.Count(x => x.Status == AttendanceStatuses.Leave),
                totalWorkingHours = Math.Round(g.Sum(x => x.TotalWorkingHours), 2),
                overtimeHours = Math.Round(g.Sum(x => x.OvertimeHours), 2)
            })
            .OrderByDescending(x => x.attendanceRecords)
            .ToListAsync();

        return Ok(new { fromDate = start, toDate = end, items = data });
    }

    [HttpGet("attendance/late")]
    public async Task<IActionResult> GetLateReport([FromQuery] DateTime? fromDate, [FromQuery] DateTime? toDate)
    {
        if (_permissionService.IsSecurity(User))
            return Forbid();

        var start = (fromDate ?? DateTime.Today.AddDays(-30)).Date;
        var end = (toDate ?? DateTime.Today).Date;
        if (end < start)
            return BadRequest(new { message = "toDate khong duoc nho hon fromDate." });

        var query = _context.Attendances
            .AsNoTracking()
            .Include(a => a.Employee).ThenInclude(e => e.Department)
            .Include(a => a.Schedule).ThenInclude(s => s!.Shift)
            .Where(a => a.WorkDate >= start && a.WorkDate <= end && a.LateMinutes > 0);
        query = await ApplyAttendanceScopeAsync(query);

        var data = await query
            .OrderByDescending(x => x.WorkDate)
            .ThenByDescending(x => x.LateMinutes)
            .Select(x => new
            {
                x.AttendanceId,
                x.EmployeeId,
                employeeName = x.Employee.FullName,
                departmentName = x.Employee.Department != null ? x.Employee.Department.Name : null,
                x.WorkDate,
                shiftName = x.Schedule != null ? x.Schedule.Shift.ShiftName : null,
                x.CheckIn,
                x.LateMinutes,
                x.Status
            })
            .ToListAsync();

        return Ok(new { fromDate = start, toDate = end, items = data });
    }

    [HttpGet("attendance/overtime")]
    public async Task<IActionResult> GetOvertimeReport([FromQuery] DateTime? fromDate, [FromQuery] DateTime? toDate)
    {
        if (_permissionService.IsSecurity(User))
            return Forbid();

        var start = (fromDate ?? DateTime.Today.AddDays(-30)).Date;
        var end = (toDate ?? DateTime.Today).Date;
        if (end < start)
            return BadRequest(new { message = "toDate khong duoc nho hon fromDate." });

        var query = _context.Attendances
            .AsNoTracking()
            .Include(a => a.Employee).ThenInclude(e => e.Department)
            .Include(a => a.Schedule).ThenInclude(s => s!.Shift)
            .Where(a => a.WorkDate >= start && a.WorkDate <= end && a.OvertimeHours > 0);
        query = await ApplyAttendanceScopeAsync(query);

        var data = await query
            .OrderByDescending(x => x.WorkDate)
            .ThenByDescending(x => x.OvertimeHours)
            .Select(x => new
            {
                x.AttendanceId,
                x.EmployeeId,
                employeeName = x.Employee.FullName,
                departmentName = x.Employee.Department != null ? x.Employee.Department.Name : null,
                x.WorkDate,
                shiftName = x.Schedule != null ? x.Schedule.Shift.ShiftName : null,
                x.CheckOut,
                x.OvertimeHours,
                x.Status
            })
            .ToListAsync();

        return Ok(new { fromDate = start, toDate = end, items = data });
    }

    [HttpGet("leave/monthly")]
    public async Task<IActionResult> GetMonthlyLeaveReport([FromQuery] int? month, [FromQuery] int? year)
    {
        if (_permissionService.IsSecurity(User))
            return Forbid();

        var now = DateTime.Today;
        var targetMonth = month ?? now.Month;
        var targetYear = year ?? now.Year;
        var fromDate = new DateTime(targetYear, targetMonth, 1);
        var toDate = fromDate.AddMonths(1).AddDays(-1);

        var query = _context.LeaveRequests
            .AsNoTracking()
            .Include(x => x.Employee).ThenInclude(e => e.Department)
            .Where(x => x.StartDate <= toDate && x.EndDate >= fromDate);
        query = await ApplyLeaveScopeAsync(query);

        var items = await query
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => new
            {
                x.LeaveRequestId,
                x.EmployeeId,
                employeeName = x.Employee.FullName,
                departmentName = x.Employee.Department != null ? x.Employee.Department.Name : null,
                x.LeaveType,
                x.StartDate,
                x.EndDate,
                days = EF.Functions.DateDiffDay(x.StartDate, x.EndDate) + 1,
                x.Status
            })
            .ToListAsync();

        return Ok(new
        {
            month = targetMonth,
            year = targetYear,
            fromDate,
            toDate,
            summary = new
            {
                totalRequests = items.Count,
                pending = items.Count(x => x.Status == LeaveRequestStatuses.Pending),
                approved = items.Count(x => x.Status == LeaveRequestStatuses.Approved),
                rejected = items.Count(x => x.Status == LeaveRequestStatuses.Rejected),
                cancelled = items.Count(x => x.Status == LeaveRequestStatuses.Cancelled)
            },
            items
        });
    }

    private async Task<IQueryable<Attendance>> ApplyAttendanceScopeAsync(IQueryable<Attendance> query)
    {
        if (_permissionService.IsAdmin(User)) return query;

        if (await _permissionService.IsManagerAsync(User))
        {
            var deptId = await _permissionService.GetUserDepartmentIdAsync(User);
            if (!deptId.HasValue) return query.Where(_ => false);
            return query.Where(x => x.Employee.DepartmentId == deptId.Value);
        }

        var employeeId = _permissionService.GetCurrentEmployeeId(User);
        if (!employeeId.HasValue) return query.Where(_ => false);
        return query.Where(x => x.EmployeeId == employeeId.Value);
    }

    private async Task<IQueryable<WorkSchedule>> ApplyScheduleScopeAsync(IQueryable<WorkSchedule> query)
    {
        if (_permissionService.IsAdmin(User)) return query;

        if (await _permissionService.IsManagerAsync(User))
        {
            var deptId = await _permissionService.GetUserDepartmentIdAsync(User);
            if (!deptId.HasValue) return query.Where(_ => false);
            return query.Where(x => x.Employee.DepartmentId == deptId.Value);
        }

        var employeeId = _permissionService.GetCurrentEmployeeId(User);
        if (!employeeId.HasValue) return query.Where(_ => false);
        return query.Where(x => x.EmployeeId == employeeId.Value);
    }

    private async Task<IQueryable<LeaveRequest>> ApplyLeaveScopeAsync(IQueryable<LeaveRequest> query)
    {
        if (_permissionService.IsAdmin(User)) return query;

        if (await _permissionService.IsManagerAsync(User))
        {
            var deptId = await _permissionService.GetUserDepartmentIdAsync(User);
            if (!deptId.HasValue) return query.Where(_ => false);
            return query.Where(x => x.Employee.DepartmentId == deptId.Value);
        }

        var employeeId = _permissionService.GetCurrentEmployeeId(User);
        if (!employeeId.HasValue) return query.Where(_ => false);
        return query.Where(x => x.EmployeeId == employeeId.Value);
    }

    private async Task<IQueryable<Employee>> ApplyEmployeeScopeAsync(IQueryable<Employee> query)
    {
        if (_permissionService.IsAdmin(User)) return query;

        if (await _permissionService.IsManagerAsync(User))
        {
            var deptId = await _permissionService.GetUserDepartmentIdAsync(User);
            if (!deptId.HasValue) return query.Where(_ => false);
            return query.Where(x => x.DepartmentId == deptId.Value);
        }

        var employeeId = _permissionService.GetCurrentEmployeeId(User);
        if (!employeeId.HasValue) return query.Where(_ => false);
        return query.Where(x => x.EmployeeId == employeeId.Value);
    }
}

