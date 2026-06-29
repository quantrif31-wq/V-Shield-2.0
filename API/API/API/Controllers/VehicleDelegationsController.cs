using API.Data;
using API.Models;
using API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace API.Controllers;

[ApiController]
[Route("api/vehicle-delegations")]
[Authorize(Roles = "NhanVien,NhanSu,Admin")]
public class VehicleDelegationsController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly IAttendancePermissionService _permissionService;

    public VehicleDelegationsController(ApplicationDbContext context, IAttendancePermissionService permissionService)
    {
        _context = context;
        _permissionService = permissionService;
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
            return BadRequest(new { message = "Xe khong o trong bai, khong the uy quyen." });

        var activeDelegation = await _context.VehicleDelegations.AnyAsync(d =>
            d.VehicleId == request.VehicleId &&
            d.Status == DelegationStatuses.Pending);
        if (activeDelegation)
            return Conflict(new { message = "Xe nay dang co yeu cau uy quyen pending." });

        var delegation = new VehicleDelegation
        {
            VehicleId = request.VehicleId,
            FromEmployeeId = fromEmployeeId.Value,
            ToEmployeeId = request.ToEmployeeId,
            Reason = request.Reason?.Trim(),
            Status = DelegationStatuses.Pending,
            RequestedAtUtc = DateTime.UtcNow
        };

        _context.VehicleDelegations.Add(delegation);
        await _context.SaveChangesAsync();

        return Ok(delegation);
    }

    [HttpGet("outgoing")]
    public async Task<IActionResult> GetOutgoing()
    {
        var employeeId = _permissionService.GetCurrentEmployeeId(User);
        if (!employeeId.HasValue)
            return BadRequest(new { message = "Tai khoan hien tai chua lien ket nhan vien." });

        var data = await _context.VehicleDelegations
            .AsNoTracking()
            .Where(d => d.FromEmployeeId == employeeId.Value)
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
            .Where(d => d.ToEmployeeId == employeeId.Value)
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
            return NotFound(new { message = "Khong tim thay yeu cau uy quyen." });

        var currentEmployeeId = _permissionService.GetCurrentEmployeeId(User);
        if (delegation.ToEmployeeId != currentEmployeeId)
            return Forbid();

        if (delegation.Status != DelegationStatuses.Pending)
            return BadRequest(new { message = "Yeu cau khong o trang thai cho duyet." });

        delegation.Status = DelegationStatuses.Approved;
        delegation.RespondedAtUtc = DateTime.UtcNow;
        delegation.Vehicle.EmployeeId = delegation.ToEmployeeId;

        await _context.SaveChangesAsync();
        return Ok(new { message = "Da chap thuan uy quyen xe." });
    }

    [HttpPatch("{id}/reject")]
    public async Task<IActionResult> Reject(int id, [FromBody] RejectDelegationRequest request)
    {
        var delegation = await _context.VehicleDelegations.FindAsync(id);
        if (delegation == null)
            return NotFound(new { message = "Khong tim thay yeu cau uy quyen." });

        var currentEmployeeId = _permissionService.GetCurrentEmployeeId(User);
        if (delegation.ToEmployeeId != currentEmployeeId)
            return Forbid();

        if (delegation.Status != DelegationStatuses.Pending)
            return BadRequest(new { message = "Yeu cau khong o trang thai cho duyet." });

        delegation.Status = DelegationStatuses.Rejected;
        delegation.RespondedAtUtc = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return Ok(new { message = "Da tu choi uy quyen xe." });
    }

    [HttpPatch("{id}/revoke")]
    public async Task<IActionResult> Revoke(int id)
    {
        var delegation = await _context.VehicleDelegations.FindAsync(id);
        if (delegation == null)
            return NotFound(new { message = "Khong tim thay yeu cau uy quyen." });

        var currentEmployeeId = _permissionService.GetCurrentEmployeeId(User);
        if (delegation.FromEmployeeId != currentEmployeeId)
            return Forbid();

        if (delegation.Status != DelegationStatuses.Pending)
            return BadRequest(new { message = "Chi co the huy yeu cau o trang thai cho duyet." });

        delegation.Status = DelegationStatuses.Revoked;
        delegation.RespondedAtUtc = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return Ok(new { message = "Da huy yeu cau uy quyen." });
    }

    public sealed record CreateDelegationRequest(int VehicleId, int ToEmployeeId, string? Reason);
    public sealed record RejectDelegationRequest(string? Reason);
}
