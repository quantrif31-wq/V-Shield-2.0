using API.Data;
using API.DTOs.PreRegistration;
using API.Models;
using API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace API.Controllers;

[ApiController]
[Route("api/pre-registrations")]
public class PreRegistrationController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly StaticVisitorQrService _qrService;

    public PreRegistrationController(ApplicationDbContext context, StaticVisitorQrService qrService)
    {
        _context = context;
        _qrService = qrService;
    }

    [HttpGet("validate/{token}")]
    [AllowAnonymous]
    public async Task<IActionResult> ValidateToken(string token)
    {
        var link = await _context.RegistrationLinks
            .Include(l => l.HostEmployee)
                .ThenInclude(e => e.Department)
            .Include(l => l.HostEmployee)
                .ThenInclude(e => e.Position)
            .Include(l => l.HostEmployee)
                .ThenInclude(e => e.Vehicles)
            .FirstOrDefaultAsync(l => l.Token == token);

        if (link == null)
            return NotFound(new { Message = "Link không tồn tại" });

        if (link.IsUsed)
            return BadRequest(new { Message = "Link đã được sử dụng" });

        if (link.ExpiredAt < DateTime.Now)
            return BadRequest(new { Message = "Link đã hết hạn", ExpiredAt = link.ExpiredAt });

        return Ok(new ValidateTokenResponseDto
        {
            HostEmployeeName = link.HostEmployee.FullName,
            HostEmployeePhone = link.HostEmployee.Phone,
            HostEmployeeEmail = link.HostEmployee.Email,
            HostDepartmentName = link.HostEmployee.Department?.Name,
            HostPositionName = link.HostEmployee.Position?.Name,
            HostFaceImageUrl = link.HostEmployee.FaceImageUrl,
            HostLicensePlates = link.HostEmployee.Vehicles?.Select(v => v.LicensePlate).ToList(),
            ExpiredAt = link.ExpiredAt
        });
    }

    [HttpPost("submit/{token}")]
    [AllowAnonymous]
    public async Task<IActionResult> SubmitRegistration(
        string token,
        [FromBody] SubmitRegistrationDto dto)
    {
        var link = await _context.RegistrationLinks
            .FirstOrDefaultAsync(l => l.Token == token);

        if (link == null || link.IsUsed || link.ExpiredAt < DateTime.Now)
            return BadRequest(new { Message = "Link không hợp lệ hoặc đã hết hạn" });

        if (dto.ExpectedTimeOut <= dto.ExpectedTimeIn)
            return BadRequest(new { Message = "Thời gian ra phải sau thời gian vào" });

        await using var tx = await _context.Database.BeginTransactionAsync();

        try
        {
            GuestProfile guest;

            if (!string.IsNullOrEmpty(dto.Phone))
            {
                guest = await _context.GuestProfiles
                            .FirstOrDefaultAsync(g => g.Phone == dto.Phone)
                        ?? new GuestProfile();
            }
            else
            {
                guest = new GuestProfile();
            }

            guest.FullName = dto.FullName;
            guest.Phone = dto.Phone;

            if (guest.GuestId == 0)
                _context.GuestProfiles.Add(guest);

            await _context.SaveChangesAsync();

            var preReg = new PreRegistration
            {
                GuestId = guest.GuestId,
                HostEmployeeId = link.HostEmployeeId,
                ExpectedTimeIn = dto.ExpectedTimeIn,
                ExpectedTimeOut = dto.ExpectedTimeOut,
                NumberOfVisitors = dto.NumberOfVisitors,
                Status = "Pending",
                CreatedAt = DateTime.Now
            };

            _context.PreRegistrations.Add(preReg);
            await _context.SaveChangesAsync();

            var nowUtc = DateTime.UtcNow;
            var visitorEntities = new List<VisitorDetail>();

            if (dto.Visitors != null && dto.Visitors.Any())
            {
                visitorEntities = dto.Visitors.Select(v => new VisitorDetail
                {
                    RegistrationId = preReg.RegistrationId,
                    FullName = v.FullName,
                    IdCardNumber = v.IdCardNumber,
                    ExpectedFaceImage = v.ExpectedFaceImage,
                    IsQrActive = true,
                    QrIssuedAt = nowUtc
                }).ToList();

                _context.VisitorDetails.AddRange(visitorEntities);
                await _context.SaveChangesAsync();

                foreach (var visitor in visitorEntities)
                {
                    var secret = _qrService.GenerateSecret();
                    var otp = _qrService.GenerateOtp(secret);
                    visitor.QrSecret = secret;
                    visitor.QrPayload = _qrService.BuildPayload(visitor.VisitorDetailId, visitor.RegistrationId, otp);
                }

                await _context.SaveChangesAsync();
            }

            link.IsUsed = true;
            await _context.SaveChangesAsync();

            await tx.CommitAsync();

            return Ok(new
            {
                Message = "Đăng ký thành công! Vui lòng chờ xác nhận.",
                RegistrationId = preReg.RegistrationId,
                Visitors = visitorEntities.Select(v => new
                {
                    v.VisitorDetailId,
                    v.FullName,
                    v.IdCardNumber,
                    v.ExpectedFaceImage,
                    QrCodeData = v.QrPayload,
                    v.QrIssuedAt,
                    v.IsQrActive
                }).ToList()
            });
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }

    [HttpGet]
    [Authorize]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? status,
        [FromQuery] DateTime? date,
        [FromQuery] int? hostEmployeeId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var query = _context.PreRegistrations
            .Include(r => r.Guest)
            .Include(r => r.HostEmployee)
            .AsQueryable();

        if (!string.IsNullOrEmpty(status))
            query = query.Where(r => r.Status == status);

        if (date.HasValue)
            query = query.Where(r => r.ExpectedTimeIn.Date == date.Value.Date);

        if (hostEmployeeId.HasValue)
            query = query.Where(r => r.HostEmployeeId == hostEmployeeId);

        var total = await query.CountAsync();

        var items = await query
            .OrderByDescending(r => r.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(r => new RegistrationListItemDto
            {
                RegistrationId = r.RegistrationId,
                GuestId = r.Guest!.GuestId,
                GuestFullName = r.Guest.FullName,
                GuestPhone = r.Guest.Phone,
                ExpectedTimeIn = r.ExpectedTimeIn,
                ExpectedTimeOut = r.ExpectedTimeOut,
                NumberOfVisitors = r.NumberOfVisitors,
                Status = r.Status,
                HostEmployeeName = r.HostEmployee!.FullName,
                CreatedAt = r.CreatedAt
            })
            .ToListAsync();

        return Ok(new
        {
            Total = total,
            Page = page,
            PageSize = pageSize,
            Items = items
        });
    }

    [HttpGet("{id}")]
    [Authorize]
    public async Task<IActionResult> GetDetail(int id)
    {
        var reg = await _context.PreRegistrations
            .Include(r => r.Guest)
            .Include(r => r.HostEmployee)
            .Include(r => r.VisitorDetails)
            .Include(r => r.AccessLogs)
            .FirstOrDefaultAsync(r => r.RegistrationId == id);

        if (reg == null)
            return NotFound(new { Message = "Không tìm thấy đơn đăng ký" });

        return Ok(new RegistrationDetailDto
        {
            RegistrationId = reg.RegistrationId,
            GuestId = reg.Guest!.GuestId,
            GuestFullName = reg.Guest.FullName,
            GuestPhone = reg.Guest.Phone,
            ExpectedTimeIn = reg.ExpectedTimeIn,
            ExpectedTimeOut = reg.ExpectedTimeOut,
            NumberOfVisitors = reg.NumberOfVisitors,
            Status = reg.Status,
            HostEmployeeName = reg.HostEmployee!.FullName,
            CreatedAt = reg.CreatedAt,

            Visitors = reg.VisitorDetails
                .OrderBy(v => v.VisitorDetailId)
                .Select(v => new VisitorInfoDto
                {
                    FullName = v.FullName,
                    IdCardNumber = v.IdCardNumber,
                    ExpectedFaceImage = v.ExpectedFaceImage,
                    QrCodeData = v.QrPayload,
                    QrIssuedAt = v.QrIssuedAt,
                    IsQrActive = v.IsQrActive
                }).ToList(),

            AccessLogs = reg.AccessLogs
                .OrderBy(l => l.Timestamp)
                .Select(l => new AccessLogDto
                {
                    LogId = l.LogId,
                    Timestamp = l.Timestamp,
                    Direction = l.Direction,
                    CapturedLicensePlate = l.CapturedLicensePlate,
                    ResultStatus = l.ResultStatus,
                    Note = l.Note
                }).ToList()
        });
    }

    [HttpPatch("{id}/status")]
    [Authorize]
    public async Task<IActionResult> UpdateStatus(int id, [FromBody] UpdateStatusDto dto)
    {
        var reg = await _context.PreRegistrations.FindAsync(id);

        if (reg == null)
            return NotFound(new { Message = "Không tìm thấy đơn đăng ký" });

        if (reg.Status is "Approved" or "Rejected")
            return BadRequest(new { Message = $"Đơn đã được xử lý ({reg.Status}), không thể thay đổi" });

        reg.Status = dto.Status;
        await _context.SaveChangesAsync();

        return Ok(new { Message = $"Đã cập nhật trạng thái thành '{dto.Status}'" });
    }

    [HttpPost("verify-visitor-qr")]
    [Authorize]
    public async Task<IActionResult> VerifyVisitorQr([FromBody] VerifyVisitorQrDto dto)
    {
        if (dto == null || string.IsNullOrWhiteSpace(dto.QrPayload))
            return BadRequest(new { Message = "QrPayload không được để trống." });

        if (!_qrService.TryParsePayload(dto.QrPayload, out var parsed, out var parseMessage) || parsed == null)
            return BadRequest(new { Message = parseMessage });

        var visitor = await _context.VisitorDetails
            .Include(v => v.Registration)
            .FirstOrDefaultAsync(v =>
                v.VisitorDetailId == parsed.VisitorId &&
                v.RegistrationId == parsed.RegistrationId);

        if (visitor == null)
            return NotFound(new { Message = "Không tìm thấy khách sở hữu QR này." });

        if (!visitor.IsQrActive)
            return BadRequest(new { Message = "QR này đã bị vô hiệu hóa." });

        if (string.IsNullOrWhiteSpace(visitor.QrSecret) || string.IsNullOrWhiteSpace(visitor.QrPayload))
            return BadRequest(new { Message = "QR chưa được khởi tạo đầy đủ." });

        var expectedOtp = _qrService.GenerateOtp(visitor.QrSecret);

        if (!string.Equals(parsed.Otp, expectedOtp, StringComparison.Ordinal))
            return BadRequest(new { Message = "QR không hợp lệ." });

        if (!string.Equals(dto.QrPayload, visitor.QrPayload, StringComparison.Ordinal))
            return BadRequest(new { Message = "QR không khớp dữ liệu đã lưu." });

        return Ok(new
        {
            Message = "QR hợp lệ",
            Data = new
            {
                visitor.VisitorDetailId,
                visitor.RegistrationId,
                visitor.FullName,
                visitor.IdCardNumber,
                visitor.IsQrActive,
                visitor.QrIssuedAt
            }
        });
    }
}