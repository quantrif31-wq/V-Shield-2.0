using API.Data;
using API.Middleware;
using API.Models;
using API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace API.Controllers
{
    [Route("api/gate-transit")]
    [ApiController]
    [Authorize]
    [RequireOperationalTask("gate-transit")]
    public class GateTransitController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IZoneTransitService _zoneTransitService;
        private readonly IUebaService _uebaService;
        private readonly EvidenceCaptureService _evidenceCapture;
        private readonly UserOperationalScopeService _scopeService;
        private readonly StaticVisitorQrService _visitorQrService;

        public GateTransitController(ApplicationDbContext context, IZoneTransitService zoneTransitService, IUebaService uebaService, EvidenceCaptureService evidenceCapture, UserOperationalScopeService scopeService, StaticVisitorQrService visitorQrService)
        {
            _context = context;
            _zoneTransitService = zoneTransitService;
            _uebaService = uebaService;
            _evidenceCapture = evidenceCapture;
            _scopeService = scopeService;
            _visitorQrService = visitorQrService;
        }

        /// <summary>Đọc cấu hình hướng của các làn mà người dùng có quyền vận hành.</summary>
        [HttpGet("lanes")]
        public async Task<IActionResult> GetTransitLanes()
        {
            var lanes = await _context.Lanes.AsNoTracking()
                .Where(lane => lane.IsActive && lane.GateId.HasValue)
                .OrderBy(lane => lane.LaneId)
                .ToListAsync();

            var visible = new List<object>();
            foreach (var lane in lanes)
            {
                if (!await _scopeService.CanAccessAsync(User, UserOperationalScopeService.TaskGateTransit, gateId: lane.GateId, requireManage: false))
                    continue;
                visible.Add(new { lane.LaneId, lane.GateId, lane.Name, Direction = LaneDirectionToTransitDirection(lane.Direction, null) });
            }
            return Ok(GateTransitApiResponse.CreateSuccess("Lấy cấu hình làn thành công.", visible));
        }

        /// <summary>Đổi hướng IN/OUT của một làn. Nhiều làn cùng hướng là hợp lệ.</summary>
        [HttpPatch("lanes/{laneId:int}/direction")]
        public async Task<IActionResult> UpdateTransitLaneDirection(int laneId, [FromBody] GateTransitLaneDirectionRequest request)
        {
            var lane = await _context.Lanes.FirstOrDefaultAsync(item => item.LaneId == laneId && item.IsActive);
            if (lane == null || !lane.GateId.HasValue)
                return NotFound(GateTransitApiResponse.CreateError("Không tìm thấy làn kiểm soát đang hoạt động."));
            if (!await _scopeService.CanAccessAsync(User, UserOperationalScopeService.TaskGateTransit, gateId: lane.GateId, requireManage: true))
                return Forbid();

            var direction = NormalizeTransitDirection(request?.Direction);
            if (direction == null)
                return BadRequest(GateTransitApiResponse.CreateError("Hướng làn chỉ nhận IN hoặc OUT."));

            lane.Direction = direction == "IN" ? "Entry" : "Exit";
            await _context.SaveChangesAsync();
            return Ok(GateTransitApiResponse.CreateSuccess("Đã lưu hướng làn.", new { lane.LaneId, lane.GateId, Direction = direction }));
        }

        /// <summary>
        /// Xác nhận thông hành đa nguồn (QR + biển số hoặc FaceID + biển số).
        /// Hướng di chuyển được lấy từ làn/cổng, không tự đảo theo số lần quét.
        /// Chỉ giao dịch thành công mới tạo ZoneTransit và cập nhật Attendance.
        /// </summary>
        [HttpPost("scan")]
        [EnableRateLimiting("qr-ops")]
        public async Task<IActionResult> ScanVehicle([FromBody] GateTransitScanRequest request)
        {
            if (request == null)
            {
                return BadRequest(GateTransitApiResponse.CreateError("Dữ liệu gửi lên không hợp lệ."));
            }

            var normalizedPlate = NormalizeLicensePlate(request.LicensePlate);

            if (string.IsNullOrWhiteSpace(normalizedPlate))
            {
                return BadRequest(GateTransitApiResponse.CreateError("Biển số không được để trống."));
            }

            if (request.EmployeeId <= 0)
            {
                return BadRequest(GateTransitApiResponse.CreateError("EmployeeId không hợp lệ."));
            }

            Lane? lane = null;
            if (request.LaneId.HasValue)
            {
                lane = await _context.Lanes
                    .AsNoTracking()
                    .FirstOrDefaultAsync(item => item.LaneId == request.LaneId.Value && item.IsActive);
                if (lane == null || !lane.GateId.HasValue)
                {
                    return BadRequest(GateTransitApiResponse.CreateError("Làn kiểm soát không hợp lệ hoặc chưa liên kết với cổng."));
                }

                if (request.GateId.HasValue && request.GateId.Value != lane.GateId.Value)
                {
                    return BadRequest(GateTransitApiResponse.CreateError("Làn kiểm soát không thuộc cổng đã chọn."));
                }

                request.GateId = lane.GateId.Value;
            }

            if (!request.GateId.HasValue)
            {
                return BadRequest(GateTransitApiResponse.CreateError("Phải chọn cổng/làn trước khi xác nhận thông hành."));
            }

            if (!await _scopeService.CanAccessAsync(User, UserOperationalScopeService.TaskGateTransit, gateId: request.GateId, requireManage: true))
            {
                return Forbid();
            }

            var transitSessionId = NormalizeTransitSessionId(request.TransitSessionId);
            var transitSessionMarker = string.IsNullOrWhiteSpace(transitSessionId) ? string.Empty : $"[TRANSIT:{transitSessionId}]";
            if (!string.IsNullOrEmpty(transitSessionMarker))
            {
                var existingLog = await _context.AccessLogs.AsNoTracking()
                    .Where(log => log.Note != null && log.Note.Contains(transitSessionMarker))
                    .OrderByDescending(log => log.LogId)
                    .FirstOrDefaultAsync();
                if (existingLog != null)
                    return Ok(GateTransitApiResponse.CreateSuccess("Phiên thông hành này đã được xử lý trước đó.", new { existingLog.LogId, existingLog.ResultStatus, existingLog.CapturedLicensePlate }));
            }

            var requestedDirection = NormalizeTransitDirection(request.Direction);
            if (!string.IsNullOrWhiteSpace(request.Direction) && requestedDirection == null)
            {
                return BadRequest(GateTransitApiResponse.CreateError("Hướng di chuyển chỉ nhận IN hoặc OUT."));
            }

            if (lane != null)
            {
                var laneDirection = LaneDirectionToTransitDirection(lane.Direction, requestedDirection);
                if (laneDirection == null)
                {
                    return BadRequest(GateTransitApiResponse.CreateError("Hướng di chuyển không phù hợp với cấu hình làn."));
                }
                requestedDirection = laneDirection;
            }

            var employee = await _context.Employees
                .FirstOrDefaultAsync(e => e.EmployeeId == request.EmployeeId);

            if (employee == null)
            {
                return NotFound(GateTransitApiResponse.CreateError($"Không tìm thấy nhân viên có id = {request.EmployeeId}."));
            }

            await using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                // Vehicle là trạng thái của phiên gửi hiện tại, không phải danh sách biển số
                // được đăng ký cứng cho một nhân viên. EmployeeId được gán lại mỗi khi xe
                // bắt đầu một lượt IN mới và chỉ người đó (hoặc người được ủy quyền) được OUT.
                var samePlateVehicles = await _context.Vehicles
                    .Where(v => v.LicensePlate != null)
                    .ToListAsync();

                samePlateVehicles = samePlateVehicles
                    .Where(v => NormalizeLicensePlate(v.LicensePlate) == normalizedPlate)
                    .ToList();

                var vehicle = samePlateVehicles.FirstOrDefault();
                var parkingStatus = NormalizeParkingStatus(vehicle?.ParkingStatus);

                var direction = requestedDirection
                    ?? (vehicle != null && parkingStatus == "IN" ? "OUT" : "IN");

                async Task CaptureEvidenceAsync(AccessLog log)
                {
                    var sourceRef = $"access-log/{log.LogId}";
                    log.CapturedSnapshotUrl = await _evidenceCapture.CaptureBase64Async(
                        request.PlateSnapshotBase64, "snapshot", sourceRef, createdByUserId: request.EmployeeId);
                    log.CapturedPlateCropUrl = await _evidenceCapture.CaptureBase64Async(
                        request.PlateCropBase64, "plate-crop", sourceRef, createdByUserId: request.EmployeeId);
                    log.CapturedFaceCropUrl = await _evidenceCapture.CaptureBase64Async(
                        request.FaceSnapshotBase64, "face-crop", sourceRef, createdByUserId: request.EmployeeId);
                    log.CapturedQrSnapshotUrl = await _evidenceCapture.CaptureBase64Async(
                        request.QrSnapshotBase64, "qr-snapshot", sourceRef, createdByUserId: request.EmployeeId);
                    log.CapturedSnapshotUrl ??= log.CapturedFaceCropUrl ?? log.CapturedQrSnapshotUrl;
                    await _context.SaveChangesAsync();
                }

                AccessLog CreateLog(string plate, string resultStatus, string note) => new()
                {
                    Timestamp = DateTime.Now,
                    Direction = direction,
                    GateId = request.GateId,
                    CameraId = request.CameraId,
                    CapturedLicensePlate = plate,
                    EmployeeId = request.EmployeeId,
                    RegistrationId = null,
                    ResultStatus = resultStatus,
                    IsBypass = false,
                    Note = $"[{NormalizeCredentialType(request.CredentialType)}]{transitSessionMarker} {note}",
                    LaneNameSnapshot = lane?.Name
                };

                async Task CompleteSuccessfulTransitAsync(AccessLog log)
                {
                    // Parking admission is the primary transaction.  Zone/attendance
                    // derivation must never roll back an already valid IN/OUT record:
                    // a transient schedule or zone configuration fault used to surface
                    // as HTTP 500 in the manual kiosk after the vehicle had been saved.
                    await transaction.CommitAsync();
                    try
                    {
                        await _zoneTransitService.ProcessAccessLogAsync(log.LogId);
                    }
                    catch
                    {
                        // Zone and attendance reconciliation is recoverable from the
                        // persisted access log; keep the gate/parking decision intact.
                    }
                    try
                    {
                        await _uebaService.AnalyzeAccessLogAsync(log);
                    }
                    catch
                    {
                        // UEBA không được làm thất bại giao dịch thông hành đã ghi nhận.
                    }
                }

                async Task<IActionResult> DenyAsync(string note, string message)
                {
                    var deniedLog = CreateLog(normalizedPlate, "FAILED", note);
                    _context.AccessLogs.Add(deniedLog);
                    await _context.SaveChangesAsync();
                    await CaptureEvidenceAsync(deniedLog);
                    await transaction.CommitAsync();
                    return Conflict(GateTransitApiResponse.CreateError(message));
                }

                if (vehicle == null)
                {
                    if (direction == "OUT")
                        return await DenyAsync("Từ chối OUT: phương tiện chưa có lượt vào hợp lệ.", "Không tìm thấy lượt vào của phương tiện; không ghi nhận chấm công.");

                    vehicle = new Vehicle
                    {
                        LicensePlate = normalizedPlate,
                        EmployeeId = request.EmployeeId,
                        VehicleTypeId = request.VehicleTypeId,
                        Description = request.Description,
                        ParkingStatus = "IN"
                    };
                    _context.Vehicles.Add(vehicle);
                }
                else if (parkingStatus == "IN")
                {
                    if (direction != "OUT")
                        return await DenyAsync("Từ chối IN: phương tiện đang có phiên gửi mở.", "Xe đang ở trong bãi; chỉ có thể xác nhận lượt ra.");

                    var isParkedByCurrentEmployee = vehicle.EmployeeId == request.EmployeeId;
                    var isApprovedDelegate = vehicle.EmployeeId.HasValue && await _context.VehicleDelegations
                        .AsNoTracking()
                        .AnyAsync(delegation => delegation.VehicleId == vehicle.VehicleId
                            && delegation.FromEmployeeId == vehicle.EmployeeId.Value
                            && delegation.ToEmployeeId == request.EmployeeId
                            && delegation.Status == DelegationStatuses.Approved);

                    if (!isParkedByCurrentEmployee && !isApprovedDelegate)
                    {
                        return await DenyAsync(
                            $"Từ chối OUT: xe đang được gửi bởi nhân viên {vehicle.EmployeeId}; không có ủy quyền hợp lệ cho nhân viên {request.EmployeeId}.",
                            "Biển số đang thuộc một phiên gửi của người khác; không ghi nhận chấm công.");
                    }

                    vehicle.ParkingStatus = "OUT";
                }
                else
                {
                    if (direction != "IN")
                        return await DenyAsync("Từ chối OUT: phương tiện chưa có phiên gửi mở.", "Không tìm thấy lượt vào đang mở của phương tiện; không ghi nhận chấm công.");

                    // Xe đã kết thúc lượt cũ: ai quét IN lần này là người giữ phiên mới.
                    vehicle.EmployeeId = request.EmployeeId;
                    vehicle.VisitorDetailId = null;
                    vehicle.ParkingStatus = "IN";
                    if (request.VehicleTypeId.HasValue) vehicle.VehicleTypeId = request.VehicleTypeId;
                    if (!string.IsNullOrWhiteSpace(request.Description)) vehicle.Description = request.Description;
                }

                var accessLog = CreateLog(
                    vehicle.LicensePlate,
                    "SUCCESS",
                    direction == "IN"
                        ? $"Xác nhận IN thành công; nhân viên {request.EmployeeId} mở phiên gửi xe."
                        : $"Xác nhận OUT thành công; kết thúc phiên gửi xe của nhân viên {vehicle.EmployeeId}.");
                _context.AccessLogs.Add(accessLog);
                await _context.SaveChangesAsync();
                await CaptureEvidenceAsync(accessLog);
                await CompleteSuccessfulTransitAsync(accessLog);

                return Ok(GateTransitApiResponse.CreateSuccess(
                    $"Đã ghi nhận {direction} và cập nhật bảng chấm công.",
                    new
                    {
                        accessLog.LogId,
                        Direction = direction,
                        vehicle.VehicleId,
                        vehicle.LicensePlate,
                        vehicle.EmployeeId,
                        vehicle.VehicleTypeId,
                        vehicle.Description,
                        vehicle.ParkingStatus
                    }));
            }
            catch (Exception ex)
            {
                try { await transaction.RollbackAsync(); } catch { }

                return StatusCode(500, GateTransitApiResponse.CreateError(
                    "Có lỗi xảy ra khi xử lý dữ liệu.",
                    ex.Message));
            }
        }
        [HttpPost("scan-guest")]
        [EnableRateLimiting("qr-ops")]
        public async Task<IActionResult> ScanGuest([FromBody] GateTransitScanRequest request)
        {
            if (request == null)
                return BadRequest("Dữ liệu không hợp lệ.");

            if (request.VisitorDetailId == null && string.IsNullOrWhiteSpace(request.QrPayload))
                return BadRequest("Phải có VisitorDetailId hoặc QrPayload.");

            VisitorDetail? visitor = null;

            if (request.VisitorDetailId.HasValue)
            {
                 visitor = await _context.VisitorDetails
    .Include(v => v.Registration)
    .FirstOrDefaultAsync(v =>
        v.VisitorDetailId == request.VisitorDetailId &&
        v.IsQrActive &&
        v.Registration != null &&
        v.Registration.Status != null &&
        v.Registration.Status.ToUpper() == "APPROVED");
            }

            if (visitor == null)
                return NotFound("Không tìm thấy khách đã được xác nhận trong bảng Visitor_Details.");

            if (request.GateId.HasValue &&
                !await _scopeService.CanAccessAsync(User, UserOperationalScopeService.TaskGateTransit, gateId: request.GateId, requireManage: true))
            {
                return Forbid();
            }

            var normalizedPlate = NormalizeLicensePlate(request.LicensePlate);
            if (string.IsNullOrWhiteSpace(normalizedPlate))
                return BadRequest("Biển số không hợp lệ.");

            var guestSessionId = NormalizeTransitSessionId(request.TransitSessionId);
            var guestSessionMarker = string.IsNullOrWhiteSpace(guestSessionId) ? string.Empty : $"[TRANSIT:{guestSessionId}]";
            if (!string.IsNullOrEmpty(guestSessionMarker))
            {
                var existingLog = await _context.AccessLogs.AsNoTracking()
                    .Where(log => log.Note != null && log.Note.Contains(guestSessionMarker))
                    .OrderByDescending(log => log.LogId).FirstOrDefaultAsync();
                if (existingLog != null)
                    return Ok(GateTransitApiResponse.CreateSuccess("Phiên thông hành này đã được xử lý trước đó.", new { existingLog.LogId, existingLog.ResultStatus, existingLog.CapturedLicensePlate }));
            }

            var vehicle = await _context.Vehicles
    .FirstOrDefaultAsync(v =>
        v.LicensePlate != null &&
        v.LicensePlate.Trim()
            .ToUpper()
            .Replace(" ", "")
            .Replace("-", "") == normalizedPlate);

            if (vehicle == null)
            {

                var newVehicle = new Vehicle
                {
                    LicensePlate = normalizedPlate,
                    EmployeeId = null,
                    VisitorDetailId = visitor.VisitorDetailId,
                    VehicleTypeId = request.VehicleTypeId,
                    Description = request.Description,
                    ParkingStatus = "IN"
                };

                _context.Vehicles.Add(newVehicle);

                var uebaLog = new AccessLog
                {
                    Timestamp = DateTime.Now,
                    Direction = "IN",
                    GateId = request.GateId,
                    CameraId = request.CameraId,
                    CapturedLicensePlate = normalizedPlate,
                    RegistrationId = visitor.RegistrationId,
                    VisitorDetailId = visitor.VisitorDetailId,
                    EmployeeId = null,
                    ResultStatus = "SUCCESS",
                    IsBypass = false,
                    Note = $"{guestSessionMarker} Thêm mới phương tiện cho khách với trạng thái IN."
                };
                _context.AccessLogs.Add(uebaLog);
                await _context.SaveChangesAsync();

                var refNew = $"access-log/{uebaLog.LogId}";
                uebaLog.CapturedSnapshotUrl = await _evidenceCapture.CaptureBase64Async(request.PlateSnapshotBase64, "snapshot", refNew);
                uebaLog.CapturedPlateCropUrl = await _evidenceCapture.CaptureBase64Async(request.PlateCropBase64, "plate-crop", refNew);
                await _context.SaveChangesAsync();

                _ = _uebaService.AnalyzeAccessLogAsync(uebaLog);

                return Ok(GateTransitApiResponse.CreateSuccess(
                    "Phương tiện của khách đã được thêm mới với trạng thái IN.",
                    new
                    {
                        newVehicle.VehicleId,
                        newVehicle.LicensePlate,
                        newVehicle.EmployeeId,
                        newVehicle.VehicleTypeId,
                        newVehicle.Description,
                        newVehicle.ParkingStatus
                    }));
            }
            else
            {
                var oldStatus = vehicle.ParkingStatus;
                var newStatus = oldStatus == "IN" ? "OUT" : "IN";
                vehicle.ParkingStatus = newStatus;
                if (vehicle.VisitorDetailId == null)
                {
                    vehicle.VisitorDetailId = visitor.VisitorDetailId;
                }

                var uebaLog = new AccessLog
                {
                    Timestamp = DateTime.Now,
                    Direction = newStatus,
                    GateId = request.GateId,
                    CameraId = request.CameraId,
                    CapturedLicensePlate = normalizedPlate,
                    RegistrationId = visitor.RegistrationId,
                    VisitorDetailId = visitor.VisitorDetailId,
                    EmployeeId = null,
                    ResultStatus = "SUCCESS",
                    IsBypass = false,
                    Note = $"{guestSessionMarker} Đổi trạng thái xe của khách từ {oldStatus} sang {newStatus}."
                };
                _context.AccessLogs.Add(uebaLog);
                await _context.SaveChangesAsync();

                var refExisting = $"access-log/{uebaLog.LogId}";
                uebaLog.CapturedSnapshotUrl = await _evidenceCapture.CaptureBase64Async(request.PlateSnapshotBase64, "snapshot", refExisting);
                uebaLog.CapturedPlateCropUrl = await _evidenceCapture.CaptureBase64Async(request.PlateCropBase64, "plate-crop", refExisting);
                await _context.SaveChangesAsync();

                _ = _uebaService.AnalyzeAccessLogAsync(uebaLog);

                return Ok(GateTransitApiResponse.CreateSuccess(
                    $"Cập nhật trạng thái xe của khách thành công: {oldStatus} -> {newStatus}."));
            }
        }

        [HttpGet("vehicle-by-employee/{employeeId:int}")]
        public async Task<IActionResult> GetVehiclesByEmployee(int employeeId)
        {
            if (!await _scopeService.CanAccessAsync(User, UserOperationalScopeService.TaskParking, requireManage: false))
                return Forbid();

            var employeeExists = await _context.Employees
                .AnyAsync(e => e.EmployeeId == employeeId);

            if (!employeeExists)
            {
                return NotFound(GateTransitApiResponse.CreateError($"Không tìm thấy nhân viên có id = {employeeId}."));
            }

            var vehicles = await _context.Vehicles
                .Where(v => v.EmployeeId == employeeId)
                .Select(v => new
                {
                    v.VehicleId,
                    v.LicensePlate,
                    v.VehicleTypeId,
                    v.EmployeeId,
                    v.Description,
                    v.ParkingStatus
                })
                .ToListAsync();

            return Ok(GateTransitApiResponse.CreateSuccess("Lấy danh sách xe thành công.", vehicles));
        }

        /// <summary>
        /// Danh sách cổng cho màn hình thủ công (Bảo vệ). Dùng task parking.
        /// </summary>
        [HttpGet("gates")]
        public async Task<IActionResult> GetManualGates()
        {
            if (!await _scopeService.CanAccessAsync(User, UserOperationalScopeService.TaskParking, requireManage: false))
                return Forbid();

            var gates = await _context.Gates
                .AsNoTracking()
                .OrderBy(g => g.GateName)
                .Select(g => new
                {
                    g.GateId,
                    g.GateName,
                    g.Location
                })
                .ToListAsync();

            return Ok(GateTransitApiResponse.CreateSuccess("Lấy danh sách cổng thành công.", gates));
        }

        /// <summary>
        /// Nhận dạng đối tượng thủ công từ mã (QR payload EMP/VIS hoặc mã số).
        /// Trả về loại đối tượng, thông tin cá nhân và danh sách xe đang gửi.
        /// </summary>
        [HttpGet("manual-subject/{code}")]
        public async Task<IActionResult> GetManualSubject(string code)
        {
            // This resolver is shared by the manual parking desk and the
            // QR-monitor fallback desk. Both only read the person identity;
            // each subsequent action still applies its own task permission.
            var canUseParking = await _scopeService.CanAccessAsync(User, UserOperationalScopeService.TaskParking, requireManage: false);
            var canUseManualGate = await _scopeService.CanAccessAsync(User, UserOperationalScopeService.TaskQrAccess, requireManage: false);
            if (!canUseParking && !canUseManualGate)
                return Forbid();

            if (string.IsNullOrWhiteSpace(code))
                return BadRequest(GateTransitApiResponse.CreateError("Mã nhận dạng không được để trống."));

            var raw = code.Trim();

            // 1. QR payload của nhân viên: EMP:<employeeId>|TS:<counter>|OTP:<otp>
            if (raw.StartsWith("EMP:", StringComparison.OrdinalIgnoreCase))
            {
                var parts = raw.Split('|', StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length == 0 || !parts[0].StartsWith("EMP:", StringComparison.OrdinalIgnoreCase))
                    return BadRequest(GateTransitApiResponse.CreateError("Mã nhân viên không hợp lệ."));

                var idText = parts[0].Split(':')[^1].Trim();
                if (!int.TryParse(idText, out var employeeId))
                    return BadRequest(GateTransitApiResponse.CreateError("Mã nhân viên không hợp lệ."));

                return await ResolveManualSubjectEmployeeAsync(employeeId);
            }

            // 2. QR payload của khách: VIS:<visitorId>|REG:<registrationId>|TS:<counter>|OTP:<otp>
            if (raw.StartsWith("VIS:", StringComparison.OrdinalIgnoreCase))
            {
                if (!_visitorQrService.TryParsePayload(raw, out var parsed, out var parseMessage) || parsed == null)
                    return BadRequest(GateTransitApiResponse.CreateError(parseMessage ?? "Mã khách không hợp lệ."));

                return await ResolveManualSubjectVisitorAsync(parsed.VisitorId);
            }

            // 3. Mã số thuần: thử nhân viên trước, sau đó thử khách theo VisitorDetailId hoặc IdCardNumber
            if (int.TryParse(raw, out var numericId))
            {
                var employeeExists = await _context.Employees.AsNoTracking()
                    .AnyAsync(e => e.EmployeeId == numericId);
                if (employeeExists)
                    return await ResolveManualSubjectEmployeeAsync(numericId);

                var visitorById = await _context.VisitorDetails.AsNoTracking()
                    .FirstOrDefaultAsync(v => v.VisitorDetailId == numericId);
                if (visitorById != null)
                    return await ResolveManualSubjectVisitorAsync(numericId);
            }

            // 4. Văn bản: khớp theo số căn cước của khách
            var normalizedIdCard = raw.ToUpperInvariant()
                .Replace(" ", "").Replace("-", "").Replace("_", "");
            if (normalizedIdCard.Length >= 4)
            {
                var visitorByCard = await _context.VisitorDetails.AsNoTracking()
                    .FirstOrDefaultAsync(v => v.IdCardNumber != null &&
                        v.IdCardNumber.ToUpper().Replace(" ", "").Replace("-", "").Replace("_", "") == normalizedIdCard);
                if (visitorByCard != null)
                    return await ResolveManualSubjectVisitorAsync(visitorByCard.VisitorDetailId);
            }

            return NotFound(GateTransitApiResponse.CreateError("Không tìm thấy đối tượng tương ứng với mã đã nhập."));
        }

        private async Task<IActionResult> ResolveManualSubjectEmployeeAsync(int employeeId)
        {
            var employee = await _context.Employees
                .AsNoTracking()
                .Include(e => e.Department)
                .Include(e => e.Position)
                .FirstOrDefaultAsync(e => e.EmployeeId == employeeId);

            if (employee == null)
                return NotFound(GateTransitApiResponse.CreateError($"Không tìm thấy nhân viên có id = {employeeId}."));

            var vehicles = await _context.Vehicles
                .AsNoTracking()
                .Where(v => v.EmployeeId == employeeId && v.ParkingStatus == "IN")
                .Select(v => new
                {
                    v.VehicleId,
                    v.LicensePlate,
                    v.VehicleTypeId,
                    v.Description,
                    v.ParkingStatus
                })
                .ToListAsync();

            return Ok(GateTransitApiResponse.CreateSuccess("Tìm thấy nhân viên.", new
            {
                SubjectType = "employee",
                SubjectId = employee.EmployeeId,
                FullName = employee.FullName,
                Phone = employee.Phone,
                Email = employee.Email,
                DepartmentName = employee.Department != null ? employee.Department.Name : null,
                PositionName = employee.Position != null ? employee.Position.Name : null,
                FaceImageUrl = employee.FaceImageUrl,
                ParkedVehicles = vehicles
            }));
        }

        private async Task<IActionResult> ResolveManualSubjectVisitorAsync(int visitorDetailId)
        {
            var visitor = await _context.VisitorDetails
                .AsNoTracking()
                .Include(v => v.Registration)
                    .ThenInclude(r => r != null ? r.Guest : null)
                .Include(v => v.Registration)
                    .ThenInclude(r => r != null ? r.HostEmployee : null)
                .FirstOrDefaultAsync(v => v.VisitorDetailId == visitorDetailId);

            if (visitor == null)
                return NotFound(GateTransitApiResponse.CreateError($"Không tìm thấy khách có mã = {visitorDetailId}."));

            var vehicles = await _context.Vehicles
                .AsNoTracking()
                .Where(v => v.VisitorDetailId == visitorDetailId && v.ParkingStatus == "IN")
                .Select(v => new
                {
                    v.VehicleId,
                    v.LicensePlate,
                    v.VehicleTypeId,
                    v.Description,
                    v.ParkingStatus
                })
                .ToListAsync();

            return Ok(GateTransitApiResponse.CreateSuccess("Tìm thấy khách.", new
            {
                SubjectType = "visitor",
                SubjectId = visitor.VisitorDetailId,
                FullName = visitor.FullName,
                IdCardNumber = visitor.IdCardNumber,
                GuestId = visitor.Registration != null ? visitor.Registration.GuestId : (int?)null,
                GuestPhone = visitor.Registration != null && visitor.Registration.Guest != null ? visitor.Registration.Guest.Phone : null,
                HostEmployeeName = visitor.Registration != null && visitor.Registration.HostEmployee != null ? visitor.Registration.HostEmployee.FullName : null,
                RegistrationStatus = visitor.Registration != null ? visitor.Registration.Status : null,
                FaceImageUrl = visitor.ExpectedFaceImage,
                ParkedVehicles = vehicles
            }));
        }

        [HttpGet("vehicle-by-plate/{licensePlate}")]
        public async Task<IActionResult> GetVehicleByPlate(string licensePlate)
        {
            if (!await _scopeService.CanAccessAsync(User, UserOperationalScopeService.TaskParking, requireManage: false))
                return Forbid();

            var normalizedPlate = NormalizeLicensePlate(licensePlate);

            if (string.IsNullOrWhiteSpace(normalizedPlate))
            {
                return BadRequest(GateTransitApiResponse.CreateError("Biển số không hợp lệ."));
            }

            var vehicles = await _context.Vehicles
                .Where(v => v.LicensePlate != null)
                .ToListAsync();

            var result = vehicles
                .Where(v => NormalizeLicensePlate(v.LicensePlate) == normalizedPlate)
                .Select(v => new
                {
                    v.VehicleId,
                    v.LicensePlate,
                    v.VehicleTypeId,
                    v.EmployeeId,
                    v.Description,
                    v.ParkingStatus
                })
                .ToList();

            if (!result.Any())
            {
                return NotFound(GateTransitApiResponse.CreateError("Không tìm thấy biển số này."));
            }

            return Ok(GateTransitApiResponse.CreateSuccess("Lấy thông tin xe thành công.", result));
        }

        [HttpGet("logs-by-plate/{licensePlate}")]
        public async Task<IActionResult> GetLogsByPlate(string licensePlate)
        {
            if (!await _scopeService.CanAccessAsync(User, UserOperationalScopeService.TaskGateTransit, requireManage: false))
                return Forbid();

            var normalizedPlate = NormalizeLicensePlate(licensePlate);

            if (string.IsNullOrWhiteSpace(normalizedPlate))
            {
                return BadRequest(GateTransitApiResponse.CreateError("Biển số không hợp lệ."));
            }

            var logs = await _context.AccessLogs
                .Where(x => x.CapturedLicensePlate != null)
                .OrderByDescending(x => x.Timestamp)
                .ToListAsync();

            var result = logs
                .Where(x => NormalizeLicensePlate(x.CapturedLicensePlate) == normalizedPlate)
                .Select(x => new
                {
                    x.LogId,
                    x.Timestamp,
                    x.Direction,
                    x.GateId,
                    x.CameraId,
                    x.CapturedLicensePlate,
                    x.EmployeeId,
                    x.ResultStatus,
                    x.IsBypass,
                    x.Note
                })
                .ToList();

            return Ok(GateTransitApiResponse.CreateSuccess("Lấy lịch sử theo biển số thành công.", result));
        }

        [HttpGet("logs-by-employee/{employeeId:int}")]
        public async Task<IActionResult> GetLogsByEmployee(int employeeId)
        {
            if (!await _scopeService.CanAccessAsync(User, UserOperationalScopeService.TaskGateTransit, requireManage: false))
                return Forbid();

            var employeeExists = await _context.Employees
                .AnyAsync(e => e.EmployeeId == employeeId);

            if (!employeeExists)
            {
                return NotFound(GateTransitApiResponse.CreateError($"Không tìm thấy nhân viên có id = {employeeId}."));
            }

            var result = await _context.AccessLogs
                .Where(x => x.EmployeeId == employeeId)
                .OrderByDescending(x => x.Timestamp)
                .Select(x => new
                {
                    x.LogId,
                    x.Timestamp,
                    x.Direction,
                    x.GateId,
                    x.CameraId,
                    x.CapturedLicensePlate,
                    x.EmployeeId,
                    x.ResultStatus,
                    x.IsBypass,
                    x.Note
                })
                .ToListAsync();

            return Ok(GateTransitApiResponse.CreateSuccess("Lấy lịch sử theo nhân viên thành công.", result));
        }

        private static string NormalizeLicensePlate(string? plate) =>
            LicensePlateHelper.NormalizeForMatch(plate);

        private static string NormalizeParkingStatus(string? status)
        {
            if (string.IsNullOrWhiteSpace(status))
            {
                return "OUT";
            }

            return status.Trim().ToUpper() == "IN" ? "IN" : "OUT";
        }

        private static string? NormalizeTransitDirection(string? direction)
        {
            if (string.IsNullOrWhiteSpace(direction)) return null;
            return direction.Trim().ToUpperInvariant() switch
            {
                "IN" or "ENTRY" => "IN",
                "OUT" or "EXIT" => "OUT",
                _ => null
            };
        }

        private static string? LaneDirectionToTransitDirection(string? laneDirection, string? requestedDirection)
        {
            if (string.Equals(laneDirection, "Entry", StringComparison.OrdinalIgnoreCase))
                return requestedDirection is null or "IN" ? "IN" : null;
            if (string.Equals(laneDirection, "Exit", StringComparison.OrdinalIgnoreCase))
                return requestedDirection is null or "OUT" ? "OUT" : null;
            if (string.Equals(laneDirection, "Bidirectional", StringComparison.OrdinalIgnoreCase))
                return requestedDirection;
            return null;
        }

        private static string NormalizeTransitSessionId(string? sessionId)
        {
            var value = (sessionId ?? string.Empty).Trim();
            return value.Length <= 100 && value.All(c => char.IsLetterOrDigit(c) || c is '-' or '_') ? value : string.Empty;
        }

        private static string NormalizeCredentialType(string? credentialType) =>
            credentialType?.Trim().ToUpperInvariant() switch
            {
                "FACE" or "FACEID" or "FACE_AND_PLATE" => "FACEID+PLATE",
                _ => "QR+PLATE"
            };
    }

    public class GateTransitScanRequest
    {
        public string LicensePlate { get; set; } = string.Empty;
        public int? EmployeeId { get; set; }
        public int? VehicleTypeId { get; set; }
        public string? Description { get; set; }
        public int? GateId { get; set; }
        public int? LaneId { get; set; }
        public int? CameraId { get; set; }
        public string? Direction { get; set; }
        public string? CredentialType { get; set; }
        public int? GuestId { get; set; }
        public int? VisitorDetailId { get; set; }
        public string? QrPayload { get; set; }
        public string? PlateSnapshotBase64 { get; set; }
        public string? PlateCropBase64 { get; set; }
        public string? FaceSnapshotBase64 { get; set; }
        public string? QrSnapshotBase64 { get; set; }
        public string? TransitSessionId { get; set; }
    }

    public class GateTransitLaneDirectionRequest
    {
        public string? Direction { get; set; }
    }

    public class GateTransitApiResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public object? Data { get; set; }

        public static GateTransitApiResponse CreateSuccess(string message, object? data = null)
        {
            return new GateTransitApiResponse
            {
                Success = true,
                Message = message,
                Data = data
            };
        }

        public static GateTransitApiResponse CreateError(string message, object? data = null)
        {
            return new GateTransitApiResponse
            {
                Success = false,
                Message = message,
                Data = data
            };
        }
    }

}
