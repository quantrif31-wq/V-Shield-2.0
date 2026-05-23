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
                return BadRequest(GateTransitApiResponse.CreateError("Dữ liệu gửi lên không hợp lệ."));
            }

            if (request.CameraId <= 0)
            {
                return BadRequest(GateTransitApiResponse.CreateError("CameraId không hợp lệ."));
            }

            // 1. Kiểm tra Camera và Gate
            var camera = await _context.Cameras.FirstOrDefaultAsync(c => c.CameraId == request.CameraId);
            if (camera == null || camera.GateId == null)
            {
                return NotFound(GateTransitApiResponse.CreateError("Camera không tồn tại hoặc chưa được gán khu vực (Gate)."));
            }

            var gateId = camera.GateId.Value;

            // 2. Bảo mật: Kiểm tra mật khẩu tài khoản
            if (request.LoggedInUserId.HasValue && request.LoggedInUserId > 0)
            {
                if (string.IsNullOrWhiteSpace(request.UserPassword))
                {
                    return Unauthorized(GateTransitApiResponse.CreateError("Yêu cầu nhập mật khẩu tài khoản để sử dụng Camera này."));
                }

                var currentUser = await _context.AppUsers.FirstOrDefaultAsync(u => u.UserId == request.LoggedInUserId.Value);
                if (currentUser == null)
                {
                    return Unauthorized(GateTransitApiResponse.CreateError("Không tìm thấy tài khoản thao tác."));
                }

                // Temporary plain-text comparison; replace with password-hash verification (e.g., BCrypt) in auth hardening phase.
                if (currentUser.PasswordHash != request.UserPassword)
                {
                    return Unauthorized(GateTransitApiResponse.CreateError("Mật khẩu tài khoản không chính xác."));
                }
            }

            // 3. Xác định danh tính (bám sát logic client gửi ID hoặc từ query bằng payload)
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
                return BadRequest(GateTransitApiResponse.CreateError("Không xác định được danh tính từ dữ liệu QR."));
            }

            // 4. Transaction và kiểm tra quyền
            await using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                bool hasAccess = false;
                string userType = targetEmployeeId.HasValue ? "Nhân viên" : "Khách";

                if (targetEmployeeId.HasValue)
                {
                    var employee = await _context.Employees.FirstOrDefaultAsync(e => e.EmployeeId == targetEmployeeId.Value);
                    if (employee == null)
                        return NotFound(GateTransitApiResponse.CreateError($"Không tìm thấy nhân viên có id = {targetEmployeeId.Value}."));

                    var permission = await _context.EmployeeAccessPermissions
                        .FirstOrDefaultAsync(p => p.EmployeeId == targetEmployeeId.Value && p.GateId == gateId);

                    hasAccess = permission != null && permission.IsAllowed;
                }
                else if (targetVisitorId.HasValue)
                {
                    // Logic yêu cầu khách phải có Status APPROVED
                    var visitor = await _context.VisitorDetails
                        .Include(v => v.Registration)
                        .FirstOrDefaultAsync(v =>
                            v.VisitorDetailId == targetVisitorId.Value &&
                            v.IsQrActive &&
                            v.Registration != null &&
                            v.Registration.Status.ToUpper() == "APPROVED");

                    if (visitor == null)
                    {
                        return NotFound(GateTransitApiResponse.CreateError("Không tìm thấy khách đã được xác nhận (hoặc QR không còn hiệu lực)."));
                    }

                    var permission = await _context.VisitorAccessPermissions
                        .FirstOrDefaultAsync(p => p.VisitorDetailId == targetVisitorId.Value && p.GateId == gateId);

                    hasAccess = permission != null && permission.IsAllowed;
                }

                // 5. Ghi log (lưu ý: để trống biển số, DB vẫn nhận bình thường)
                var logStatus = hasAccess ? "SUCCESS" : "FAILED_DENIED";
                var logNote = hasAccess
                    ? $"Xác thực QR thành công. {userType} được phép vào khu vực."
                    : $"Từ chối. {userType} không có quyền truy cập khu vực này.";

                var newLog = new AccessLog
                {
                    Timestamp = DateTime.Now,
                    Direction = "IN",
                    GateId = gateId,
                    CameraId = request.CameraId,
                    EmployeeId = targetEmployeeId,
                    VisitorDetailId = targetVisitorId,
                    CapturedLicensePlate = null, // <- để trống đúng cấu trúc DB
                    ResultStatus = logStatus,
                    IsBypass = false,
                    Note = logNote
                };

                _context.AccessLogs.Add(newLog);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                // 6. Trả kết quả
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
                return StatusCode(500, GateTransitApiResponse.CreateError("Có lỗi xảy ra khi xử lý dữ liệu.", ex.Message));
            }
        }
    }
}
