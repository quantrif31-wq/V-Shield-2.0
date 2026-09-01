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
using System.Security.Cryptography;
using System.Security.Claims;
using System.Text;
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

            // Cổng triển khai khai báo trên giao diện phải khớp với cổng gắn với camera đã chọn
            if (request.GateId.HasValue && request.GateId.Value != gateId)
            {
                return BadRequest(GateTransitApiResponse.CreateError("Cổng triển khai đã chọn không khớp với cổng gắn với camera. Vui lòng chọn lại cổng hoặc camera."));
            }

            // 3. Xác định danh tính (bám sát logic client gửi ID hoặc tự query bằng payload)
            int? targetEmployeeId = request.EmployeeId;
            int? targetVisitorId = request.VisitorDetailId;
            string? qrValidationError = null;

            if (targetVisitorId == null && targetEmployeeId == null && !string.IsNullOrWhiteSpace(request.QrPayload))
            {
                var normalizedPayload = request.QrPayload.Trim();

                var employeeIdFromPayload = TryParseEmployeeIdFromDynamicPayload(normalizedPayload);
                if (employeeIdFromPayload.HasValue && employeeIdFromPayload.Value > 0)
                {
                    var dynamicValidation = await TryValidateDynamicEmployeePayloadAsync(normalizedPayload, employeeIdFromPayload.Value);
                    if (dynamicValidation.Success)
                    {
                        targetEmployeeId = employeeIdFromPayload.Value;
                    }
                    else
                    {
                        qrValidationError = dynamicValidation.Message;
                    }
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
                                qrValidationError = null;
                            }
                        }
                    }
                }
            }

            if (targetEmployeeId == null && targetVisitorId == null)
            {
                return BadRequest(GateTransitApiResponse.CreateError(qrValidationError ?? "Không xác định được danh tính từ dữ liệu QR."));
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

                    // Quyền ưu tiên: Admin/tài khoản/vai trò → quyền gạt tay nhân viên → kế thừa chức vụ
                    bool? employeeAllowed = await ResolveEmployeeGateAccessAsync(employee, gateId);
                    if (employeeAllowed == null && employee.PositionId.HasValue)
                    {
                        var positionPermission = await _context.PositionAccessPermissions
                            .FirstOrDefaultAsync(p => p.PositionId == employee.PositionId.Value && p.GateId == gateId);
                        employeeAllowed = positionPermission?.IsAllowed;
                    }

                    hasAccess = employeeAllowed ?? false;
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

                if (targetEmployeeId.HasValue && hasAccess && !request.DeferTransit)
                {
                    await _zoneTransitService.ProcessAccessLogAsync(newLog.LogId);
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

                if (request.GateId.HasValue && request.GateId.Value != verify.Camera.GateId)
                {
                    return BadRequest(GateTransitApiResponse.CreateError("Cổng triển khai đã chọn không khớp với cổng gắn với camera. Vui lòng chọn lại cổng hoặc camera."));
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
                        // Quyền ưu tiên: Admin/tài khoản/vai trò → quyền gạt tay nhân viên → kế thừa chức vụ
                        bool? employeeAllowed = await ResolveEmployeeGateAccessAsync(employee, request.GateId);
                        if (employeeAllowed == null && employee.PositionId.HasValue)
                        {
                            var positionPermission = await _context.PositionAccessPermissions
                                .FirstOrDefaultAsync(p => p.PositionId == employee.PositionId.Value && p.GateId == request.GateId);
                            employeeAllowed = positionPermission?.IsAllowed;
                        }

                        hasAccess = employeeAllowed ?? false;
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

                if (request.EmployeeId.HasValue && accessGranted)
                {
                    await _zoneTransitService.ProcessAccessLogAsync(newLog.LogId);
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

        private async Task<(bool Success, string Message)> TryValidateDynamicEmployeePayloadAsync(string payload, int expectedEmployeeId)
        {
            var parseResult = ParseDynamicPayload(payload);
            if (!parseResult.Success)
            {
                return (false, parseResult.Message);
            }

            if (parseResult.EmployeeId != expectedEmployeeId)
            {
                return (false, "EmployeeId trong QR không khớp với payload.");
            }

            var dynamicQr = await _context.EmployeeDynamicQrs
                .Include(item => item.Employee)
                .FirstOrDefaultAsync(item => item.EmployeeId == expectedEmployeeId && item.IsActive);

            if (dynamicQr == null)
            {
                return (false, "Không tìm thấy cấu hình QR động của nhân viên.");
            }

            if (dynamicQr.Employee == null || dynamicQr.Employee.Status != true)
            {
                return (false, "Nhân viên không còn hoạt động.");
            }

            var utcNow = DateTime.UtcNow;
            var currentCounter = GetCurrentCounter(utcNow, dynamicQr.TimeStepSeconds);
            if (Math.Abs(parseResult.Counter!.Value - currentCounter) > 4)
            {
                return (false, "QR động đã hết hạn hoặc chưa đến hiệu lực.");
            }

            var expectedOtp = GenerateTotp(dynamicQr.SecretKey, parseResult.Counter.Value, dynamicQr.Digits);
            if (!FixedTimeEquals(parseResult.Otp!, expectedOtp))
            {
                return (false, "QR động không hợp lệ.");
            }

            return (true, "OK");
        }

        private static (bool Success, int? EmployeeId, long? Counter, string? Otp, string Message) ParseDynamicPayload(string payload)
        {
            try
            {
                var parts = payload.Split('|', StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length != 3)
                {
                    return (false, null, null, null, "Payload QR không đúng định dạng.");
                }

                var empPart = parts[0].Split(':');
                var tsPart = parts[1].Split(':');
                var otpPart = parts[2].Split(':');

                if (empPart.Length != 2 || tsPart.Length != 2 || otpPart.Length != 2)
                {
                    return (false, null, null, null, "Payload QR không đúng định dạng.");
                }

                if (!empPart[0].Equals("EMP", StringComparison.OrdinalIgnoreCase))
                {
                    return (false, null, null, null, "Thiếu EMP trong payload.");
                }

                if (!tsPart[0].Equals("TS", StringComparison.OrdinalIgnoreCase))
                {
                    return (false, null, null, null, "Thiếu TS trong payload.");
                }

                if (!otpPart[0].Equals("OTP", StringComparison.OrdinalIgnoreCase))
                {
                    return (false, null, null, null, "Thiếu OTP trong payload.");
                }

                if (!int.TryParse(empPart[1], out var employeeId))
                {
                    return (false, null, null, null, "EmployeeId không hợp lệ.");
                }

                if (!long.TryParse(tsPart[1], out var counter))
                {
                    return (false, null, null, null, "Counter không hợp lệ.");
                }

                var otp = otpPart[1]?.Trim();
                if (string.IsNullOrWhiteSpace(otp))
                {
                    return (false, null, null, null, "OTP không hợp lệ.");
                }

                return (true, employeeId, counter, otp, "OK");
            }
            catch
            {
                return (false, null, null, null, "Không thể phân tích payload QR.");
            }
        }

        private static long GetCurrentCounter(DateTime utcNow, int timeStepSeconds)
        {
            var unixTime = new DateTimeOffset(utcNow).ToUnixTimeSeconds();
            return unixTime / timeStepSeconds;
        }

        private static string GenerateTotp(string base32Secret, long counter, int digits)
        {
            var key = Base32Decode(base32Secret);
            var counterBytes = BitConverter.GetBytes(counter);

            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(counterBytes);
            }

            using var hmac = new HMACSHA1(key);
            var hash = hmac.ComputeHash(counterBytes);
            var offset = hash[^1] & 0x0F;

            int binaryCode =
                ((hash[offset] & 0x7F) << 24) |
                ((hash[offset + 1] & 0xFF) << 16) |
                ((hash[offset + 2] & 0xFF) << 8) |
                (hash[offset + 3] & 0xFF);

            int otp = binaryCode % (int)Math.Pow(10, digits);
            return otp.ToString().PadLeft(digits, '0');
        }

        private static byte[] Base32Decode(string input)
        {
            const string alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";
            var output = new List<byte>();
            var normalized = input.Trim().TrimEnd('=').ToUpperInvariant();
            var bitBuffer = 0;
            var bitsLeft = 0;

            foreach (var c in normalized)
            {
                var val = alphabet.IndexOf(c);
                if (val < 0)
                {
                    throw new FormatException("SecretKey Base32 không hợp lệ.");
                }

                bitBuffer <<= 5;
                bitBuffer |= val & 0x1F;
                bitsLeft += 5;

                if (bitsLeft >= 8)
                {
                    output.Add((byte)(bitBuffer >> (bitsLeft - 8)));
                    bitsLeft -= 8;
                }
            }

            return output.ToArray();
        }

        private static bool FixedTimeEquals(string left, string right)
        {
            var leftBytes = Encoding.UTF8.GetBytes(left);
            var rightBytes = Encoding.UTF8.GetBytes(right);

            return leftBytes.Length == rightBytes.Length &&
                   CryptographicOperations.FixedTimeEquals(leftBytes, rightBytes);
        }

        /// <summary>
        /// Giải quyết quyền qua cổng cho một nhân viên theo thứ tự ưu tiên:
        /// Admin (luôn được vào) → quyền tường minh theo nhân viên → quyền riêng theo tài khoản
        /// → quyền mặc định theo vai trò tài khoản. Trả null nếu chưa có quyết định
        /// (gọi tiếp tục cho phép kế thừa theo chức vụ ở điểm gọi).
        /// </summary>
        private async Task<bool?> ResolveEmployeeGateAccessAsync(Employee employee, int gateId)
        {
            var appUser = employee.AppUser != null
                ? employee.AppUser
                : await _context.AppUsers
                    .AsNoTracking()
                    .FirstOrDefaultAsync(u => u.EmployeeId == employee.EmployeeId);

            if (appUser != null && string.Equals(appUser.Role, "Admin", StringComparison.OrdinalIgnoreCase))
                return true;

            var explicitPermission = await _context.EmployeeAccessPermissions
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.EmployeeId == employee.EmployeeId && p.GateId == gateId);
            if (explicitPermission != null)
                return explicitPermission.IsAllowed;

            if (appUser != null)
            {
                var userGate = await _context.UserGateAccessPermissions
                    .AsNoTracking()
                    .FirstOrDefaultAsync(p => p.UserId == appUser.UserId && p.GateId == gateId);
                if (userGate != null)
                    return userGate.IsAllowed;

                var roleGate = await _context.RoleGateAccessPermissions
                    .AsNoTracking()
                    .FirstOrDefaultAsync(p => p.Role == appUser.Role && p.GateId == gateId);
                if (roleGate != null)
                    return roleGate.IsAllowed;
            }

            return null;
        }
    }
}
