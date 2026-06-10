using System.Security.Claims;
using API.Data;
using API.DTOs;
using API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public class UsersController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly Services.IAuthenticationService _authService;

    public UsersController(ApplicationDbContext context, Services.IAuthenticationService authService)
    {
        _context = context;
        _authService = authService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var users = await _context.AppUsers
            .OrderBy(u => u.UserId)
            .Select(u => new UserResponse
            {
                UserId = u.UserId,
                Username = u.Username,
                FullName = u.FullName,
                Role = u.Role,
                IsActive = u.IsActive,
                CreatedAt = u.CreatedAt,
                EmployeeId = u.EmployeeId,
                MfaEnabled = u.MfaEnabled,
                MfaRequired = u.Role == "Admin" || u.Role == "BaoVe",
                LastLoginAtUtc = u.LastLoginAtUtc
            })
            .ToListAsync();

        return Ok(users);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var user = await _context.AppUsers.FindAsync(id);
        if (user == null)
            return NotFound(new { message = $"Khong tim thay tai khoan ID {id}" });

        return Ok(new UserResponse
        {
            UserId = user.UserId,
            Username = user.Username,
            FullName = user.FullName,
            Role = user.Role,
            IsActive = user.IsActive,
            CreatedAt = user.CreatedAt,
            EmployeeId = user.EmployeeId,
            MfaEnabled = user.MfaEnabled,
            MfaRequired = _authService.RequiresMfa(user),
            LastLoginAtUtc = user.LastLoginAtUtc
        });
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateUserRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var username = request.Username.Trim();
        if (string.IsNullOrWhiteSpace(username))
            return BadRequest(new { message = "Ten dang nhap khong duoc de trong" });

        var normalizedUsername = NormalizeUsernameInvariant(username);

        if (await _context.AppUsers.AnyAsync(u => u.Username.Trim().ToUpper() == normalizedUsername))
            return Conflict(new { message = $"Ten dang nhap '{username}' da ton tai" });

        if (request.EmployeeId.HasValue && request.EmployeeId.Value > 0)
        {
            if (!await _context.Employees.AnyAsync(e => e.EmployeeId == request.EmployeeId))
                return BadRequest(new { message = $"EmployeeID {request.EmployeeId} khong ton tai" });
        }

        var user = new AppUser
        {
            Username = username,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            FullName = request.FullName,
            Role = request.Role,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            LastPasswordChangedAtUtc = DateTime.UtcNow,
            EmployeeId = (request.EmployeeId.HasValue && request.EmployeeId.Value > 0) ? request.EmployeeId : null
        };

        _context.AppUsers.Add(user);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = user.UserId }, new UserResponse
        {
            UserId = user.UserId,
            Username = user.Username,
            FullName = user.FullName,
            Role = user.Role,
            IsActive = user.IsActive,
            CreatedAt = user.CreatedAt,
            EmployeeId = user.EmployeeId,
            MfaEnabled = user.MfaEnabled,
            MfaRequired = _authService.RequiresMfa(user),
            LastLoginAtUtc = user.LastLoginAtUtc
        });
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateUserRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var user = await _context.AppUsers.FindAsync(id);
        if (user == null)
            return NotFound(new { message = $"Khong tim thay tai khoan ID {id}" });

        if (request.FullName != null)
            user.FullName = request.FullName;

        if (request.Role != null)
            user.Role = request.Role;

        if (request.IsActive.HasValue)
            user.IsActive = request.IsActive.Value;

        if (!string.IsNullOrWhiteSpace(request.Password))
        {
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);
            user.LastPasswordChangedAtUtc = DateTime.UtcNow;
            user.TokenVersion++;
        }

        if (request.EmployeeId.HasValue)
        {
            if (request.EmployeeId.Value > 0)
            {
                if (!await _context.Employees.AnyAsync(e => e.EmployeeId == request.EmployeeId))
                    return BadRequest(new { message = $"EmployeeID {request.EmployeeId} khong ton tai" });

                user.EmployeeId = request.EmployeeId;
            }
            else
            {
                user.EmployeeId = null;
            }
        }

        await _context.SaveChangesAsync();

        return Ok(new UserResponse
        {
            UserId = user.UserId,
            Username = user.Username,
            FullName = user.FullName,
            Role = user.Role,
            IsActive = user.IsActive,
            CreatedAt = user.CreatedAt,
            EmployeeId = user.EmployeeId,
            MfaEnabled = user.MfaEnabled,
            MfaRequired = _authService.RequiresMfa(user),
            LastLoginAtUtc = user.LastLoginAtUtc
        });
    }

    [HttpPost("{id}/mfa/reset")]
    public async Task<IActionResult> ResetMfa(int id)
    {
        var user = await _context.AppUsers.FindAsync(id);
        if (user == null)
            return NotFound(new { message = $"Khong tim thay tai khoan ID {id}" });

        user.MfaEnabled = false;
        user.MfaSecretProtected = null;
        user.MfaConfiguredAtUtc = null;
        user.TokenVersion++;

        var activeTokens = await _context.UserRefreshTokens
            .Where(t => t.UserId == id && t.RevokedAtUtc == null && t.ExpiresAtUtc > DateTime.UtcNow)
            .ToListAsync();

        foreach (var token in activeTokens)
        {
            token.RevokedAtUtc = DateTime.UtcNow;
            token.RevocationReason = "MFA reset";
        }

        await _context.SaveChangesAsync();
        return Ok(new { message = "Da dat lai MFA. Tai khoan se thiet lap lai o lan dang nhap tiep theo." });
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var user = await _context.AppUsers.FindAsync(id);
        if (user == null)
            return NotFound(new { message = $"Khong tim thay tai khoan ID {id}" });

        var currentUserIdClaim = User.FindFirstValue(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub);
        if (currentUserIdClaim != null && int.TryParse(currentUserIdClaim, out var currentUserId) && currentUserId == id)
            return BadRequest(new { message = "Khong the xoa tai khoan dang dang nhap" });

        _context.AppUsers.Remove(user);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    private static string NormalizeUsernameInvariant(string username) =>
        username.Trim().ToUpperInvariant();
}

