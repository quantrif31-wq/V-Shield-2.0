using API.Data;
using API.DTOs;
using API.Middleware;
using API.Models;
using API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace API.Controllers;

[ApiController]
[Route("api/work-schedules")]
[Authorize]
public class WorkSchedulesController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly IAttendancePermissionService _permissionService;

    public WorkSchedulesController(ApplicationDbContext context, IAttendancePermissionService permissionService)
    {
        _context = context;
        _permissionService = permissionService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] int? employeeId,
        [FromQuery] int? departmentId,
        [FromQuery] int? shiftId,
        [FromQuery] string? status,
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate)
    {
        var query = _context.WorkSchedules
            .AsNoTracking()
            .Include(x => x.Employee).ThenInclude(e => e.Department)
            .Include(x => x.Shift)
            .AsQueryable();

        query = await ApplyScopeAsync(query);

        if (employeeId.HasValue) query = query.Where(x => x.EmployeeId == employeeId.Value);
        if (departmentId.HasValue) query = query.Where(x => x.Employee.DepartmentId == departmentId.Value);
        if (shiftId.HasValue) query = query.Where(x => x.ShiftId == shiftId.Value);
        if (!string.IsNullOrWhiteSpace(status)) query = query.Where(x => x.Status == status.Trim());
        if (fromDate.HasValue) query = query.Where(x => x.WorkDate >= fromDate.Value.Date);
        if (toDate.HasValue) query = query.Where(x => x.WorkDate <= toDate.Value.Date);

        var data = await query
            .OrderBy(x => x.WorkDate)
            .ThenBy(x => x.Shift.StartTime)
            .Select(x => new
            {
                x.ScheduleId,
                x.EmployeeId,
                employeeName = x.Employee.FullName,
                departmentId = x.Employee.DepartmentId,
                departmentName = x.Employee.Department != null ? x.Employee.Department.Name : null,
                x.ShiftId,
                shiftName = x.Shift.ShiftName,
                shiftStartTime = x.Shift.StartTime,
                shiftEndTime = x.Shift.EndTime,
                x.WorkDate,
                x.Status,
                x.Note,
                x.CreatedBy,
                x.CreatedAt,
                x.UpdatedAt
            })
            .ToListAsync();

        return Ok(data);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var schedule = await _context.WorkSchedules
            .AsNoTracking()
            .Include(x => x.Employee).ThenInclude(e => e.Department)
            .Include(x => x.Shift)
            .FirstOrDefaultAsync(x => x.ScheduleId == id);

        if (schedule == null)
            return NotFound(new { message = $"Khong tim thay lich lam ID {id}" });

        if (!await CanViewEmployeeAsync(schedule.EmployeeId))
            return Forbid();

        return Ok(new
        {
            schedule.ScheduleId,
            schedule.EmployeeId,
            employeeName = schedule.Employee.FullName,
            departmentId = schedule.Employee.DepartmentId,
            departmentName = schedule.Employee.Department?.Name,
            schedule.ShiftId,
            shiftName = schedule.Shift.ShiftName,
            shiftStartTime = schedule.Shift.StartTime,
            shiftEndTime = schedule.Shift.EndTime,
            schedule.WorkDate,
            schedule.Status,
            schedule.Note,
            schedule.CreatedBy,
            schedule.CreatedAt,
            schedule.UpdatedAt
        });
    }

    [HttpGet("employee/{employeeId:int}")]
    public async Task<IActionResult> GetByEmployee(int employeeId)
    {
        if (!await CanViewEmployeeAsync(employeeId))
            return Forbid();

        var data = await _context.WorkSchedules
            .AsNoTracking()
            .Include(x => x.Shift)
            .Where(x => x.EmployeeId == employeeId)
            .OrderByDescending(x => x.WorkDate)
            .Select(x => new
            {
                x.ScheduleId,
                x.EmployeeId,
                x.ShiftId,
                shiftName = x.Shift.ShiftName,
                shiftStartTime = x.Shift.StartTime,
                shiftEndTime = x.Shift.EndTime,
                x.WorkDate,
                x.Status,
                x.Note,
                x.CreatedBy,
                x.CreatedAt,
                x.UpdatedAt
            })
            .ToListAsync();

        return Ok(data);
    }

    [HttpPost]
    [RequireOperationalTask("metadata")]
    public async Task<IActionResult> Create([FromBody] WorkScheduleUpsertRequest request)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        if (!await EnsureCanManageAsync()) return Forbid();
        if (request.WorkDate == null)
            return BadRequest(new { message = "WorkDate la bat buoc." });

        var employee = await _context.Employees.FirstOrDefaultAsync(e => e.EmployeeId == request.EmployeeId);
        if (employee == null) return NotFound(new { message = "Nhan vien khong ton tai." });
        if (employee.Status != true) return BadRequest(new { message = "Nhan vien da ngung hoat dong." });
        if (!await _permissionService.CanManageEmployeeAsync(User, request.EmployeeId)) return Forbid();

        var shift = await _context.Shifts.FirstOrDefaultAsync(s => s.ShiftId == request.ShiftId);
        if (shift == null) return NotFound(new { message = "Ca lam khong ton tai." });
        if (!shift.IsActive) return BadRequest(new { message = "Ca lam da bi khoa." });

        var workDate = request.WorkDate.Value.Date;
        var exists = await _context.WorkSchedules.AnyAsync(x =>
            x.EmployeeId == request.EmployeeId &&
            x.ShiftId == request.ShiftId &&
            x.WorkDate == workDate);
        if (exists) return Conflict(new { message = "Nhan vien da co lich lam trung ngay va trung ca." });

        var schedule = new WorkSchedule
        {
            EmployeeId = request.EmployeeId,
            ShiftId = request.ShiftId,
            WorkDate = workDate,
            Status = WorkScheduleStatuses.Scheduled,
            Note = request.Note?.Trim(),
            CreatedBy = _permissionService.GetCurrentUserId(User),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.WorkSchedules.Add(schedule);
        await _context.SaveChangesAsync();
        return CreatedAtAction(nameof(GetById), new { id = schedule.ScheduleId }, schedule);
    }

    [HttpPut("{id:int}")]
    [RequireOperationalTask("metadata")]
    public async Task<IActionResult> Update(int id, [FromBody] WorkScheduleUpsertRequest request)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        if (!await EnsureCanManageAsync()) return Forbid();
        if (request.WorkDate == null)
            return BadRequest(new { message = "WorkDate la bat buoc." });

        var schedule = await _context.WorkSchedules
            .Include(x => x.Employee)
            .FirstOrDefaultAsync(x => x.ScheduleId == id);
        if (schedule == null)
            return NotFound(new { message = $"Khong tim thay lich lam ID {id}" });

        if (!await _permissionService.CanManageEmployeeAsync(User, schedule.EmployeeId)) return Forbid();
        if (!await _permissionService.CanManageEmployeeAsync(User, request.EmployeeId)) return Forbid();

        var shift = await _context.Shifts.FirstOrDefaultAsync(s => s.ShiftId == request.ShiftId);
        if (shift == null) return NotFound(new { message = "Ca lam khong ton tai." });
        if (!shift.IsActive) return BadRequest(new { message = "Ca lam da bi khoa." });

        if (schedule.Employee.Status != true)
            return BadRequest(new { message = "Nhan vien da ngung hoat dong." });

        var hasAttendance = await _context.Attendances.AnyAsync(a => a.ScheduleId == id);
        var targetDate = request.WorkDate.Value.Date;

        if (hasAttendance &&
            (schedule.EmployeeId != request.EmployeeId || schedule.ShiftId != request.ShiftId || schedule.WorkDate != targetDate))
        {
            return BadRequest(new { message = "Lich lam da co du lieu cham cong, khong the thay doi nhan vien/ca/ngay." });
        }

        var duplicated = await _context.WorkSchedules.AnyAsync(x =>
            x.ScheduleId != id &&
            x.EmployeeId == request.EmployeeId &&
            x.ShiftId == request.ShiftId &&
            x.WorkDate == targetDate);
        if (duplicated) return Conflict(new { message = "Nhan vien da co lich lam trung ngay va trung ca." });

        schedule.EmployeeId = request.EmployeeId;
        schedule.ShiftId = request.ShiftId;
        schedule.WorkDate = targetDate;
        if (!string.Equals(schedule.Status, WorkScheduleStatuses.Cancelled, StringComparison.OrdinalIgnoreCase))
            schedule.Status = WorkScheduleStatuses.Changed;
        schedule.Note = request.Note?.Trim();
        schedule.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return Ok(schedule);
    }

    [HttpPatch("{id:int}/cancel")]
    [RequireOperationalTask("metadata")]
    public async Task<IActionResult> Cancel(int id)
    {
        if (!await EnsureCanManageAsync()) return Forbid();

        var schedule = await _context.WorkSchedules.FirstOrDefaultAsync(x => x.ScheduleId == id);
        if (schedule == null)
            return NotFound(new { message = $"Khong tim thay lich lam ID {id}" });

        if (!await _permissionService.CanManageEmployeeAsync(User, schedule.EmployeeId)) return Forbid();

        schedule.Status = WorkScheduleStatuses.Cancelled;
        schedule.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return Ok(new { message = "Da huy lich lam.", scheduleId = id });
    }

    [HttpDelete("{id:int}")]
    [RequireOperationalTask("metadata")]
    public async Task<IActionResult> Delete(int id)
    {
        return await Cancel(id);
    }

    private async Task<IQueryable<WorkSchedule>> ApplyScopeAsync(IQueryable<WorkSchedule> query)
    {
        if (_permissionService.IsAdmin(User) || _permissionService.IsSecurity(User))
            return query;

        if (await _permissionService.IsManagerAsync(User))
        {
            var departmentId = await _permissionService.GetUserDepartmentIdAsync(User);
            if (!departmentId.HasValue) return query.Where(_ => false);
            return query.Where(x => x.Employee.DepartmentId == departmentId.Value);
        }

        var employeeId = _permissionService.GetCurrentEmployeeId(User);
        if (!employeeId.HasValue) return query.Where(_ => false);

        return query.Where(x => x.EmployeeId == employeeId.Value);
    }

    private async Task<bool> CanViewEmployeeAsync(int employeeId)
    {
        if (_permissionService.IsAdmin(User) || _permissionService.IsSecurity(User))
            return true;

        if (await _permissionService.IsManagerAsync(User))
            return await _permissionService.CanManageEmployeeAsync(User, employeeId);

        return _permissionService.GetCurrentEmployeeId(User) == employeeId;
    }

    private async Task<bool> EnsureCanManageAsync() =>
        _permissionService.IsAdmin(User) || await _permissionService.IsManagerAsync(User);
}

