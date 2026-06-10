using System.Security.Claims;
using API.Data;
using API.DTOs;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.EntityFrameworkCore;

namespace API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly Services.IAuthenticationService _authService;
    private readonly ApplicationDbContext _context;

    public AuthController(Services.IAuthenticationService AuthenticationService, ApplicationDbContext context)
    {
        _authService = AuthenticationService;
        _context = context;
    }

    /// <summary>Đăng nhập và nhận JWT token</summary>
    [HttpPost("login")]
    [AllowAnonymous]
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        if (_authService.IsLoginTemporarilyLocked(request.Username))
        {
            return StatusCode(StatusCodes.Status429TooManyRequests, new
            {
                message = "Tài khoản tạm thời bị khóa do đăng nhập sai nhiều lần. Vui lòng thử lại sau."
            });
        }

        var result = await _authService.LoginAsync(request);

        if (result == null)
            return Unauthorized(new { message = "Tên đăng nhập hoặc mật khẩu không đúng" });

        return Ok(result);
    }

    /// <summary>Gia hạn phiên bằng refresh token và xoay token mới</summary>
    [HttpPost("refresh")]
    [AllowAnonymous]
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> Refresh([FromBody] RefreshTokenRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var result = await _authService.RefreshAsync(request.RefreshToken);
        if (result == null)
            return Unauthorized(new { message = "Phiên đăng nhập không hợp lệ hoặc đã hết hạn" });

        return Ok(result);
    }

    /// <summary>Đăng xuất khỏi phiên hiện tại (ghi log hệ thống)</summary>
    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout([FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] LogoutRequest? request)
    {
        var userIdClaim = User.FindFirstValue(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub);
        if (userIdClaim != null && int.TryParse(userIdClaim, out var userId))
        {
            await _authService.LogoutAsync(userId, request?.RefreshToken);
        }

        return Ok(new { message = "Đăng xuất thành công" });
    }

    /// <summary>Lấy thông tin người dùng hiện đang đăng nhập</summary>
    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> GetMe()
    {
        var userIdClaim = User.FindFirstValue(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub);
        if (userIdClaim == null || !int.TryParse(userIdClaim, out var userId))
            return Unauthorized();

        var user = await _context.AppUsers.FindAsync(userId);
        if (user == null)
            return NotFound();

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
}

