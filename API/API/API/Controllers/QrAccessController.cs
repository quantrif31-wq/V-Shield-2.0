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
    public class QrAccessController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public QrAccessController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpPost("scan-access")]
        public async Task<IActionResult> ScanAccess([FromBody] QrScanAccessRequest request)
        {
            if (request == null)
            {
                return BadRequest(GateTransitApiResponse.CreateError("D? li?u g?i lên không h?p l?."));
            }

            if (request.CameraId <= 0)
            {
                return BadRequest(GateTransitApiResponse.CreateError("CameraId không h?p l?."));
            }

            // 1. Ki?m tra Camera & Gate
            var camera = await _context.Cameras.FirstOrDefaultAsync(c => c.CameraId == request.CameraId);
            if (camera == null || camera.GateId == null)
            {
                return NotFound(GateTransitApiResponse.CreateError("Camera không t?n t?i ho?c chua du?c gán khu v?c (Gate)."));
            }

            var gateId = camera.GateId.Value;

            // 2. B?o m?t: Ki?m tra m?t kh?u tài kho?n
            if (request.LoggedInUserId.HasValue && request.LoggedInUserId > 0)
            {
                if (string.IsNullOrWhiteSpace(request.UserPassword))
                {
                    return Unauthorized(GateTransitApiResponse.CreateError("Yêu c?u nh?p m?t kh?u tài kho?n d? s? d?ng Camera này."));
                }

                var currentUser = await _context.AppUsers.FirstOrDefaultAsync(u => u.UserId == request.LoggedInUserId.Value);
                if (currentUser == null)
                {
                    return Unauthorized(GateTransitApiResponse.CreateError("Không tìm th?y tài kho?n thao tác."));
                }

                // TODO: Ð?i thành hàm Verify Hash M?t kh?u c?a b?n (ví d? BCrypt.Verify)
                if (currentUser.PasswordHash != request.UserPassword)
                {
                    return Unauthorized(GateTransitApiResponse.CreateError("M?t kh?u tài kho?n không chính xác."));
                }
            }

            // 3. Xác d?nh danh tính (Bám sát logic Client g?i ID ho?c t? Query b?ng Payload)
            int? targetEmployeeId = request.EmployeeId;
            int? targetVisitorId = request.VisitorDetailId;

            if (targetVisitorId == null && targetEmployeeId == null && !string.IsNullOrWhiteSpace(request.QrPayload))
            {
                var visitorMatch = await _context.VisitorDetails
                    .FirstOrDefaultAsync(v => v.QrPayload == request.QrPayload && v.IsQrActive);

                if (visitorMatch != null)
                {
                    targetVisitorId = visitorMatch.VisitorDetailId;
                }
            }

            if (targetEmployeeId == null && targetVisitorId == null)
            {
                return BadRequest(GateTransitApiResponse.CreateError("Không xác d?nh du?c danh tính t? d? li?u QR."));
            }

            // 4. Transaction & Ki?m tra quy?n
            await using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                bool hasAccess = false;
                string userType = targetEmployeeId.HasValue ? "Nhân viên" : "Khách";

                if (targetEmployeeId.HasValue)
                {
                    var employee = await _context.Employees.FirstOrDefaultAsync(e => e.EmployeeId == targetEmployeeId.Value);
                    if (employee == null)
                        return NotFound(GateTransitApiResponse.CreateError($"Không tìm th?y nhân viên có id = {targetEmployeeId.Value}."));

                    var permission = await _context.EmployeeAccessPermissions
                        .FirstOrDefaultAsync(p => p.EmployeeId == targetEmployeeId.Value && p.GateId == gateId);

                    hasAccess = permission != null && permission.IsAllowed;
                }
                else if (targetVisitorId.HasValue)
                {
                    // Logic yêu c?u khách ph?i có Status APPROVED
                    var visitor = await _context.VisitorDetails
                        .Include(v => v.Registration)
                        .FirstOrDefaultAsync(v =>
                            v.VisitorDetailId == targetVisitorId.Value &&
                            v.IsQrActive &&
                            v.Registration != null &&
                            v.Registration.Status.ToUpper() == "APPROVED");

                    if (visitor == null)
                    {
                        return NotFound(GateTransitApiResponse.CreateError("Không tìm th?y khách dã du?c xác nh?n (ho?c QR không còn hi?u l?c)."));
                    }

                    var permission = await _context.VisitorAccessPermissions
                        .FirstOrDefaultAsync(p => p.VisitorDetailId == targetVisitorId.Value && p.GateId == gateId);

                    hasAccess = permission != null && permission.IsAllowed;
                }

                // 5. Ghi Log (Luu ý: Ð? tr?ng bi?n s?, DB v?n nh?n bình thu?ng)
                var logStatus = hasAccess ? "SUCCESS" : "FAILED_DENIED";
                var logNote = hasAccess
                    ? $"Xác th?c QR thành công. {userType} du?c phép vào khu v?c."
                    : $"T? ch?i. {userType} không có quy?n truy c?p khu v?c này.";

                var newLog = new AccessLog
                {
                    Timestamp = DateTime.Now,
                    Direction = "IN",
                    GateId = gateId,
                    CameraId = request.CameraId,
                    EmployeeId = targetEmployeeId,
                    VisitorDetailId = targetVisitorId,
                    CapturedLicensePlate = null, // <- B? tr?ng dúng c?u trúc DB
                    ResultStatus = logStatus,
                    IsBypass = false,
                    Note = logNote
                };

                _context.AccessLogs.Add(newLog);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                // 6. Tr? k?t qu?
                if (!hasAccess)
                {
                    return StatusCode(403, GateTransitApiResponse.CreateError(logNote, new { LogId = newLog.LogId }));
                }

                return Ok(GateTransitApiResponse.CreateSuccess(logNote, new
                {
                    LogId = newLog.LogId,
                    EmployeeId = targetEmployeeId,
                    VisitorDetailId = targetVisitorId,
                    GateId = gateId,
                    Timestamp = newLog.Timestamp
                }));
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return StatusCode(500, GateTransitApiResponse.CreateError("Có l?i x?y ra khi x? lý d? li?u.", ex.Message));
            }
        }
    }
}
