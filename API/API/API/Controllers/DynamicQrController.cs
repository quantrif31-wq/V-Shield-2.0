using API.Data;
using API.Models;
using API.Models.DTOs;
using API.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Data;
using System.Security.Cryptography;
using System.Text;

namespace API.Controllers
{
    [Route("api/dynamic-qr")]
    [Route("api/QR_Dong")]
    [ApiController]
    public class DynamicQrController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<DynamicQrController> _logger;
        private readonly StaticVisitorQrService _visitorQrService;

        public DynamicQrController(
    ApplicationDbContext context,
    ILogger<DynamicQrController> logger,
    StaticVisitorQrService visitorQrService)
        {
            _context = context;
            _logger = logger;
            _visitorQrService = visitorQrService;
        }

        /// <summary>
        /// T?o QR d?ng hi?n t?i cho nhân viên.
        /// Luu ý b?o m?t:
        /// - Ch? tr? qrPayload cho FE d? render QR
        /// - KHÔNG tr? OTP raw ra ngoài ngoài payload QR
        /// </summary>
        [HttpPost("generate")]
        public async Task<IActionResult> GenerateDynamicQr([FromBody] GenerateDynamicQrRequest request)
        {
            if (request == null || request.EmployeeId <= 0)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "EmployeeId không h?p l?."
                });
            }

            var employee = await _context.Employees
                .FirstOrDefaultAsync(x => x.EmployeeId == request.EmployeeId && x.Status == true);

            if (employee == null)
            {
                return NotFound(new
                {
                    success = false,
                    message = "Không tìm th?y nhân viên ho?c nhân viên dang không ho?t d?ng."
                });
            }

            var dynamicQr = await _context.EmployeeDynamicQrs
                .FirstOrDefaultAsync(x => x.EmployeeId == request.EmployeeId);

            if (dynamicQr == null)
            {
                dynamicQr = new EmployeeDynamicQr
                {
                    EmployeeId = request.EmployeeId,
                    SecretKey = GenerateBase32Secret(),
                    TimeStepSeconds = 30,
                    Digits = 6,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };

                _context.EmployeeDynamicQrs.Add(dynamicQr);
                await _context.SaveChangesAsync();
            }

            if (!dynamicQr.IsActive)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "QR d?ng c?a nhân viên này dang b? vô hi?u hóa."
                });
            }

            var utcNow = DateTime.UtcNow;
            var counter = GetCurrentCounter(utcNow, dynamicQr.TimeStepSeconds);
            var otp = GenerateTotp(dynamicQr.SecretKey, counter, dynamicQr.Digits);
            var expiresAtUtc = GetCounterExpiryUtc(utcNow, dynamicQr.TimeStepSeconds);

            // Payload FE dùng d? render thành QR image
            var qrPayload = $"EMP:{employee.EmployeeId}|TS:{counter}|OTP:{otp}";

            return Ok(new
            {
                success = true,
                message = "T?o QR d?ng thành công.",
                data = new
                {
                    employeeId = employee.EmployeeId,
                    employeeName = employee.FullName,
                    qrPayload,
                    timeStepSeconds = dynamicQr.TimeStepSeconds,
                    generatedAtUtc = utcNow,
                    expiresAtUtc,
                    remainingSeconds = (int)Math.Max(0, Math.Floor((expiresAtUtc - utcNow).TotalSeconds))
                }
            });
        }

        /// <summary>
        /// Verify QR d?ng:
        /// 1. Parse payload
        /// 2. Tìm secret c?a nhân viên
        /// 3. Ch? ch?p nh?n dúng counter hi?n t?i
        /// 4. OTP ph?i kh?p tuy?t d?i
        /// 5. KHÔNG ch?n quét l?p l?i trong cùng time-step
        /// => Mi?n còn th?i gian hi?u l?c thì du?c dùng nhi?u l?n
        /// </summary>
        [HttpPost("verify")]
        public async Task<IActionResult> VerifyDynamicQr([FromBody] VerifyDynamicQrRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.QrPayload))
            {
                return BadRequest(new
                {
                    success = false,
                    message = "QrPayload không du?c d? tr?ng."
                });
            }

            var normalizedPayload = (request.QrPayload ?? string.Empty).Trim();
            var parseResult = ParseQrPayload(normalizedPayload);
            if (!parseResult.Success)
{
    // ?? fallback sang QR tinh
    var staticResult = await TryVerifyStaticVisitorQr(request);

    if (staticResult.Success)
    {
        await SaveScanLog(null, request.QrPayload, true,
        "Xác th?c QR tinh thành công.", request.ScannerDevice);

        return Ok(new
        {
            success = true,
            message = "Xác th?c QR tinh thành công.",
            data = staticResult.Data
        });
    }

    await SaveScanLog(null, request.QrPayload, false,
        $"Dynamic fail: {parseResult.Message} | Static fail: {staticResult.Message}",
        request.ScannerDevice);

    return BadRequest(new
    {
        success = false,
        message = "QR không h?p l? (c? d?ng và tinh)."
    });
}

            var employeeId = parseResult.EmployeeId!.Value;
            var payloadCounter = parseResult.Counter!.Value;
            var payloadOtp = parseResult.Otp!;

            await using var transaction = await _context.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted);

            try
            {
                var dynamicQr = await _context.EmployeeDynamicQrs
                    .Include(x => x.Employee)
                    .FirstOrDefaultAsync(x => x.EmployeeId == employeeId && x.IsActive);

                if (dynamicQr == null)
                {
                    await SaveScanLog(employeeId, request.QrPayload, false,
                        "Không tìm th?y c?u hình QR d?ng trong database.", request.ScannerDevice);

                    await transaction.CommitAsync();

                    return NotFound(new
                    {
                        success = false,
                        message = "Không tìm th?y QR d?ng c?a nhân viên trong database."
                    });
                }

                if (dynamicQr.Employee == null || dynamicQr.Employee.Status != true)
                {
                    await SaveScanLog(employeeId, request.QrPayload, false,
                        "Nhân viên không ho?t d?ng ho?c không t?n t?i.", request.ScannerDevice);

                    await transaction.CommitAsync();

                    return BadRequest(new
                    {
                        success = false,
                        message = "Nhân viên không ho?t d?ng ho?c không t?n t?i."
                    });
                }

                var utcNow = DateTime.UtcNow;
                var currentCounter = GetCurrentCounter(utcNow, dynamicQr.TimeStepSeconds);
                var counterDelta = Math.Abs(payloadCounter - currentCounter);

                // Cho phep lech toi da 1 time-step de tranh loi sat bien 30s.
                if (counterDelta > 1)
                {
                    await SaveScanLog(employeeId, request.QrPayload, false,
                        "QR dã h?t h?n ho?c chua d?n hi?u l?c.", request.ScannerDevice);

                    await transaction.CommitAsync();

                    return BadRequest(new
                    {
                        success = false,
                        message = "QR dã h?t h?n ho?c chua d?n hi?u l?c."
                    });
                }

                // OTP duoc sinh theo counter nam trong payload (sau khi da check delta <= 1).
                var expectedOtp = GenerateTotp(dynamicQr.SecretKey, payloadCounter, dynamicQr.Digits);

                if (!FixedTimeEquals(payloadOtp, expectedOtp))
                {
                    await SaveScanLog(employeeId, request.QrPayload, false,
                        "QR d?ng không h?p l?.", request.ScannerDevice);

                    await transaction.CommitAsync();

                    // ?? fallback QR tinh
                    var staticResult = await TryVerifyStaticVisitorQr(request);

                    if (staticResult.Success)
                    {
                        await SaveScanLog(employeeId, request.QrPayload, true,
                            "Fallback sang QR tinh thành công.", request.ScannerDevice);

                        await transaction.CommitAsync();

                        return Ok(new
                        {
                            success = true,
                            message = "Xác th?c QR tinh thành công.",
                            data = staticResult.Data
                        });
                    }
                }

                // ÐÃ B? CO CH?:
                // if (dynamicQr.LastUsedCounter.HasValue && dynamicQr.LastUsedCounter.Value == currentCounter)
                // {
                //     return Conflict(... "QR này dã du?c s? d?ng tru?c dó.")
                // }

                // Có th? v?n c?p nh?t th?i gian verify g?n nh?t d? audit
                dynamicQr.UpdatedAt = utcNow;

                // N?u b?n mu?n luu th?ng kê l?n quét g?n nh?t thì m? l?i dòng du?i:
                // dynamicQr.LastUsedCounter = currentCounter;

                await _context.SaveChangesAsync();

                await SaveScanLog(employeeId, request.QrPayload, true,
                    "Xác th?c QR d?ng thành công.", request.ScannerDevice);

                await transaction.CommitAsync();

                return Ok(new
                {
                    success = true,
                    message = "Xác th?c QR d?ng thành công.",
                    data = new
                    {
                        employeeId = dynamicQr.EmployeeId,
                        employeeName = dynamicQr.Employee.FullName,
                        verifiedAtUtc = utcNow,
                        counter = currentCounter,
                        expiresAtUtc = GetCounterExpiryUtc(utcNow, dynamicQr.TimeStepSeconds)
                    }
                });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();

                _logger.LogError(ex, "L?i khi verify QR d?ng.");

                await SaveScanLog(employeeId, request.QrPayload, false,
                    "L?i h? th?ng khi xác th?c QR.", request.ScannerDevice);

                return StatusCode(StatusCodes.Status500InternalServerError, new
                {
                    success = false,
                    message = "L?i h? th?ng khi xác th?c QR."
                });
            }
        }

        // =========================
        // HELPER METHODS
        // =========================

        private async Task SaveScanLog(int? employeeId, string qrPayload, bool isValid, string message, string? scannerDevice)
        {
            try
            {
                var log = new DynamicQrScanLog
                {
                    EmployeeId = employeeId ?? 0,
                    QrPayload = qrPayload,
                    IsValid = isValid,
                    Message = message,
                    ScannerDevice = scannerDevice,
                    ScannedAt = DateTime.UtcNow
                };

                _context.DynamicQrScanLogs.Add(log);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "L?i khi luu DynamicQrScanLog");
            }
        }

        private static long GetCurrentCounter(DateTime utcNow, int timeStepSeconds)
        {
            var unixTime = new DateTimeOffset(utcNow).ToUnixTimeSeconds();
            return unixTime / timeStepSeconds;
        }

        private static DateTime GetCounterExpiryUtc(DateTime utcNow, int timeStepSeconds)
        {
            var unixTime = new DateTimeOffset(utcNow).ToUnixTimeSeconds();
            var nextBoundary = ((unixTime / timeStepSeconds) + 1) * timeStepSeconds;
            return DateTimeOffset.FromUnixTimeSeconds(nextBoundary).UtcDateTime;
        }

        private static string GenerateTotp(string base32Secret, long counter, int digits = 6)
        {
            var key = Base32Decode(base32Secret);
            var counterBytes = BitConverter.GetBytes(counter);

            if (BitConverter.IsLittleEndian)
                Array.Reverse(counterBytes);

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

        private static string GenerateBase32Secret(int length = 20)
        {
            var bytes = new byte[length];
            RandomNumberGenerator.Fill(bytes);
            return Base32Encode(bytes);
        }

        private static (bool Success, int? EmployeeId, long? Counter, string? Otp, string Message) ParseQrPayload(string payload)
        {
            try
            {
                var parts = payload.Split('|', StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length != 3)
                    return (false, null, null, null, "Payload QR không dúng d?nh d?ng.");

                var empPart = parts[0].Split(':');
                var tsPart = parts[1].Split(':');
                var otpPart = parts[2].Split(':');

                if (empPart.Length != 2 || tsPart.Length != 2 || otpPart.Length != 2)
                    return (false, null, null, null, "Payload QR không dúng d?nh d?ng.");

                if (!empPart[0].Equals("EMP", StringComparison.OrdinalIgnoreCase))
                    return (false, null, null, null, "Thi?u EMP trong payload.");

                if (!tsPart[0].Equals("TS", StringComparison.OrdinalIgnoreCase))
                    return (false, null, null, null, "Thi?u TS trong payload.");

                if (!otpPart[0].Equals("OTP", StringComparison.OrdinalIgnoreCase))
                    return (false, null, null, null, "Thi?u OTP trong payload.");

                if (!int.TryParse(empPart[1], out var employeeId))
                    return (false, null, null, null, "EmployeeId không h?p l?.");

                if (!long.TryParse(tsPart[1], out var counter))
                    return (false, null, null, null, "Counter không h?p l?.");

                var otp = otpPart[1]?.Trim();
                if (string.IsNullOrWhiteSpace(otp))
                    return (false, null, null, null, "OTP không h?p l?.");

                return (true, employeeId, counter, otp, "OK");
            }
            catch
            {
                return (false, null, null, null, "Không th? phân tích payload QR.");
            }
        }

        private static bool FixedTimeEquals(string left, string right)
        {
            if (left == null || right == null)
                return false;

            var leftBytes = Encoding.UTF8.GetBytes(left);
            var rightBytes = Encoding.UTF8.GetBytes(right);

            return leftBytes.Length == rightBytes.Length &&
                   CryptographicOperations.FixedTimeEquals(leftBytes, rightBytes);
        }

        private static string Base32Encode(byte[] data)
        {
            const string alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";
            var result = new StringBuilder();

            if (data == null || data.Length == 0)
                return string.Empty;

            int bitBuffer = data[0];
            int currentByte = 1;
            int bitsLeft = 8;

            while (bitsLeft > 0 || currentByte < data.Length)
            {
                if (bitsLeft < 5)
                {
                    if (currentByte < data.Length)
                    {
                        bitBuffer <<= 8;
                        bitBuffer |= data[currentByte++] & 0xFF;
                        bitsLeft += 8;
                    }
                    else
                    {
                        int pad = 5 - bitsLeft;
                        bitBuffer <<= pad;
                        bitsLeft += pad;
                    }
                }

                int index = 0x1F & (bitBuffer >> (bitsLeft - 5));
                bitsLeft -= 5;
                result.Append(alphabet[index]);
            }

            return result.ToString();
        }

        private static byte[] Base32Decode(string input)
        {
            const string alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";

            input = input.Trim().TrimEnd('=').ToUpperInvariant();

            var output = new List<byte>();
            int bitBuffer = 0;
            int bitsLeft = 0;

            foreach (char c in input)
            {
                int val = alphabet.IndexOf(c);
                if (val < 0)
                    throw new FormatException("SecretKey Base32 không h?p l?.");

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
        private async Task<(bool Success, string Message, object? Data)> TryVerifyStaticVisitorQr(VerifyDynamicQrRequest request)
        {
            try
            {
                var ok = _visitorQrService.TryParsePayload(request.QrPayload, out var payload, out var message);

                if (!ok || payload == null)
                    return (false, message, null);

                var visitor = await _context.VisitorDetails
    .Include(v => v.Registration)
    .ThenInclude(r => r!.HostEmployee)
    .FirstOrDefaultAsync(v =>
        v.VisitorDetailId == payload.VisitorId &&
        v.RegistrationId == payload.RegistrationId);

                if (visitor == null)
                    return (false, "Không tìm th?y visitor", null);

                if (!visitor.IsQrActive)
                    return (false, "QR dã b? khóa", null);

                var expectedOtp = _visitorQrService.GenerateOtp(visitor.QrSecret);

                if (payload.Otp != expectedOtp)
                    return (false, "OTP không dúng", null);

                return (true, "OK", new
                {
                    type = "STATIC",
                    visitorId = visitor.VisitorDetailId,
                    fullName = visitor.FullName,
                    host = visitor.Registration?.HostEmployee?.FullName
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "L?i verify QR tinh");
                return (false, "L?i h? th?ng QR tinh", null);
            }
        }
    }
}



