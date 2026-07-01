using API.Data;
using API.DTOs;
using API.Middleware;
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
    [Authorize]
    [RequireOperationalTask("qr-access")]
    public class QrAccessController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly StaticVisitorQrService _visitorQrService;
        private readonly IZoneTransitService _zoneTransitService;
        private readonly EvidenceCaptureService _evidenceCapture;
        private readonly UserOperationalScopeService _scopeService;

        public QrAccessController(ApplicationDbContext context, StaticVisitorQrService visitorQrService, IZoneTransitService zoneTransitService, EvidenceCaptureService evidenceCapture, UserOperationalScopeService scopeService)
        {
            _context = context;
            _visitorQrService = visitorQrService;
            _zoneTransitService = zoneTransitService;
            _evidenceCapture = evidenceCapture;
            _scopeService = scopeService;
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

                var sourceRef = $"access-log/{newLog.LogId}";
                newLog.CapturedQrSnapshotUrl = await _evidenceCapture.CaptureBase64Async(request.QrSnapshotBase64, "qr-snapshot", sourceRef);
                newLog.CapturedFaceImageUrl = await _evidenceCapture.CaptureBase64Async(request.FaceSnapshotBase64, "face-crop", sourceRef);
                newLog.CapturedSnapshotUrl = newLog.CapturedQrSnapshotUrl ?? newLog.CapturedFaceImageUrl;
                await _context.SaveChangesAsync();

                await transaction.CommitAsync();

                if (targetEmployeeId.HasValue)
                {
                    _ = _zoneTransitService.ProcessTransitAsync(
                        targetEmployeeId.Value, gateId, "IN", newLog.Timestamp ?? DateTime.Now, ZoneTransitSources.Qr);
                }

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

        [HttpPost("manual-access")]
        [EnableRateLimiting("ops")]
        public async Task<IActionResult> ManualAccess([FromBody] ManualAccessRequest request)
        {
            if (request == null)
                return BadRequest(GateTransitApiResponse.CreateError("Dữ liệu gửi lên không hợp lệ."));

            var currentUserId = GetCurrentUserId();
            if (!currentUserId.HasValue)
                return Unauthorized(GateTransitApiResponse.CreateError("Không xác định được tài khoản đăng nhập."));

            var currentUser = await _context.AppUsers.FirstOrDefaultAsync(u => u.UserId == currentUserId.Value);
            if (currentUser == null || !currentUser.IsActive)
                return Unauthorized(GateTransitApiResponse.CreateError("Tài khoản không hợp lệ hoặc không còn hoạt động."));

            if (!string.Equals(currentUser.Role, "Admin", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(currentUser.Role, "BaoVe", StringComparison.OrdinalIgnoreCase))
                return Forbid();

            var gate = await _context.Gates.FirstOrDefaultAsync(g => g.GateId == request.GateId);
            if (gate == null)
                return NotFound(GateTransitApiResponse.CreateError("Cổng không tồn tại."));

            if (!await _scopeService.CanAccessAsync(User, UserOperationalScopeService.TaskQrAccess, gateId: request.GateId, requireManage: true))
                return Forbid();

            if (request.EmployeeId == null && request.VisitorDetailId == null)
                return BadRequest(GateTransitApiResponse.CreateError("Phải cung cấp mã nhân viên hoặc mã khách."));

            await using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                bool hasAccess = false;
                bool deniedByGuard = request.IsDenied;
                string userType = request.EmployeeId.HasValue ? "Nhân viên" : "Khách";
                string subjectName = "";

                if (request.EmployeeId.HasValue)
                {
                    var employee = await _context.Employees.FirstOrDefaultAsync(e => e.EmployeeId == request.EmployeeId.Value);
                    if (employee == null)
                        return NotFound(GateTransitApiResponse.CreateError($"Không tìm thấy nhân viên có id = {request.EmployeeId.Value}."));
                    subjectName = employee.FullName ?? "";

                    if (!deniedByGuard)
                    {
                        var permission = await _context.EmployeeAccessPermissions
                            .FirstOrDefaultAsync(p => p.EmployeeId == request.EmployeeId.Value && p.GateId == request.GateId);
                        hasAccess = permission != null && permission.IsAllowed;
                    }
                }
                else if (request.VisitorDetailId.HasValue)
                {
                    var visitor = await _context.VisitorDetails
                        .Include(v => v.Registration)
                        .FirstOrDefaultAsync(v =>
                            v.VisitorDetailId == request.VisitorDetailId.Value &&
                            v.IsQrActive &&
                            v.Registration != null &&
                            v.Registration.Status != null &&
                            v.Registration.Status.ToUpper() == "APPROVED");

                    if (visitor == null)
                        return NotFound(GateTransitApiResponse.CreateError("Không tìm thấy khách đã được xác nhận."));
                    subjectName = visitor.FullName ?? "";

                    if (!deniedByGuard)
                    {
                        var permission = await _context.VisitorAccessPermissions
                            .FirstOrDefaultAsync(p => p.VisitorDetailId == request.VisitorDetailId.Value && p.GateId == request.GateId);
                        hasAccess = permission != null && permission.IsAllowed;
                    }
                }

                bool accessGranted = !deniedByGuard && hasAccess;
                var reasonText = !string.IsNullOrWhiteSpace(request.Reason) ? $" Lý do: {request.Reason}" : "";
                string logNote;

                if (deniedByGuard)
                {
                    logNote = $"Bảo vệ từ chối thủ công — nhân dạng không khớp. {userType}: {subjectName}.{reasonText}";
                }
                else if (hasAccess)
                {
                    logNote = $"Vào cổng thủ công (QR tê liệt). {userType} được phép vào khu vực.{reasonText}";
                }
                else
                {
                    logNote = $"Từ chối. {userType} không có quyền truy cập khu vực này.{reasonText}";
                }

                var newLog = new AccessLog
                {
                    Timestamp = DateTime.Now,
                    Direction = "IN",
                    GateId = request.GateId,
                    CameraId = null,
                    EmployeeId = request.EmployeeId,
                    VisitorDetailId = request.VisitorDetailId,
                    CapturedLicensePlate = null,
                    ResultStatus = accessGranted ? "SUCCESS" : "FAILED_DENIED",
                    IsBypass = true,
                    Note = logNote
                };

                _context.AccessLogs.Add(newLog);
                await _context.SaveChangesAsync();

                var sourceRef = $"access-log/{newLog.LogId}";
                newLog.CapturedFaceImageUrl = await _evidenceCapture.CaptureBase64Async(request.FaceSnapshotBase64, "face-crop", sourceRef);
                newLog.CapturedSnapshotUrl = newLog.CapturedFaceImageUrl;
                await _context.SaveChangesAsync();

                await transaction.CommitAsync();

                if (request.EmployeeId.HasValue && hasAccess)
                {
                    _ = _zoneTransitService.ProcessTransitAsync(
                        request.EmployeeId.Value, request.GateId, "IN", newLog.Timestamp ?? DateTime.Now, ZoneTransitSources.Qr);
                }

                if (accessGranted)
                {
                    return Ok(GateTransitApiResponse.CreateSuccess(logNote, new
                    {
                        LogId = newLog.LogId,
                        EmployeeId = request.EmployeeId,
                        VisitorDetailId = request.VisitorDetailId,
                        SubjectName = subjectName,
                        GateId = request.GateId,
                        Timestamp = newLog.Timestamp
                    }));
                }

                return StatusCode(403, GateTransitApiResponse.CreateError(logNote, new
                {
                    LogId = newLog.LogId,
                    EmployeeId = request.EmployeeId,
                    VisitorDetailId = request.VisitorDetailId,
                    SubjectName = subjectName,
                    GateId = request.GateId
                }));
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return StatusCode(500, GateTransitApiResponse.CreateError("Có lỗi xảy ra khi xử lý dữ liệu.", ex.Message));
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

            var canAccessScope = await _scopeService.CanAccessAsync(
                User,
                UserOperationalScopeService.TaskQrAccess,
                gateId: camera.GateId,
                requireManage: true);

            if (!canAccessScope)
            {
                return (false, "Tai khoan khong duoc phan cong van hanh cong nay.", null);
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

