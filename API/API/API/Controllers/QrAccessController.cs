using API.Data;
using API.DTOs;
using API.Models;
using API.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;
using System;
using System.Threading.Tasks;

namespace API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
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
        public async Task<IActionResult> ScanAccess([FromBody] QrScanAccessRequest request)
        {
            if (request == null)
            {
                return BadRequest(GateTransitApiResponse.CreateError("D? li?u g?i l�n kh�ng h?p l?."));
            }

            if (request.CameraId <= 0)
            {
                return BadRequest(GateTransitApiResponse.CreateError("CameraId kh�ng h?p l?."));
            }

            var verify = await ValidateCameraAndUserAccess(request);
            if (!verify.Ok || verify.Camera == null)
            {
                return Unauthorized(GateTransitApiResponse.CreateError(verify.Message ?? "Khong the xac thuc thao tac camera."));
            }

            var camera = verify.Camera;
            var gateId = camera.GateId!.Value;

            // 3. X�c d?nh danh t�nh (b�m s�t logic client g?i ID ho?c t? query b?ng payload)
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
                return BadRequest(GateTransitApiResponse.CreateError("Kh�ng x�c d?nh du?c danh t�nh t? d? li?u QR."));
            }

            // 4. Transaction v� ki?m tra quy?n
            await using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                bool hasAccess = false;
                string userType = targetEmployeeId.HasValue ? "Nh�n vi�n" : "Kh�ch";
                string subjectName = "";

                if (targetEmployeeId.HasValue)
                {
                    var employee = await _context.Employees.FirstOrDefaultAsync(e => e.EmployeeId == targetEmployeeId.Value);
                    if (employee == null)
                        return NotFound(GateTransitApiResponse.CreateError($"Kh�ng t�m th?y nh�n vi�n c� id = {targetEmployeeId.Value}."));
                    subjectName = employee.FullName ?? "";

                    var permission = await _context.EmployeeAccessPermissions
                        .FirstOrDefaultAsync(p => p.EmployeeId == targetEmployeeId.Value && p.GateId == gateId);

                    hasAccess = permission != null && permission.IsAllowed;
                }
                else if (targetVisitorId.HasValue)
                {
                    // Logic y�u c?u kh�ch ph?i c� Status APPROVED
                    var visitor = await _context.VisitorDetails
                        .Include(v => v.Registration)
                        .FirstOrDefaultAsync(v =>
                            v.VisitorDetailId == targetVisitorId.Value &&
                            v.IsQrActive &&
                            v.Registration != null &&
                            v.Registration.Status.ToUpper() == "APPROVED");

                    if (visitor == null)
                    {
                        return NotFound(GateTransitApiResponse.CreateError("Kh�ng t�m th?y kh�ch d� du?c x�c nh?n (ho?c QR kh�ng c�n hi?u l?c)."));
                    }
                    subjectName = visitor.FullName ?? "";

                    var permission = await _context.VisitorAccessPermissions
                        .FirstOrDefaultAsync(p => p.VisitorDetailId == targetVisitorId.Value && p.GateId == gateId);

                    hasAccess = permission != null && permission.IsAllowed;
                }

                // 5. Ghi log (luu �: d? tr?ng bi?n s?, DB v?n nh?n b�nh thu?ng)
                var logStatus = hasAccess ? "SUCCESS" : "FAILED_DENIED";
                var logNote = hasAccess
                    ? $"X�c th?c QR th�nh c�ng. {userType} du?c ph�p v�o khu v?c."
                    : $"T? ch?i. {userType} kh�ng c� quy?n truy c?p khu v?c n�y.";

                var newLog = new AccessLog
                {
                    Timestamp = DateTime.Now,
                    Direction = "IN",
                    GateId = gateId,
                    CameraId = request.CameraId,
                    EmployeeId = targetEmployeeId,
                    VisitorDetailId = targetVisitorId,
                    CapturedLicensePlate = null, // <- d? tr?ng d�ng c?u tr�c DB
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
                return StatusCode(500, GateTransitApiResponse.CreateError("C� l?i x?y ra khi x? l� d? li?u.", ex.Message));
            }
        }

        [HttpPost("verify-camera-auth")]
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

            if (!request.LoggedInUserId.HasValue || request.LoggedInUserId.Value <= 0)
            {
                return (false, "Thieu LoggedInUserId.", null);
            }

            if (string.IsNullOrWhiteSpace(request.UserPassword))
            {
                return (false, "Yeu cau nhap mat khau tai khoan de su dung camera nay.", null);
            }

            var currentUser = await _context.AppUsers.FirstOrDefaultAsync(u => u.UserId == request.LoggedInUserId.Value);
            if (currentUser == null)
            {
                return (false, "Khong tim thay tai khoan thao tac.", null);
            }

            if (!BCrypt.Net.BCrypt.Verify(request.UserPassword, currentUser.PasswordHash))
            {
                return (false, "Mat khau tai khoan khong chinh xac.", null);
            }

            return (true, null, camera);
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
