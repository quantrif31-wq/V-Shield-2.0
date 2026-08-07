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
    }
}
