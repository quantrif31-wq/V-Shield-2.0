using API.Data;
using API.Middleware;
using API.Models;
using API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace API.Controllers;

[ApiController]
[Route("api/vehicle-delegations")]
[Authorize]
public class VehicleDelegationsController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly IAttendancePermissionService _permissionService;
    private readonly INotificationService _notificationService;

    public VehicleDelegationsController(ApplicationDbContext context, IAttendancePermissionService permissionService, INotificationService notificationService)
    {
        _context = context;
        _permissionService = permissionService;
        _notificationService = notificationService;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateDelegationRequest request)
    {
        var fromEmployeeId = _permissionService.GetCurrentEmployeeId(User);
        if (!fromEmployeeId.HasValue)
            return BadRequest(new { message = "Tai khoan hien tai chua lien ket nhan vien." });

        var vehicle = await _context.Vehicles.FirstOrDefaultAsync(v => v.VehicleId == request.VehicleId);
        if (vehicle == null)
            return NotFound(new { message = "Xe khong ton tai." });
        if (vehicle.EmployeeId != fromEmployeeId.Value)
            return BadRequest(new { message = "Ban khong phai chu so huu xe nay." });
        if (vehicle.ParkingStatus != "IN")
            return BadRequest(new { message = "Xe khong o trong bai, khong the tao yeu cau chuyen nhuong." });
        if (request.ToEmployeeId == fromEmployeeId.Value)
            return BadRequest(new { message = "Nguoi nhan chuyen nhuong phai la nguoi khac chu xe hien tai." });

        var recipientExists = await _context.Employees.AnyAsync(employee => employee.EmployeeId == request.ToEmployeeId);
        if (!recipientExists)
            return NotFound(new { message = "Khong tim thay nguoi nhan chuyen nhuong." });

        var activeDelegation = await _context.VehicleDelegations.AnyAsync(d =>
            d.VehicleId == request.VehicleId &&
            d.Status == DelegationStatuses.Pending);
        if (activeDelegation)
            return Conflict(new { message = "Xe nay dang co yeu cau chuyen nhuong cho duyet." });

        var delegation = new VehicleDelegation
        {
            VehicleId = request.VehicleId,
            FromEmployeeId = fromEmployeeId.Value,
            ToEmployeeId = request.ToEmployeeId,
            RequestedByEmployeeId = fromEmployeeId.Value,
            Reason = request.Reason?.Trim(),
            Status = DelegationStatuses.Pending,
            RequestedAtUtc = DateTime.UtcNow
        };

        _context.VehicleDelegations.Add(delegation);
        await _context.SaveChangesAsync();

        var fromEmployee = await _context.Employees.FindAsync(fromEmployeeId.Value);
        var fromName = fromEmployee?.FullName ?? "";
        await _notificationService.NotifyEventAsync("Approval.VehicleDelegation.Created",
            "Yêu cầu chuyển nhượng xe mới",
            $"{fromName} muốn chuyển quyền sở hữu xe {vehicle.LicensePlate} cho bạn.",
            "VehicleDelegation", delegation.VehicleDelegationId.ToString(),
            "/vehicle-transfer");

        return Ok(delegation);
    }

    [HttpPost("ownership-requests")]
    public async Task<IActionResult> RequestOwnership([FromBody] RequestOwnershipRequest request)
    {
        var requesterId = _permissionService.GetCurrentEmployeeId(User);
        if (!requesterId.HasValue)
            return BadRequest(new { message = "Tai khoan hien tai chua lien ket nhan vien." });

        var vehicle = await _context.Vehicles.Include(item => item.Employee)
            .FirstOrDefaultAsync(item => item.VehicleId == request.VehicleId);
        if (vehicle == null || !vehicle.EmployeeId.HasValue)
            return NotFound(new { message = "Khong tim thay xe co chu so huu de xin chuyen nhuong." });
        if (vehicle.ParkingStatus != "IN")
            return BadRequest(new { message = "Chi co the xin chuyen nhuong xe dang o trong bai." });
        if (vehicle.EmployeeId == requesterId.Value)
            return BadRequest(new { message = "Ban da la chu so huu hien tai cua xe nay." });

        var pending = await _context.VehicleDelegations.AnyAsync(item =>
            item.VehicleId == vehicle.VehicleId && item.Status == DelegationStatuses.Pending);
        if (pending)
            return Conflict(new { message = "Xe nay dang co yeu cau chuyen nhuong cho duyet." });

        var ownershipRequest = new VehicleDelegation
        {
            VehicleId = vehicle.VehicleId,
            FromEmployeeId = vehicle.EmployeeId.Value,
            ToEmployeeId = requesterId.Value,
            RequestedByEmployeeId = requesterId.Value,
            Reason = request.Reason?.Trim(),
            Status = DelegationStatuses.Pending,
            RequestedAtUtc = DateTime.UtcNow
        };
        _context.VehicleDelegations.Add(ownershipRequest);
        await _context.SaveChangesAsync();

        var requester = await _context.Employees.FindAsync(requesterId.Value);
        await _notificationService.NotifyEventAsync("Approval.VehicleDelegation.Created",
            "Yêu cầu xin chuyển nhượng xe",
            $"{requester?.FullName ?? "Một nhân viên"} xin nhận quyền sở hữu xe {vehicle.LicensePlate}.",
            "VehicleDelegation", ownershipRequest.VehicleDelegationId.ToString(), "/vehicle-transfer");

        return Ok(ownershipRequest);
    }

    [HttpGet("available-for-ownership-request")]
    public async Task<IActionResult> GetAvailableForOwnershipRequest()
    {
        var employeeId = _permissionService.GetCurrentEmployeeId(User);
        if (!employeeId.HasValue)
            return BadRequest(new { message = "Tai khoan hien tai chua lien ket nhan vien." });

        var data = await _context.Vehicles.AsNoTracking()
            .Where(vehicle => vehicle.ParkingStatus == "IN" && vehicle.EmployeeId.HasValue && vehicle.EmployeeId != employeeId.Value)
            .Include(vehicle => vehicle.Employee)
            .OrderBy(vehicle => vehicle.LicensePlate)
            .Select(vehicle => new { vehicle.VehicleId, vehicle.LicensePlate, vehicle.Description, ownerName = vehicle.Employee!.FullName })
            .ToListAsync();
        return Ok(data);
    }

    [HttpGet("outgoing")]
    public async Task<IActionResult> GetOutgoing()
    {
        var employeeId = _permissionService.GetCurrentEmployeeId(User);
        if (!employeeId.HasValue)
            return BadRequest(new { message = "Tai khoan hien tai chua lien ket nhan vien." });

        var data = await _context.VehicleDelegations
            .AsNoTracking()
            .Where(d => d.RequestedByEmployeeId == employeeId.Value ||
                (d.RequestedByEmployeeId == null && d.FromEmployeeId == employeeId.Value))
            .Include(d => d.Vehicle)
            .Include(d => d.ToEmployee)
            .OrderByDescending(d => d.RequestedAtUtc)
            .Select(d => new
            {
                d.VehicleDelegationId,
                d.VehicleId,
                licensePlate = d.Vehicle.LicensePlate,
                d.ToEmployeeId,
                toEmployeeName = d.ToEmployee.FullName,
                d.RequestedByEmployeeId,
                requestKind = d.RequestedByEmployeeId == d.ToEmployeeId ? "OwnershipRequest" : "TransferOffer",
                d.Reason,
                d.Status,
                d.RequestedAtUtc,
                d.RespondedAtUtc
            })
            .ToListAsync();

        return Ok(data);
    }

    [HttpGet("incoming")]
    public async Task<IActionResult> GetIncoming()
    {
        var employeeId = _permissionService.GetCurrentEmployeeId(User);
        if (!employeeId.HasValue)
            return BadRequest(new { message = "Tai khoan hien tai chua lien ket nhan vien." });

        var data = await _context.VehicleDelegations
            .AsNoTracking()
            .Where(d => (d.RequestedByEmployeeId == d.ToEmployeeId && d.FromEmployeeId == employeeId.Value) ||
                ((d.RequestedByEmployeeId == null || d.RequestedByEmployeeId == d.FromEmployeeId) && d.ToEmployeeId == employeeId.Value))
            .Include(d => d.Vehicle)
            .Include(d => d.FromEmployee)
            .OrderByDescending(d => d.RequestedAtUtc)
            .Select(d => new
            {
                d.VehicleDelegationId,
                d.VehicleId,
                licensePlate = d.Vehicle.LicensePlate,
                d.FromEmployeeId,
                fromEmployeeName = d.FromEmployee.FullName,
                d.RequestedByEmployeeId,
                requestKind = d.RequestedByEmployeeId == d.ToEmployeeId ? "OwnershipRequest" : "TransferOffer",
                d.Reason,
                d.Status,
                d.RequestedAtUtc,
                d.RespondedAtUtc
            })
            .ToListAsync();

        return Ok(data);
    }

    [HttpGet]
    [Authorize(Roles = "Admin,NhanSu")]
    public async Task<IActionResult> GetAll()
    {
        var data = await _context.VehicleDelegations
            .AsNoTracking()
            .Include(d => d.Vehicle)
            .Include(d => d.FromEmployee)
            .Include(d => d.ToEmployee)
            .OrderByDescending(d => d.RequestedAtUtc)
            .Select(d => new
            {
                d.VehicleDelegationId,
                d.VehicleId,
                licensePlate = d.Vehicle.LicensePlate,
                d.FromEmployeeId,
                fromEmployeeName = d.FromEmployee.FullName,
                d.ToEmployeeId,
                toEmployeeName = d.ToEmployee.FullName,
                d.Reason,
                d.Status,
                d.RequestedAtUtc,
                d.RespondedAtUtc
            })
            .ToListAsync();

        return Ok(data);
    }

    [HttpPatch("{id}/approve")]
    public async Task<IActionResult> Approve(int id)
    {
        var delegation = await _context.VehicleDelegations
            .Include(d => d.Vehicle)
            .FirstOrDefaultAsync(d => d.VehicleDelegationId == id);

        if (delegation == null)
            return NotFound(new { message = "Khong tim thay yeu cau chuyen nhuong." });

        var currentEmployeeId = _permissionService.GetCurrentEmployeeId(User);
        var approvalEmployeeId = delegation.RequestedByEmployeeId == delegation.ToEmployeeId
            ? delegation.FromEmployeeId
            : delegation.ToEmployeeId;
        if (approvalEmployeeId != currentEmployeeId)
            return Forbid();

        if (delegation.Status != DelegationStatuses.Pending)
            return BadRequest(new { message = "Yeu cau khong o trang thai cho duyet." });
        if (delegation.Vehicle.ParkingStatus != "IN")
            return Conflict(new { message = "Xe da roi bai, khong the hoan tat chuyen nhuong cua phien gui nay." });
        if (delegation.Vehicle.EmployeeId != delegation.FromEmployeeId)
            return Conflict(new { message = "Chu so huu xe da thay doi; yeu cau chuyen nhuong nay khong con hieu luc." });

        delegation.Status = DelegationStatuses.Approved;
        delegation.RespondedAtUtc = DateTime.UtcNow;
        // Chuyển nhượng chỉ hoàn tất khi người nhận đồng ý.  Từ thời điểm này
        // Vehicle.EmployeeId là chủ mới, bao gồm cả quyền xác nhận lượt OUT
        // của phiên gửi đang mở. Bản ghi VehicleDelegation chỉ còn vai trò lịch sử.
        delegation.Vehicle.EmployeeId = delegation.ToEmployeeId;

        await _context.SaveChangesAsync();

        var delegationWithNav = await _context.VehicleDelegations
            .Include(d => d.FromEmployee).Include(d => d.Vehicle)
            .FirstOrDefaultAsync(d => d.VehicleDelegationId == id);
        if (delegationWithNav != null)
        {
            await _notificationService.NotifyEventAsync("Approval.VehicleDelegation.Approved",
                "Chuyển nhượng xe đã hoàn tất",
                $"{delegationWithNav.ToEmployee?.FullName ?? "Người nhận"} đã xác nhận chuyển quyền sở hữu xe {delegationWithNav.Vehicle?.LicensePlate}.",
                "VehicleDelegation", id.ToString(),
                "/vehicle-transfer");
        }

        return Ok(new { message = "Da chap thuan chuyen nhuong va cap nhat chu so huu xe." });
    }

    [HttpPatch("{id}/reject")]
    public async Task<IActionResult> Reject(int id, [FromBody] RejectDelegationRequest request)
    {
        var delegation = await _context.VehicleDelegations.FindAsync(id);
        if (delegation == null)
            return NotFound(new { message = "Khong tim thay yeu cau chuyen nhuong." });

        var currentEmployeeId = _permissionService.GetCurrentEmployeeId(User);
        var approvalEmployeeId = delegation.RequestedByEmployeeId == delegation.ToEmployeeId
            ? delegation.FromEmployeeId
            : delegation.ToEmployeeId;
        if (approvalEmployeeId != currentEmployeeId)
            return Forbid();

        if (delegation.Status != DelegationStatuses.Pending)
            return BadRequest(new { message = "Yeu cau khong o trang thai cho duyet." });

        delegation.Status = DelegationStatuses.Rejected;
        delegation.RespondedAtUtc = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        var delegationWithNav = await _context.VehicleDelegations
            .Include(d => d.FromEmployee).Include(d => d.Vehicle)
            .FirstOrDefaultAsync(d => d.VehicleDelegationId == id);
        if (delegationWithNav != null)
        {
            await _notificationService.NotifyEventAsync("Approval.VehicleDelegation.Rejected",
                "Chuyển nhượng xe bị từ chối",
                $"{delegationWithNav.ToEmployee?.FullName ?? "Người nhận"} đã từ chối nhận quyền sở hữu xe {delegationWithNav.Vehicle?.LicensePlate}.",
                "VehicleDelegation", id.ToString(),
                "/vehicle-transfer");
        }

        return Ok(new { message = "Da tu choi yeu cau chuyen nhuong xe." });
    }

    [HttpPatch("{id}/revoke")]
    public async Task<IActionResult> Revoke(int id)
    {
        var delegation = await _context.VehicleDelegations.FindAsync(id);
        if (delegation == null)
            return NotFound(new { message = "Khong tim thay yeu cau chuyen nhuong." });

        var currentEmployeeId = _permissionService.GetCurrentEmployeeId(User);
        if ((delegation.RequestedByEmployeeId ?? delegation.FromEmployeeId) != currentEmployeeId)
            return Forbid();

        if (delegation.Status != DelegationStatuses.Pending)
            return BadRequest(new { message = "Chi co the huy yeu cau o trang thai cho duyet." });

        delegation.Status = DelegationStatuses.Revoked;
        delegation.RespondedAtUtc = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return Ok(new { message = "Da huy yeu cau chuyen nhuong." });
    }

    public sealed record CreateDelegationRequest(int VehicleId, int ToEmployeeId, string? Reason);
    public sealed record RequestOwnershipRequest(int VehicleId, string? Reason);
    public sealed record RejectDelegationRequest(string? Reason);
}
