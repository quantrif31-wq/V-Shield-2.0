using API.Data;
using API.DTOs;
using API.Models;
using API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text.RegularExpressions;
using System;
using System.Threading.Tasks;

namespace API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Admin,BaoVe")]
    public class QrAccessController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly StaticVisitorQrService _visitorQrService;

        public QrAccessController(ApplicationDbContext context, StaticVisitorQrService visitorQrService)
        {
            _context = context;
            _visitorQrService = visitorQrService;
        }

        [HttpPost("scan-access")]
        [EnableRateLimiting("ops")]
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

            var verify = await ValidateCameraAndUserAccess(request);
            if (!verify.Ok || verify.Camera == null)
            {
                return Unauthorized(GateTransitApiResponse.CreateError(verify.Message ?? "Khong the xac thuc thao tac camera."));
            }

            var camera = verify.Camera;
            var gateId = camera.GateId!.Value;

            // 3. Xác định danh tính (bám sát logic client gửi ID hoặc tự query bằng payload)
            int? targetEmployeeId = request.EmployeeId;
            int? targetVisitorId = request.VisitorDetailId;

            if (targetVisitorId == null && targetEmployeeId == null && !string.IsNullOrWhiteSpace(request.QrPayload))
            {
                var normalizedPayload = request.QrPayload.Trim();

                var employeeIdFromPayload = TryParseEmployeeIdFromDynamicPayload(normalizedPayload);
                if (employeeIdFromPayload.HasValue && employeeIdFromPayload.Value > 0)
                {
                    targetEmployeeId = employeeIdFromPayload.Value;
                }
                if (targetEmployeeId == null)
                {
                    if (_visitorQrService.TryParsePayload(normalizedPayload, out var visitorPayload, out _) && visitorPayload != null)
                    {
                        var visitorMatch = await _context.VisitorDetails
                            .FirstOrDefaultAsync(v =>
                                v.VisitorDetailId == visitorPayload.VisitorId &&
                                v.RegistrationId == visitorPayload.RegistrationId &&
                                v.IsQrActive);

                        if (visitorMatch != null && !string.IsNullOrWhiteSpace(visitorMatch.QrSecret))
                        {
                            var nowCounter = _visitorQrService.GetCurrentCounter(DateTime.UtcNow);
                            var expectedOtp = _visitorQrService.GenerateOtp(visitorMatch.QrSecret, visitorPayload.Counter);
                            var isFresh = Math.Abs(visitorPayload.Counter - nowCounter) <= 1;
                            var isOtpValid = string.Equals(visitorPayload.Otp, expectedOtp, StringComparison.Ordinal);

                            if (isFresh && isOtpValid)
                            {
                                targetVisitorId = visitorMatch.VisitorDetailId;
                            }
                        }
                    }
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
                string subjectName = "";

                if (targetEmployeeId.HasValue)
                {
                    var employee = await _context.Employees.FirstOrDefaultAsync(e => e.EmployeeId == targetEmployeeId.Value);
                    if (employee == null)
                        return NotFound(GateTransitApiResponse.CreateError($"Không tìm thấy nhân viên có id = {targetEmployeeId.Value}."));
                    subjectName = employee.FullName ?? "";

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
                            v.Registration.Status != null &&
                            v.Registration.Status.ToUpper() == "APPROVED");

                    if (visitor == null)
                    {
                        return NotFound(GateTransitApiResponse.CreateError("Không tìm thấy khách đã được xác nhận (hoặc QR không còn hiệu lực)."));
                    }
                    subjectName = visitor.FullName ?? "";

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
                    return StatusCode(403, GateTransitApiResponse.CreateError(logNote, new
                    {
                        LogId = newLog.LogId,
                        EmployeeId = targetEmployeeId,
                        VisitorDetailId = targetVisitorId,
                        SubjectName = subjectName,
                        GateId = gateId
                    }));
                }

                return Ok(GateTransitApiResponse.CreateSuccess(logNote, new
                {
                    LogId = newLog.LogId,
                    EmployeeId = targetEmployeeId,
                    VisitorDetailId = targetVisitorId,
                    SubjectName = subjectName,
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

        [HttpPost("verify-camera-auth")]
        [EnableRateLimiting("ops")]
        public async Task<IActionResult> VerifyCameraAuth([FromBody] QrScanAccessRequest request)
        {
            if (request == null)
            {
                return BadRequest(GateTransitApiResponse.CreateError("Du lieu gui len khong hop le."));
            }

            try
            {
                var verify = await ValidateCameraAndUserAccess(request);
                if (!verify.Ok || verify.Camera == null)
                {
                    return Unauthorized(GateTransitApiResponse.CreateError(verify.Message ?? "Khong the xac thuc camera."));
                }

                return Ok(GateTransitApiResponse.CreateSuccess("Xac thuc camera thanh cong.", new
                {
                    cameraId = verify.Camera.CameraId,
                    gateId = verify.Camera.GateId
                }));
            }
            catch (Exception ex)
            {
                return StatusCode(500, GateTransitApiResponse.CreateError("Co loi khi xac thuc camera.", ex.Message));
            }
        }

        private async Task<(bool Ok, string? Message, Camera? Camera)> ValidateCameraAndUserAccess(QrScanAccessRequest request)
        {
            if (request.CameraId <= 0)
            {
                return (false, "CameraId khong hop le.", null);
            }

            var camera = await _context.Cameras.FirstOrDefaultAsync(c => c.CameraId == request.CameraId);
            if (camera == null || camera.GateId == null)
            {
                return (false, "Camera khong ton tai hoac chua duoc gan Gate.", null);
            }

            var currentUserId = GetCurrentUserId();
            if (!currentUserId.HasValue)
            {
                return (false, "Khong xac dinh duoc tai khoan dang nhap.", null);
            }

            var currentUser = await _context.AppUsers.FirstOrDefaultAsync(u => u.UserId == currentUserId.Value);
            if (currentUser == null)
            {
                return (false, "Khong tim thay tai khoan thao tac.", null);
            }

            if (!currentUser.IsActive)
            {
                return (false, "Tai khoan dang nhap khong con hoat dong.", null);
            }

            if (!string.Equals(currentUser.Role, "Admin", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(currentUser.Role, "BaoVe", StringComparison.OrdinalIgnoreCase))
            {
                return (false, "Tai khoan khong co quyen su dung camera nay.", null);
            }

            return (true, null, camera);
        }

        private int? GetCurrentUserId()
        {
            var raw = User.FindFirstValue(JwtRegisteredClaimNames.Sub);
            return int.TryParse(raw, out var id) ? id : null;
        }

        private static int? TryParseEmployeeIdFromDynamicPayload(string payload)
        {
            if (string.IsNullOrWhiteSpace(payload))
            {
                return null;
            }

            var match = Regex.Match(payload.Trim(), @"(?:^|\|)EMP:(\d+)(?:\||$)", RegexOptions.IgnoreCase);
            if (!match.Success || match.Groups.Count < 2)
            {
                return null;
            }

            if (int.TryParse(match.Groups[1].Value, out var employeeId) && employeeId > 0)
            {
                return employeeId;
            }

            return null;
        }
    }
}

