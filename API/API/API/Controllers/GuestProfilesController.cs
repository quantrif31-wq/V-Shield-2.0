using API.Data;
using API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace API.Controllers;

[ApiController]
[Route("api/guest-profiles")]
[Authorize(Roles = "Admin,LeTan")]
public class GuestProfilesController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public GuestProfilesController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? query = null)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var guestsQuery = _context.GuestProfiles.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(query))
        {
            var normalized = query.Trim();
            guestsQuery = guestsQuery.Where(guest =>
                guest.FullName.Contains(normalized) ||
                (guest.Phone != null && guest.Phone.Contains(normalized)) ||
                (guest.DefaultLicensePlate != null && guest.DefaultLicensePlate.Contains(normalized)));
        }

        var total = await guestsQuery.CountAsync();
        var items = await guestsQuery
            .OrderBy(guest => guest.FullName)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(guest => new
            {
                guest.GuestId,
                guest.FullName,
                guest.Phone,
                guest.DefaultLicensePlate,
                guest.FaceImageUrl,
                preRegistrationCount = guest.PreRegistrations.Count(),
                lastRegistrationAt = guest.PreRegistrations
                    .OrderByDescending(registration => registration.CreatedAt)
                    .Select(registration => (DateTime?)registration.CreatedAt)
                    .FirstOrDefault(),
                nextExpectedVisit = guest.PreRegistrations
                    .Where(registration => registration.ExpectedTimeIn >= DateTime.Today)
                    .OrderBy(registration => registration.ExpectedTimeIn)
                    .Select(registration => (DateTime?)registration.ExpectedTimeIn)
                    .FirstOrDefault()
            })
            .ToListAsync();

        return Ok(new
        {
            page,
            pageSize,
            total,
            items
        });
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetDetail(int id)
    {
        var guest = await _context.GuestProfiles.AsNoTracking()
            .Where(item => item.GuestId == id)
            .Select(item => new
            {
                item.GuestId,
                item.FullName,
                item.Phone,
                item.DefaultLicensePlate,
                item.FaceImageUrl,
                preRegistrations = item.PreRegistrations
                    .OrderByDescending(registration => registration.CreatedAt)
                    .Select(registration => new
                    {
                        registration.RegistrationId,
                        registration.ExpectedTimeIn,
                        registration.ExpectedTimeOut,
                        registration.Status,
                        // Đã xóa dòng registration.ExpectedLicensePlate ở đây
                        registration.NumberOfVisitors,
                        hostEmployeeName = registration.HostEmployee != null ? registration.HostEmployee.FullName : null
                    })
                    .ToList()
            })
            .FirstOrDefaultAsync();

        if (guest == null)
        {
            return NotFound(new { message = $"Không tìm thấy hồ sơ khách #{id}" });
        }

        return Ok(guest);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] UpsertGuestProfileRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.FullName))
        {
            return BadRequest(new { message = "Họ tên khách là bắt buộc" });
        }

        var guest = new GuestProfile
        {
            FullName = request.FullName.Trim(),
            Phone = NormalizeOptional(request.Phone),
            DefaultLicensePlate = NormalizeOptional(request.DefaultLicensePlate),
            FaceImageUrl = NormalizeOptional(request.FaceImageUrl)
        };

        _context.GuestProfiles.Add(guest);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetDetail), new { id = guest.GuestId }, new
        {
            guest.GuestId,
            guest.FullName,
            guest.Phone,
            guest.DefaultLicensePlate,
            guest.FaceImageUrl
        });
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpsertGuestProfileRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.FullName))
        {
            return BadRequest(new { message = "Họ tên khách là bắt buộc" });
        }

        var guest = await _context.GuestProfiles.FindAsync(id);
        if (guest == null)
        {
            return NotFound(new { message = $"Không tìm thấy hồ sơ khách #{id}" });
        }

        guest.FullName = request.FullName.Trim();
        guest.Phone = NormalizeOptional(request.Phone);
        guest.DefaultLicensePlate = NormalizeOptional(request.DefaultLicensePlate);
        guest.FaceImageUrl = NormalizeOptional(request.FaceImageUrl);

        await _context.SaveChangesAsync();

        return Ok(new
        {
            guest.GuestId,
            guest.FullName,
            guest.Phone,
            guest.DefaultLicensePlate,
            guest.FaceImageUrl
        });
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var guest = await _context.GuestProfiles
            .Include(item => item.PreRegistrations)
            .FirstOrDefaultAsync(item => item.GuestId == id);

        if (guest == null)
        {
            return NotFound(new { message = $"Không tìm thấy hồ sơ khách #{id}" });
        }

        if (guest.PreRegistrations.Any())
        {
            return BadRequest(new
            {
                message = $"Không thể xóa hồ sơ khách đang có {guest.PreRegistrations.Count} lượt đăng ký liên quan"
            });
        }

        _context.GuestProfiles.Remove(guest);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    [HttpGet("visitor-directory")]
    public async Task<IActionResult> GetVisitorDirectory(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? query = null,
        [FromQuery] int? hostEmployeeId = null,
        [FromQuery] string? registrationStatus = null,
        [FromQuery] string? idCardNumber = null)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var visitorsQuery = _context.VisitorDetails
            .AsNoTracking()
            .Include(v => v.Registration)
                .ThenInclude(r => r!.HostEmployee)
            .Include(v => v.Registration)
                .ThenInclude(r => r!.Guest)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(query))
        {
            var normalized = query.Trim();
            visitorsQuery = visitorsQuery.Where(v =>
                v.FullName.Contains(normalized) ||
                (v.IdCardNumber != null && v.IdCardNumber.Contains(normalized)) ||
                (v.Registration != null && v.Registration.Guest != null && v.Registration.Guest.Phone != null && v.Registration.Guest.Phone.Contains(normalized)) ||
                (v.Registration != null && v.Registration.HostEmployee != null && v.Registration.HostEmployee.FullName.Contains(normalized)));
        }
        if (hostEmployeeId.HasValue)
        {
            visitorsQuery = visitorsQuery.Where(v => v.Registration != null && v.Registration.HostEmployeeId == hostEmployeeId.Value);
        }
        if (!string.IsNullOrWhiteSpace(registrationStatus))
        {
            var status = registrationStatus.Trim();
            visitorsQuery = visitorsQuery.Where(v => v.Registration != null && v.Registration.Status != null && v.Registration.Status == status);
        }
        if (!string.IsNullOrWhiteSpace(idCardNumber))
        {
            var cccd = idCardNumber.Trim();
            visitorsQuery = visitorsQuery.Where(v => v.IdCardNumber != null && v.IdCardNumber.Contains(cccd));
        }

        var total = await visitorsQuery.CountAsync();
        var items = await visitorsQuery
            .OrderByDescending(v => v.VisitorDetailId)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(v => new
            {
                v.VisitorDetailId,
                v.RegistrationId,
                GuestId = v.Registration != null ? v.Registration.GuestId : null,
                v.FullName,
                v.IdCardNumber,
                GuestPhone = v.Registration != null && v.Registration.Guest != null ? v.Registration.Guest.Phone : null,
                HostEmployeeId = v.Registration != null ? v.Registration.HostEmployeeId : null,
                HostEmployeeName = v.Registration != null && v.Registration.HostEmployee != null ? v.Registration.HostEmployee.FullName : null,
                RegistrationStatus = v.Registration != null ? v.Registration.Status : null,
                v.IsQrActive
            })
            .ToListAsync();

        return Ok(new { page, pageSize, total, items });
    }

    [HttpPut("visitor-directory/{visitorDetailId:int}")]
    public async Task<IActionResult> UpdateVisitorDirectoryItem(int visitorDetailId, [FromBody] UpdateVisitorDirectoryRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.FullName))
            return BadRequest(new { message = "Ho ten khach la bat buoc." });

        var visitor = await _context.VisitorDetails
            .Include(v => v.Registration)
            .FirstOrDefaultAsync(v => v.VisitorDetailId == visitorDetailId);

        if (visitor == null || visitor.Registration == null)
            return NotFound(new { message = "Khong tim thay ban ghi khach." });

        visitor.FullName = request.FullName.Trim();
        visitor.IdCardNumber = NormalizeOptional(request.IdCardNumber);
        visitor.Registration.HostEmployeeId = request.HostEmployeeId;

        await _context.SaveChangesAsync();

        return Ok(new { message = "Cap nhat khach thanh cong." });
    }

    [HttpDelete("visitor-directory/{visitorDetailId:int}")]
    public async Task<IActionResult> DeleteVisitorDirectoryItem(int visitorDetailId)
    {
        var visitor = await _context.VisitorDetails
            .Include(v => v.Registration)
            .FirstOrDefaultAsync(v => v.VisitorDetailId == visitorDetailId);

        if (visitor == null)
            return NotFound(new { message = "Khong tim thay ban ghi khach." });

        var recentCutoff = DateTime.Now.AddDays(-7);
        var recentLogCount = await _context.AccessLogs
            .AsNoTracking()
            .Where(l => l.VisitorDetailId == visitorDetailId && l.Timestamp.HasValue && l.Timestamp.Value >= recentCutoff)
            .CountAsync();
        if (recentLogCount > 0)
        {
            return Conflict(new
            {
                message = $"Khong the xoa vi khach co {recentLogCount} log ra/vao trong 7 ngay gan day."
            });
        }

        _context.VisitorDetails.Remove(visitor);

        if (visitor.Registration != null && visitor.Registration.NumberOfVisitors > 0)
        {
            visitor.Registration.NumberOfVisitors = Math.Max(0, visitor.Registration.NumberOfVisitors - 1);
        }

        await _context.SaveChangesAsync();
        return NoContent();
    }

    [HttpGet("visitor-directory/{visitorDetailId:int}/access-logs")]
    public async Task<IActionResult> GetVisitorAccessLogs(int visitorDetailId)
    {
        var exists = await _context.VisitorDetails.AsNoTracking().AnyAsync(v => v.VisitorDetailId == visitorDetailId);
        if (!exists) return NotFound(new { message = "Khong tim thay khach." });

        var logs = await _context.AccessLogs
            .AsNoTracking()
            .Where(l => l.VisitorDetailId == visitorDetailId)
            .OrderByDescending(l => l.Timestamp)
            .Take(100)
            .Select(l => new
            {
                l.LogId,
                l.Timestamp,
                l.Direction,
                l.ResultStatus,
                l.Note,
                GateName = l.Gate != null ? l.Gate.GateName : null,
                CameraName = l.Camera != null ? l.Camera.CameraName : null
            })
            .ToListAsync();

        return Ok(new { items = logs });
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    public sealed class UpsertGuestProfileRequest
    {
        public string FullName { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public string? DefaultLicensePlate { get; set; }
        public string? FaceImageUrl { get; set; }
    }

    public sealed class UpdateVisitorDirectoryRequest
    {
        public string FullName { get; set; } = string.Empty;
        public string? IdCardNumber { get; set; }
        public int? HostEmployeeId { get; set; }
    }
}
