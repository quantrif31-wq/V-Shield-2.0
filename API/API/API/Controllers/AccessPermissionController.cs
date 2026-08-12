using API.Data;
using API.DTOs;
using API.Middleware;
using API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;

namespace API.Controllers
{
    [Route("api/access-permissions")]
    [ApiController]
    [Authorize]
    [RequireOperationalTask("restricted-zone")]
    public class AccessPermissionController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public AccessPermissionController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpPost("set-permission")]
        public async Task<IActionResult> SetPermission([FromBody] SetPermissionRequest request)
        {
            if (request == null)
                return BadRequest(GateTransitApiResponse.CreateError("Du lieu gui len khong hop le."));

            if (request.EmployeeId == null && request.VisitorDetailId == null)
                return BadRequest(GateTransitApiResponse.CreateError("Phai cung cap EmployeeId hoac VisitorDetailId."));

            var gateExists = await _context.Gates.AnyAsync(g => g.GateId == request.GateId);
            if (!gateExists)
                return NotFound(GateTransitApiResponse.CreateError($"Khong tim thay khu vuc (Gate) co id = {request.GateId}."));

            try
            {
                if (request.EmployeeId.HasValue)
                {
                    var permission = await _context.EmployeeAccessPermissions
                        .FirstOrDefaultAsync(p => p.EmployeeId == request.EmployeeId && p.GateId == request.GateId);

                    if (permission == null)
                    {
                        _context.EmployeeAccessPermissions.Add(new EmployeeAccessPermission
                        {
                            EmployeeId = request.EmployeeId.Value,
                            GateId = request.GateId,
                            IsAllowed = request.IsAllowed
                        });
                    }
                    else
                    {
                        permission.IsAllowed = request.IsAllowed;
                    }
                }
                else if (request.VisitorDetailId.HasValue)
                {
                    var permission = await _context.VisitorAccessPermissions
                        .FirstOrDefaultAsync(p => p.VisitorDetailId == request.VisitorDetailId && p.GateId == request.GateId);

                    if (permission == null)
                    {
                        _context.VisitorAccessPermissions.Add(new VisitorAccessPermission
                        {
                            VisitorDetailId = request.VisitorDetailId.Value,
                            GateId = request.GateId,
                            IsAllowed = request.IsAllowed
                        });
                    }
                    else
                    {
                        permission.IsAllowed = request.IsAllowed;
                    }
                }

                await _context.SaveChangesAsync();
                return Ok(GateTransitApiResponse.CreateSuccess("Cap nhat quyen truy cap khu vuc thanh cong."));
            }
            catch (Exception ex)
            {
                return StatusCode(500, GateTransitApiResponse.CreateError("Co loi xay ra khi cap nhat du lieu.", ex.Message));
            }
        }

        [HttpPost("position/toggle-gate")]
        public async Task<IActionResult> TogglePositionGate([FromBody] TogglePositionGateRequest request)
        {
            if (request == null)
                return BadRequest(GateTransitApiResponse.CreateError("Du lieu gui len khong hop le."));

            if (!await _context.Positions.AnyAsync(p => p.PositionId == request.PositionId))
                return NotFound(GateTransitApiResponse.CreateError($"Khong tim thay chuc vu co id = {request.PositionId}."));

            if (!await _context.Gates.AnyAsync(g => g.GateId == request.GateId))
                return NotFound(GateTransitApiResponse.CreateError($"Khong tim thay khu vuc (Gate) co id = {request.GateId}."));

            try
            {
                var row = await _context.PositionAccessPermissions
                    .FirstOrDefaultAsync(p => p.PositionId == request.PositionId && p.GateId == request.GateId);

                if (request.Enabled)
                {
                    if (row == null)
                    {
                        _context.PositionAccessPermissions.Add(new PositionAccessPermission
                        {
                            PositionId = request.PositionId,
                            GateId = request.GateId,
                            IsAllowed = true
                        });
                    }
                    else
                    {
                        row.IsAllowed = true;
                    }
                }
                else
                {
                    if (row != null)
                    {
                        _context.PositionAccessPermissions.Remove(row);
                    }
                }

                await _context.SaveChangesAsync();
                return Ok(GateTransitApiResponse.CreateSuccess(
                    request.Enabled ? "Da bat quyen khu vuc mac dinh cho chuc vu." : "Da tat quyen khu vuc mac dinh cho chuc vu."));
            }
            catch (Exception ex)
            {
                return StatusCode(500, GateTransitApiResponse.CreateError("Co loi xay ra khi cap nhat du lieu.", ex.Message));
            }
        }

        [HttpPost("employee/toggle-gate")]
        public async Task<IActionResult> ToggleEmployeeGate([FromBody] ToggleEmployeeGateRequest request)
        {
            if (request == null)
                return BadRequest(GateTransitApiResponse.CreateError("Du lieu gui len khong hop le."));

            var employee = await _context.Employees
                .AsNoTracking()
                .FirstOrDefaultAsync(e => e.EmployeeId == request.EmployeeId);

            if (employee == null)
                return NotFound(GateTransitApiResponse.CreateError($"Khong tim thay nhan vien co id = {request.EmployeeId}."));

            if (!await _context.Gates.AnyAsync(g => g.GateId == request.GateId))
                return NotFound(GateTransitApiResponse.CreateError($"Khong tim thay khu vuc (Gate) co id = {request.GateId}."));

            try
            {
                var explicitRow = await _context.EmployeeAccessPermissions
                    .FirstOrDefaultAsync(p => p.EmployeeId == request.EmployeeId && p.GateId == request.GateId);

                if (request.Enabled)
                {
                    // Nếu chức vụ đã có quyền mặc định ở khu vực này thì xóa override tay để
                    // quay về kế thừa; ngược lại tạo dòng quyền tường minh (gạt bật).
                    var inherited = employee.PositionId.HasValue &&
                        await _context.PositionAccessPermissions.AnyAsync(p =>
                            p.PositionId == employee.PositionId.Value &&
                            p.GateId == request.GateId &&
                            p.IsAllowed);

                    if (inherited)
                    {
                        if (explicitRow != null)
                            _context.EmployeeAccessPermissions.Remove(explicitRow);
                    }
                    else if (explicitRow == null)
                    {
                        _context.EmployeeAccessPermissions.Add(new EmployeeAccessPermission
                        {
                            EmployeeId = request.EmployeeId,
                            GateId = request.GateId,
                            IsAllowed = true
                        });
                    }
                    else
                    {
                        explicitRow.IsAllowed = true;
                    }
                }
                else
                {
                    // Gạt tắt tay: tạo dòng override IsAllowed=false (chặn cả khi chức vụ cho phép)
                    if (explicitRow == null)
                    {
                        _context.EmployeeAccessPermissions.Add(new EmployeeAccessPermission
                        {
                            EmployeeId = request.EmployeeId,
                            GateId = request.GateId,
                            IsAllowed = false
                        });
                    }
                    else
                    {
                        explicitRow.IsAllowed = false;
                    }
                }

                await _context.SaveChangesAsync();
                return Ok(GateTransitApiResponse.CreateSuccess(
                    request.Enabled ? "Da bat quyen khu vuc cho nhan vien." : "Da tat quyen khu vuc cho nhan vien."));
            }
            catch (Exception ex)
            {
                return StatusCode(500, GateTransitApiResponse.CreateError("Co loi xay ra khi cap nhat du lieu.", ex.Message));
            }
        }
    }
}
