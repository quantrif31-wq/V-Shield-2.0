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
[Route("api/leave-requests")]
[Authorize]
[RequireOperationalTask("approvals")]
public class LeaveRequestsController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly IAttendancePermissionService _permissionService;
    private readonly INotificationService _notificationService;

    public LeaveRequestsController(ApplicationDbContext context, IAttendancePermissionService permissionService, INotificationService notificationService)
    {
        _context = context;
        _permissionService = permissionService;
        _notificationService = notificationService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] int? employeeId,
        [FromQuery] int? departmentId,
        [FromQuery] string? status,
        [FromQuery] string? leaveType,
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate)
    {
        var query = _context.LeaveRequests
            .AsNoTracking()
            .Include(x => x.Employee).ThenInclude(e => e.Department)
            .Include(x => x.Approver)
            .AsQueryable();

        query = await ApplyScopeAsync(query);

        if (employeeId.HasValue) query = query.Where(x => x.EmployeeId == employeeId.Value);
        if (departmentId.HasValue) query = query.Where(x => x.Employee.DepartmentId == departmentId.Value);
        if (!string.IsNullOrWhiteSpace(status)) query = query.Where(x => x.Status == status.Trim());
        if (!string.IsNullOrWhiteSpace(leaveType)) query = query.Where(x => x.LeaveType == leaveType.Trim());
        if (fromDate.HasValue) query = query.Where(x => x.StartDate >= fromDate.Value.Date);
        if (toDate.HasValue) query = query.Where(x => x.EndDate <= toDate.Value.Date);

        var data = await query
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => new
            {
                x.LeaveRequestId,
                x.EmployeeId,
                employeeName = x.Employee.FullName,
                departmentId = x.Employee.DepartmentId,
                departmentName = x.Employee.Department != null ? x.Employee.Department.Name : null,
                x.LeaveType,
                x.StartDate,
                x.EndDate,
                x.Reason,
                x.Status,
                x.ApproverId,
                approverName = x.Approver != null ? x.Approver.FullName : null,
                x.RejectReason,
                x.CreatedAt,
                x.ApprovedAt,
                x.UpdatedAt
            })
            .ToListAsync();

        return Ok(data);
    }

    [HttpGet("my")]
    public async Task<IActionResult> GetMyRequests()
    {
        var employeeId = _permissionService.GetCurrentEmployeeId(User);
        if (!employeeId.HasValue)
            return BadRequest(new { message = "Tai khoan hien tai chua lien ket nhan vien." });

        var data = await _context.LeaveRequests
            .AsNoTracking()
            .Where(x => x.EmployeeId == employeeId.Value)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync();

        return Ok(data);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var item = await _context.LeaveRequests
            .AsNoTracking()
            .Include(x => x.Employee).ThenInclude(e => e.Department)
            .Include(x => x.Approver)
            .FirstOrDefaultAsync(x => x.LeaveRequestId == id);
        if (item == null)
            return NotFound(new { message = $"Khong tim thay don nghi ID {id}" });

        if (!await CanViewEmployeeAsync(item.EmployeeId))
            return Forbid();

        return Ok(new
        {
            item.LeaveRequestId,
            item.EmployeeId,
            employeeName = item.Employee.FullName,
            departmentId = item.Employee.DepartmentId,
            departmentName = item.Employee.Department?.Name,
            item.LeaveType,
            item.StartDate,
            item.EndDate,
            item.Reason,
            item.Status,
            item.ApproverId,
            approverName = item.Approver?.FullName,
            item.RejectReason,
            item.CreatedAt,
            item.ApprovedAt,
            item.UpdatedAt
        });
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] LeaveRequestCreateRequest request)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        if (!LeaveTypes.All.Contains(request.LeaveType))
            return BadRequest(new { message = "Loai nghi khong hop le." });
        if (request.StartDate == null || request.EndDate == null)
            return BadRequest(new { message = "StartDate va EndDate la bat buoc." });
        if (string.IsNullOrWhiteSpace(request.Reason))
            return BadRequest(new { message = "Ly do xin nghi khong duoc de trong." });

        var startDate = request.StartDate.Value.Date;
        var endDate = request.EndDate.Value.Date;
        if (endDate < startDate)
            return BadRequest(new { message = "EndDate khong duoc nho hon StartDate." });

        var targetEmployeeId = request.EmployeeId ?? _permissionService.GetCurrentEmployeeId(User);
        if (!targetEmployeeId.HasValue)
            return BadRequest(new { message = "Tai khoan hien tai chua lien ket nhan vien." });

        if (!_permissionService.IsAdmin(User) &&
            !await _permissionService.IsManagerAsync(User) &&
            _permissionService.GetCurrentEmployeeId(User) != targetEmployeeId.Value)
        {
            return Forbid();
        }

        if ((_permissionService.IsAdmin(User) || await _permissionService.IsManagerAsync(User)) &&
            !await _permissionService.CanManageEmployeeAsync(User, targetEmployeeId.Value) &&
            !_permissionService.IsAdmin(User))
        {
            return Forbid();
        }

        var employee = await _context.Employees.FirstOrDefaultAsync(e => e.EmployeeId == targetEmployeeId.Value);
        if (employee == null)
            return NotFound(new { message = "Nhan vien khong ton tai." });
        if (employee.Status != true)
            return BadRequest(new { message = "Nhan vien da ngung hoat dong." });

        var hasConflict = await _context.LeaveRequests.AnyAsync(x =>
            x.EmployeeId == targetEmployeeId.Value &&
            (x.Status == LeaveRequestStatuses.Pending || x.Status == LeaveRequestStatuses.Approved) &&
            x.StartDate <= endDate &&
            x.EndDate >= startDate);

        if (hasConflict)
            return Conflict(new { message = "Don nghi bi trung voi don Pending/Approved da ton tai." });

        var leaveRequest = new LeaveRequest
        {
            EmployeeId = targetEmployeeId.Value,
            LeaveType = request.LeaveType,
            StartDate = startDate,
            EndDate = endDate,
            Reason = request.Reason.Trim(),
            Status = LeaveRequestStatuses.Pending,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.LeaveRequests.Add(leaveRequest);
        await _context.SaveChangesAsync();
        var requesterName = await _context.Employees.Where(e => e.EmployeeId == targetEmployeeId.Value).Select(e => e.FullName).FirstOrDefaultAsync() ?? "Nhân viên";
        await _notificationService.NotifyEventAsync("Approval.LeaveRequest.Submitted",
            $"Đơn nghỉ phép mới từ {requesterName}",
            $"{requesterName} xin nghỉ {request.LeaveType} từ {startDate:dd/MM} đến {endDate:dd/MM}: {request.Reason}",
            "LeaveRequest", leaveRequest.LeaveRequestId.ToString(),
            "/attendance/leave-approvals");
        return CreatedAtAction(nameof(GetById), new { id = leaveRequest.LeaveRequestId }, leaveRequest);
    }

    [HttpPut("{id:int}/approve")]
    public async Task<IActionResult> Approve(int id)
    {
        if (!await EnsureCanApproveAsync()) return Forbid();

        var leaveRequest = await _context.LeaveRequests.FirstOrDefaultAsync(x => x.LeaveRequestId == id);
        if (leaveRequest == null)
            return NotFound(new { message = $"Khong tim thay don nghi ID {id}" });

        if (!await _permissionService.CanManageEmployeeAsync(User, leaveRequest.EmployeeId) && !_permissionService.IsAdmin(User))
            return Forbid();

        if (!string.Equals(leaveRequest.Status, LeaveRequestStatuses.Pending, StringComparison.OrdinalIgnoreCase))
            return BadRequest(new { message = "Chi duoc duyet don dang o trang thai Pending." });

        leaveRequest.Status = LeaveRequestStatuses.Approved;
        leaveRequest.ApproverId = _permissionService.GetCurrentUserId(User);
        leaveRequest.ApprovedAt = DateTime.UtcNow;
        leaveRequest.RejectReason = null;
        leaveRequest.UpdatedAt = DateTime.UtcNow;

        var schedules = await _context.WorkSchedules
            .Where(s =>
                s.EmployeeId == leaveRequest.EmployeeId &&
                s.WorkDate >= leaveRequest.StartDate.Date &&
                s.WorkDate <= leaveRequest.EndDate.Date &&
                s.Status != WorkScheduleStatuses.Cancelled)
            .ToListAsync();

        foreach (var schedule in schedules)
        {
            schedule.Status = WorkScheduleStatuses.Leave;
            schedule.UpdatedAt = DateTime.UtcNow;
        }

        var attendances = await _context.Attendances
            .Where(a =>
                a.EmployeeId == leaveRequest.EmployeeId &&
                a.WorkDate >= leaveRequest.StartDate.Date &&
                a.WorkDate <= leaveRequest.EndDate.Date)
            .ToListAsync();

        foreach (var attendance in attendances)
        {
            attendance.Status = AttendanceStatuses.Leave;
            attendance.UpdatedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();
        var approverName = await _context.Employees.Where(e => e.EmployeeId == leaveRequest.EmployeeId).Select(e => e.FullName).FirstOrDefaultAsync() ?? "";
        await _notificationService.NotifyEventAsync("Approval.LeaveRequest.Approved",
            "Đơn nghỉ phép đã được duyệt",
            $"Đơn nghỉ phép {leaveRequest.LeaveType} từ {leaveRequest.StartDate:dd/MM} đến {leaveRequest.EndDate:dd/MM} đã được duyệt.",
            "LeaveRequest", leaveRequest.LeaveRequestId.ToString(),
            "/attendance/my-leave-requests");
        return Ok(new { message = "Da duyet don nghi.", leaveRequestId = id });
    }

    [HttpPut("{id:int}/reject")]
    public async Task<IActionResult> Reject(int id, [FromBody] LeaveRequestRejectRequest request)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        if (!await EnsureCanApproveAsync()) return Forbid();

        var leaveRequest = await _context.LeaveRequests.FirstOrDefaultAsync(x => x.LeaveRequestId == id);
        if (leaveRequest == null)
            return NotFound(new { message = $"Khong tim thay don nghi ID {id}" });

        if (!await _permissionService.CanManageEmployeeAsync(User, leaveRequest.EmployeeId) && !_permissionService.IsAdmin(User))
            return Forbid();

        if (!string.Equals(leaveRequest.Status, LeaveRequestStatuses.Pending, StringComparison.OrdinalIgnoreCase))
            return BadRequest(new { message = "Chi duoc tu choi don dang o trang thai Pending." });

        leaveRequest.Status = LeaveRequestStatuses.Rejected;
        leaveRequest.ApproverId = _permissionService.GetCurrentUserId(User);
        leaveRequest.ApprovedAt = null;
        leaveRequest.RejectReason = request.RejectReason.Trim();
        leaveRequest.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        await _notificationService.NotifyEventAsync("Approval.LeaveRequest.Rejected",
            "Đơn nghỉ phép bị từ chối",
            $"Đơn nghỉ phép {leaveRequest.LeaveType} từ {leaveRequest.StartDate:dd/MM} đến {leaveRequest.EndDate:dd/MM} bị từ chối. Lý do: {request.RejectReason}",
            "LeaveRequest", leaveRequest.LeaveRequestId.ToString(),
            "/attendance/my-leave-requests");
        return Ok(new { message = "Da tu choi don nghi.", leaveRequestId = id });
    }

    [HttpPut("{id:int}/cancel")]
    public async Task<IActionResult> Cancel(int id)
    {
        var leaveRequest = await _context.LeaveRequests.FirstOrDefaultAsync(x => x.LeaveRequestId == id);
        if (leaveRequest == null)
            return NotFound(new { message = $"Khong tim thay don nghi ID {id}" });

        var currentEmployeeId = _permissionService.GetCurrentEmployeeId(User);
        if (currentEmployeeId != leaveRequest.EmployeeId && !_permissionService.IsAdmin(User))
            return Forbid();

        if (!string.Equals(leaveRequest.Status, LeaveRequestStatuses.Pending, StringComparison.OrdinalIgnoreCase))
            return BadRequest(new { message = "Chi duoc huy don o trang thai Pending." });

        leaveRequest.Status = LeaveRequestStatuses.Cancelled;
        leaveRequest.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return Ok(new { message = "Da huy don nghi.", leaveRequestId = id });
    }

    private async Task<IQueryable<LeaveRequest>> ApplyScopeAsync(IQueryable<LeaveRequest> query)
    {
        if (_permissionService.IsAdmin(User))
            return query;

        if (User.IsInRole("NhanSu"))
            return query;

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

    private async Task<bool> CanViewEmployeeAsync(int employeeId)
    {
        if (_permissionService.IsAdmin(User)) return true;
        if (await _permissionService.IsManagerAsync(User))
            return await _permissionService.CanManageEmployeeAsync(User, employeeId);
        return _permissionService.GetCurrentEmployeeId(User) == employeeId;
    }

    private async Task<bool> EnsureCanApproveAsync() =>
        _permissionService.IsAdmin(User) || await _permissionService.IsManagerAsync(User) || User.IsInRole("NhanSu");
}

