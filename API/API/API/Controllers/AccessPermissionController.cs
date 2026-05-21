using API.Data;
using API.DTOs;
using API.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;

namespace API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
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
                return BadRequest(GateApiResponse.CreateError("Dữ liệu gửi lên không hợp lệ."));

            if (request.EmployeeId == null && request.VisitorDetailId == null)
                return BadRequest(GateApiResponse.CreateError("Phải cung cấp EmployeeId hoặc VisitorDetailId."));

            var gateExists = await _context.Gates.AnyAsync(g => g.GateId == request.GateId);
            if (!gateExists)
                return NotFound(GateApiResponse.CreateError($"Không tìm thấy khu vực (Gate) có id = {request.GateId}."));

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
                return Ok(GateApiResponse.CreateSuccess("Cập nhật quyền truy cập khu vực thành công."));
            }
            catch (Exception ex)
            {
                return StatusCode(500, GateApiResponse.CreateError("Có lỗi xảy ra khi cập nhật dữ liệu.", ex.Message));
            }
        }
    }
}