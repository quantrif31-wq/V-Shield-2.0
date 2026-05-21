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
                return BadRequest(GateApiResponse.CreateError("Dữ liệu gửi lên không hợp lệ."));
            }

            if (request.CameraId <= 0)
            {
                return BadRequest(GateApiResponse.CreateError("CameraId không hợp lệ."));
            }

            // 1. Kiểm tra Camera & Gate
            var camera = await _context.Cameras.FirstOrDefaultAsync(c => c.CameraId == request.CameraId);
            if (camera == null || camera.GateId == null)
            {
                return NotFound(GateApiResponse.CreateError("Camera không tồn tại hoặc chưa được gán khu vực (Gate)."));
            }

            var gateId = camera.GateId.Value;

            // 2. Bảo mật: Kiểm tra mật khẩu tài khoản
            if (request.LoggedInUserId.HasValue && request.LoggedInUserId > 0)
            {
                if (string.IsNullOrWhiteSpace(request.UserPassword))
                {
                    return Unauthorized(GateApiResponse.CreateError("Yêu cầu nhập mật khẩu tài khoản để sử dụng Camera này."));
                }

                var currentUser = await _context.AppUsers.FirstOrDefaultAsync(u => u.UserId == request.LoggedInUserId.Value);
                if (currentUser == null)
                {
                    return Unauthorized(GateApiResponse.CreateError("Không tìm thấy tài khoản thao tác."));
                }

                // TODO: Đổi thành hàm Verify Hash Mật khẩu của bạn (ví dụ BCrypt.Verify)
                if (currentUser.PasswordHash != request.UserPassword)
                {
                    return Unauthorized(GateApiResponse.CreateError("Mật khẩu tài khoản không chính xác."));
                }
            }

            // 3. Xác định danh tính (Bám sát logic Client gửi ID hoặc tự Query bằng Payload)
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
                return BadRequest(GateApiResponse.CreateError("Không xác định được danh tính từ dữ liệu QR."));
            }

            // 4. Transaction & Kiểm tra quyền
            await using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                bool hasAccess = false;
                string userType = targetEmployeeId.HasValue ? "Nhân viên" : "Khách";

                if (targetEmployeeId.HasValue)
                {
                    var employee = await _context.Employees.FirstOrDefaultAsync(e => e.EmployeeId == targetEmployeeId.Value);
                    if (employee == null)
                        return NotFound(GateApiResponse.CreateError($"Không tìm thấy nhân viên có id = {targetEmployeeId.Value}."));

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
                        return NotFound(GateApiResponse.CreateError("Không tìm thấy khách đã được xác nhận (hoặc QR không còn hiệu lực)."));
                    }

                    var permission = await _context.VisitorAccessPermissions
                        .FirstOrDefaultAsync(p => p.VisitorDetailId == targetVisitorId.Value && p.GateId == gateId);

                    hasAccess = permission != null && permission.IsAllowed;
                }

                // 5. Ghi Log (Lưu ý: Để trống biển số, DB vẫn nhận bình thường)
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
                    CapturedLicensePlate = null, // <- Bỏ trống đúng cấu trúc DB
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
                    return StatusCode(403, GateApiResponse.CreateError(logNote, new { LogId = newLog.LogId }));
                }

                return Ok(GateApiResponse.CreateSuccess(logNote, new
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
                return StatusCode(500, GateApiResponse.CreateError("Có lỗi xảy ra khi xử lý dữ liệu.", ex.Message));
            }
        }
    }
}