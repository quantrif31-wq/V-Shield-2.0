using API.Data;
using API.DTOs;
using API.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;

namespace API.Controllers
{
    [Route("api/access-permissions")]
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
                return BadRequest(GateTransitApiResponse.CreateError("D? li?u g?i lên không h?p l?."));

            if (request.EmployeeId == null && request.VisitorDetailId == null)
                return BadRequest(GateTransitApiResponse.CreateError("Ph?i cung c?p EmployeeId ho?c VisitorDetailId."));

            var gateExists = await _context.Gates.AnyAsync(g => g.GateId == request.GateId);
            if (!gateExists)
                return NotFound(GateTransitApiResponse.CreateError($"Không tìm th?y khu v?c (Gate) có id = {request.GateId}."));

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
                return Ok(GateTransitApiResponse.CreateSuccess("C?p nh?t quy?n truy c?p khu v?c thành công."));
            }
            catch (Exception ex)
            {
                return StatusCode(500, GateTransitApiResponse.CreateError("Có l?i x?y ra khi c?p nh?t d? li?u.", ex.Message));
            }
        }
    }
}


