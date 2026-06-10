using API.Data;
using API.DTOs.PreRegistration;
using API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace API.Controllers;

[ApiController]
[Route("api/registration-links")]
public class RegistrationLinkController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly IConfiguration _config;

    public RegistrationLinkController(ApplicationDbContext context, IConfiguration config)
    {
        _context = context;
        _config = config;
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> CreateLink([FromBody] CreateLinkRequestDto dto)
    {
        var employee = await _context.Employees.FindAsync(dto.HostEmployeeId);
        if (employee == null)
            return NotFound(new { Message = "KhÃ´ng tÃ¬m tháº¥y nhÃ¢n viÃªn" });

        // Táº¡o token 32 kÃ½ tá»± khÃ´ng dáº¥u gáº¡ch, khÃ³ Ä‘oÃ¡n
        var token = Guid.NewGuid().ToString("N");

        var link = new RegistrationLink
        {
            Token = token,
            HostEmployeeId = dto.HostEmployeeId,
            ExpiredAt = DateTime.Now.AddHours(dto.ExpiryHours),
            IsUsed = false,
            CreatedAt = DateTime.Now
        };

        _context.RegistrationLinks.Add(link);
        await _context.SaveChangesAsync();

        var frontendUrl = ResolvePortalBaseUrl();

        return Ok(new CreateLinkResponseDto
        {
            Token = token,
            RegistrationUrl = $"{frontendUrl}/register/{token}",
            ExpiredAt = link.ExpiredAt
        });
    }

    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetLinks([FromQuery] string? query = null)
    {
        var linksQuery = _context.RegistrationLinks
            .AsNoTracking()
            .Include(link => link.HostEmployee)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(query))
        {
            var normalized = query.Trim();
            linksQuery = linksQuery.Where(link =>
                link.Token.Contains(normalized) ||
                link.HostEmployee.FullName.Contains(normalized));
        }

        var now = DateTime.Now;
        var frontendUrl = ResolvePortalBaseUrl();

        var items = await linksQuery
            .OrderByDescending(link => link.CreatedAt)
            .Select(link => new
            {
                link.LinkId,
                link.Token,
                link.HostEmployeeId,
                hostEmployeeName = link.HostEmployee.FullName,
                link.ExpiredAt,
                link.IsUsed,
                link.CreatedAt,
                isExpired = link.ExpiredAt < now,
                registrationUrl = $"{frontendUrl}/register/{link.Token}"
            })
            .ToListAsync();

        return Ok(items);
    }

    private string ResolvePortalBaseUrl()
    {
        // 1) Æ¯u tiÃªn cáº¥u hÃ¬nh riÃªng cho trang Ä‘Äƒng kÃ½ khÃ¡ch (náº¿u cÃ³)
        var guestPortalUrl = (_config["AppSettings:GuestRegistrationPortalUrl"] ?? string.Empty).Trim();
        if (!string.IsNullOrWhiteSpace(guestPortalUrl))
        {
            return guestPortalUrl.TrimEnd('/');
        }

        // 2) Æ¯u tiÃªn Origin thá»±c táº¿ tá»« request UI Ä‘ang gá»i API (thÆ°á»ng lÃ  portal chÃ­nh)
        var originHeader = Request.Headers["Origin"].FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(originHeader))
        {
            return originHeader.TrimEnd('/');
        }

        // 3) Fallback cáº¥u hÃ¬nh FrontendUrl (Ä‘á»ƒ tÆ°Æ¡ng thÃ­ch cÅ©)
        var frontendUrl = (_config["AppSettings:FrontendUrl"] ?? string.Empty).Trim();
        if (!string.IsNullOrWhiteSpace(frontendUrl))
        {
            return frontendUrl.TrimEnd('/');
        }

        // 4) Fallback cuá»‘i: host hiá»‡n táº¡i cá»§a API
        return $"{Request.Scheme}://{Request.Host}".TrimEnd('/');
    }
}

