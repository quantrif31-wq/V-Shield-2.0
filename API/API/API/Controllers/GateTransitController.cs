using API.Data;
using API.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace API.Controllers
{
    [Route("api/gate-transit")]
    [ApiController]
    public class GateTransitController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public GateTransitController(ApplicationDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Nhận biển số và employeeId.
        /// - Nếu đúng biển số và đúng employee đã tồn tại => toggle IN/OUT.
        /// - Nếu chưa có đúng cặp đó:
        ///     + Nếu biển đang thuộc người khác và đang IN => fail.
        ///     + Ngược lại => thêm mới với trạng thái IN.
        /// </summary>
        [HttpPost("scan")]
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

            var employee = await _context.Employees
                .FirstOrDefaultAsync(e => e.EmployeeId == request.EmployeeId);

            if (employee == null)
            {
                return NotFound(GateTransitApiResponse.CreateError($"Không tìm thấy nhân viên có id = {request.EmployeeId}."));
            }

            await using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var samePlateVehicles = await _context.Vehicles
                    .Where(v => v.LicensePlate != null)
                    .ToListAsync();

                samePlateVehicles = samePlateVehicles
                    .Where(v => NormalizeLicensePlate(v.LicensePlate) == normalizedPlate)
                    .ToList();

                var currentVehicle = samePlateVehicles
                    .FirstOrDefault(v => v.EmployeeId == request.EmployeeId);

                if (currentVehicle != null)
                {
                    var oldStatus = NormalizeParkingStatus(currentVehicle.ParkingStatus);
                    var newStatus = oldStatus == "IN" ? "OUT" : "IN";

                    currentVehicle.ParkingStatus = newStatus;

                    _context.AccessLogs.Add(new AccessLog
                    {
                        Timestamp = DateTime.Now,
                        Direction = newStatus,
                        GateId = request.GateId,
                        CameraId = request.CameraId,
                        CapturedLicensePlate = currentVehicle.LicensePlate,
                        EmployeeId = request.EmployeeId,
                        RegistrationId = null,
                        ResultStatus = "SUCCESS",
                        IsBypass = false,
                        Note = $"Đổi trạng thái xe từ {oldStatus} sang {newStatus}"
                    });

                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    return Ok(GateTransitApiResponse.CreateSuccess(
                        $"Cập nhật trạng thái thành công: {oldStatus} -> {newStatus}.",
                        new
                        {
                            currentVehicle.VehicleId,
                            currentVehicle.LicensePlate,
                            currentVehicle.EmployeeId,
                            currentVehicle.VehicleTypeId,
                            currentVehicle.Description,
                            currentVehicle.ParkingStatus
                        }));
                }

                // 2. Nếu chưa có đúng cặp đó, kiểm tra biển có đang thuộc người khác và đang IN không
                var conflictVehicle = samePlateVehicles.FirstOrDefault(v =>
                    v.EmployeeId != request.EmployeeId &&
                    NormalizeParkingStatus(v.ParkingStatus) == "IN");

                if (conflictVehicle != null)
                {
                    _context.AccessLogs.Add(new AccessLog
                    {
                        Timestamp = DateTime.Now,
                        Direction = "IN",
                        GateId = request.GateId,
                        CameraId = request.CameraId,
                        CapturedLicensePlate = normalizedPlate,
                        EmployeeId = request.EmployeeId,
                        RegistrationId = null,
                        ResultStatus = "FAILED",
                        IsBypass = false,
                        Note = $"Biển số đang được giữ bởi nhân viên có id là {conflictVehicle.EmployeeId}"
                    });

                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    return Conflict(GateTransitApiResponse.CreateError(
                        $"Biển số đang được giữ bởi 1 nhân viên có id là {conflictVehicle.EmployeeId}."));
                }

                var newVehicle = new Vehicle
                {
                    LicensePlate = normalizedPlate,
                    EmployeeId = request.EmployeeId,
                    VehicleTypeId = request.VehicleTypeId,
                    Description = request.Description,
                    ParkingStatus = "IN"
                };

                _context.Vehicles.Add(newVehicle);

                _context.AccessLogs.Add(new AccessLog
                {
                    Timestamp = DateTime.Now,
                    Direction = "IN",
                    GateId = request.GateId,
                    CameraId = request.CameraId,
                    CapturedLicensePlate = normalizedPlate,
                    EmployeeId = request.EmployeeId,
                    RegistrationId = null,
                    ResultStatus = "SUCCESS",
                    IsBypass = false,
                    Note = "Thêm mới phương tiện và cho vào bãi với trạng thái IN"
                });

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return Ok(GateTransitApiResponse.CreateSuccess(
                    "Chưa có dữ liệu trước đó. Đã thêm mới phương tiện với trạng thái IN.",
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
            catch (Exception ex)
            {
                await transaction.RollbackAsync();

                return StatusCode(500, GateTransitApiResponse.CreateError(
                    "Có lỗi xảy ra khi xử lý dữ liệu.",
                    ex.Message));
            }
        }
        [HttpPost("scan-guest")]
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
        v.Registration.Status.ToUpper() == "APPROVED");
            }

            if (visitor == null)
                return NotFound("Không tìm thấy khách đã được xác nhận trong bảng Visitor_Details.");

            var normalizedPlate = NormalizeLicensePlate(request.LicensePlate);
            if (string.IsNullOrWhiteSpace(normalizedPlate))
                return BadRequest("Biển số không hợp lệ.");

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
                    VehicleTypeId = request.VehicleTypeId,
                    Description = request.Description,
                    ParkingStatus = "IN"
                };

                _context.Vehicles.Add(newVehicle);

                _context.AccessLogs.Add(new AccessLog
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
                    Note = "Thêm mới phương tiện cho khách với trạng thái IN."
                });

                await _context.SaveChangesAsync();

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

                _context.AccessLogs.Add(new AccessLog
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
                    Note = $"Đổi trạng thái xe của khách từ {oldStatus} sang {newStatus}."
                });

                await _context.SaveChangesAsync();

                return Ok(GateTransitApiResponse.CreateSuccess(
                    $"Cập nhật trạng thái xe của khách thành công: {oldStatus} -> {newStatus}."));
            }
        }

        [HttpGet("vehicle-by-employee/{employeeId:int}")]
        public async Task<IActionResult> GetVehiclesByEmployee(int employeeId)
        {
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

        [HttpGet("vehicle-by-plate/{licensePlate}")]
        public async Task<IActionResult> GetVehicleByPlate(string licensePlate)
        {
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

        private static string NormalizeLicensePlate(string? plate)
        {
            if (string.IsNullOrWhiteSpace(plate))
            {
                return string.Empty;
            }

            return plate.Trim()
                .ToUpper()
                .Replace(" ", string.Empty)
                .Replace("-", string.Empty);
        }

        private static string NormalizeParkingStatus(string? status)
        {
            if (string.IsNullOrWhiteSpace(status))
            {
                return "OUT";
            }

            return status.Trim().ToUpper() == "IN" ? "IN" : "OUT";
        }
    }

    public class GateTransitScanRequest
    {
        public string LicensePlate { get; set; } = string.Empty;
        public int? EmployeeId { get; set; }
        public int? VehicleTypeId { get; set; }
        public string? Description { get; set; }
        public int? GateId { get; set; }
        public int? CameraId { get; set; }
        public int? GuestId { get; set; }
        public int? VisitorDetailId { get; set; }
        public string? QrPayload { get; set; }
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
