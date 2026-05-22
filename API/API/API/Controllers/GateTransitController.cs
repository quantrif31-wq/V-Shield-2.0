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
        /// Nh?n bi?n s? + employeeId.
        /// - N?u dúng bi?n s? + dúng employee dã t?n t?i => toggle IN/OUT.
        /// - N?u chua có dúng c?p dó:
        ///     + N?u bi?n dang thu?c ngu?i khác và dang IN => fail.
        ///     + Ngu?c l?i => thêm m?i v?i tr?ng thái IN.
        /// </summary>
        [HttpPost("scan")]
        public async Task<IActionResult> ScanVehicle([FromBody] GateTransitScanRequest request)
        {
            if (request == null)
            {
                return BadRequest(GateTransitApiResponse.CreateError("D? li?u g?i lên không h?p l?."));
            }

            var normalizedPlate = NormalizeLicensePlate(request.LicensePlate);

            if (string.IsNullOrWhiteSpace(normalizedPlate))
            {
                return BadRequest(GateTransitApiResponse.CreateError("Bi?n s? không du?c d? tr?ng."));
            }

            if (request.EmployeeId <= 0)
            {
                return BadRequest(GateTransitApiResponse.CreateError("EmployeeId không h?p l?."));
            }

            var employee = await _context.Employees
                .FirstOrDefaultAsync(e => e.EmployeeId == request.EmployeeId);

            if (employee == null)
            {
                return NotFound(GateTransitApiResponse.CreateError($"Không tìm th?y nhân viên có id = {request.EmployeeId}."));
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
                        Note = $"Ð?i tr?ng thái xe t? {oldStatus} sang {newStatus}"
                    });

                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    return Ok(GateTransitApiResponse.CreateSuccess(
                        $"C?p nh?t tr?ng thái thành công: {oldStatus} -> {newStatus}.",
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

                // 2. N?u chua có dúng c?p dó, ki?m tra bi?n có dang thu?c ngu?i khác và dang IN không
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
                        Note = $"Bi?n s? dang du?c g?i b?i nhân viên có id là {conflictVehicle.EmployeeId}"
                    });

                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    return Conflict(GateTransitApiResponse.CreateError(
                        $"Bi?n s? dang du?c g?i b?i 1 nhân viên có id là {conflictVehicle.EmployeeId}."));
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
                    Note = "Thêm m?i phuong ti?n và cho vào bãi v?i tr?ng thái IN"
                });

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return Ok(GateTransitApiResponse.CreateSuccess(
                    "Chua có d? li?u tru?c dó. Ðã thêm m?i phuong ti?n v?i tr?ng thái IN.",
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
                    "Có l?i x?y ra khi x? lý d? li?u.",
                    ex.Message));
            }
        }
        [HttpPost("scan-guest")]
        public async Task<IActionResult> ScanGuest([FromBody] GateTransitScanRequest request)
        {
            if (request == null)
                return BadRequest("D? li?u không h?p l?.");

            if (request.VisitorDetailId == null && string.IsNullOrWhiteSpace(request.QrPayload))
                return BadRequest("Ph?i có VisitorDetailId ho?c QrPayload.");

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
                return NotFound("Không tìm th?y khách dã du?c xác nh?n trong b?ng Visitor_Details.");

            var normalizedPlate = NormalizeLicensePlate(request.LicensePlate);
            if (string.IsNullOrWhiteSpace(normalizedPlate))
                return BadRequest("Bi?n s? không h?p l?.");

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
                    Note = "Thêm m?i phuong ti?n cho khách v?i tr?ng thái IN."
                });

                await _context.SaveChangesAsync();

                return Ok(GateTransitApiResponse.CreateSuccess(
                    "Phuong ti?n c?a khách dã du?c thêm m?i v?i tr?ng thái IN.",
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
                    Note = $"Ð?i tr?ng thái xe c?a khách t? {oldStatus} sang {newStatus}."
                });

                await _context.SaveChangesAsync();

                return Ok(GateTransitApiResponse.CreateSuccess(
                    $"C?p nh?t tr?ng thái xe c?a khách thành công: {oldStatus} -> {newStatus}."));
            }
        }

        [HttpGet("vehicle-by-employee/{employeeId:int}")]
        public async Task<IActionResult> GetVehiclesByEmployee(int employeeId)
        {
            var employeeExists = await _context.Employees
                .AnyAsync(e => e.EmployeeId == employeeId);

            if (!employeeExists)
            {
                return NotFound(GateTransitApiResponse.CreateError($"Không tìm th?y nhân viên có id = {employeeId}."));
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

            return Ok(GateTransitApiResponse.CreateSuccess("L?y danh sách xe thành công.", vehicles));
        }

        [HttpGet("vehicle-by-plate/{licensePlate}")]
        public async Task<IActionResult> GetVehicleByPlate(string licensePlate)
        {
            var normalizedPlate = NormalizeLicensePlate(licensePlate);

            if (string.IsNullOrWhiteSpace(normalizedPlate))
            {
                return BadRequest(GateTransitApiResponse.CreateError("Bi?n s? không h?p l?."));
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
                return NotFound(GateTransitApiResponse.CreateError("Không tìm th?y bi?n s? này."));
            }

            return Ok(GateTransitApiResponse.CreateSuccess("L?y thông tin xe thành công.", result));
        }

        [HttpGet("logs-by-plate/{licensePlate}")]
        public async Task<IActionResult> GetLogsByPlate(string licensePlate)
        {
            var normalizedPlate = NormalizeLicensePlate(licensePlate);

            if (string.IsNullOrWhiteSpace(normalizedPlate))
            {
                return BadRequest(GateTransitApiResponse.CreateError("Bi?n s? không h?p l?."));
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

            return Ok(GateTransitApiResponse.CreateSuccess("L?y l?ch s? theo bi?n s? thành công.", result));
        }

        [HttpGet("logs-by-employee/{employeeId:int}")]
        public async Task<IActionResult> GetLogsByEmployee(int employeeId)
        {
            var employeeExists = await _context.Employees
                .AnyAsync(e => e.EmployeeId == employeeId);

            if (!employeeExists)
            {
                return NotFound(GateTransitApiResponse.CreateError($"Không tìm th?y nhân viên có id = {employeeId}."));
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

            return Ok(GateTransitApiResponse.CreateSuccess("L?y l?ch s? theo nhân viên thành công.", result));
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
