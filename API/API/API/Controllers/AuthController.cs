using System.Security.Claims;
using API.Data;
using API.DTOs;
using API.Middleware;
using API.Models;
using API.Services;
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
    private readonly Services.IStepUpService _stepUpService;
    private readonly ApplicationDbContext _context;
    private readonly UserOperationalScopeService _scopeService;

    public AuthController(
        Services.IAuthenticationService AuthenticationService,
        Services.IStepUpService stepUpService,
        ApplicationDbContext context,
        UserOperationalScopeService scopeService)
    {
        _authService = AuthenticationService;
        _stepUpService = stepUpService;
        _context = context;
        _scopeService = scopeService;
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

        var hasOperationalScopeAssignments = await _scopeService.HasScopedAssignmentsAsync(user.UserId);
        var operationalTaskKeys = await _scopeService.GetEffectiveTaskKeysAsync(user.UserId, user.Role);

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
            RequiresPasswordChange = _authService.RequiresPasswordChange(user),
            LastLoginAtUtc = user.LastLoginAtUtc,
            HasOperationalScopeAssignments = hasOperationalScopeAssignments,
            OperationalTaskKeys = operationalTaskKeys
        });
    }

    /// <summary>Đổi mật khẩu của người dùng đang đăng nhập (bắt buộc sau khi kích hoạt MFA lần đầu)</summary>
    [HttpPost("change-password")]
    [Authorize]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var userId = GetCurrentUserId();
        if (!userId.HasValue)
            return Unauthorized();

        var result = await _authService.ChangePasswordAsync(userId.Value, request.CurrentPassword, request.NewPassword);
        if (result == null)
            return NotFound(new { message = "Người dùng không tồn tại hoặc đã bị vô hiệu hóa." });

        if (!result.Success)
            return BadRequest(new { message = result.Message ?? "Không thể đổi mật khẩu." });

        return Ok(new { message = "Đổi mật khẩu thành công." });
    }

    [HttpPost("step-up/start")]
    [Authorize]
    public async Task<IActionResult> StartStepUp([FromBody] StepUpStartRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var userId = GetCurrentUserId();
        if (!userId.HasValue)
            return Unauthorized();

        var session = await _stepUpService.StartAsync(
            userId.Value,
            request.Action,
            request.Reason,
            HttpContext.Connection.RemoteIpAddress?.ToString(),
            Request.Headers.UserAgent.ToString());

        return session == null ? Unauthorized() : Ok(session);
    }

    [HttpPost("step-up/verify")]
    [Authorize]
    public async Task<IActionResult> VerifyStepUp([FromBody] StepUpVerifyRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var userId = GetCurrentUserId();
        if (!userId.HasValue)
            return Unauthorized();

        var session = await _stepUpService.VerifyAsync(userId.Value, request.SessionId, request.Password, request.MfaCode);
        return session == null
            ? Unauthorized(new { message = "Xác thực tăng cường thất bại." })
            : Ok(session);
    }

    [HttpGet("step-up/status")]
    [Authorize]
    public async Task<IActionResult> GetStepUpStatus([FromQuery] string? action, [FromQuery] long? sessionId)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue)
            return Unauthorized();

        var session = await _stepUpService.GetStatusAsync(userId.Value, action, sessionId);
        return Ok(session ?? new StepUpSessionResponse
        {
            Status = "None",
            Action = action ?? "AllPrivilegedActions",
            Active = false
        });
    }

    [HttpPost("mfa/recovery-codes")]
    [Authorize]
    [RequireStepUp(PrivilegedActions.UserAdministration)]
    public async Task<IActionResult> GenerateMfaRecoveryCodes([FromBody] MfaRecoveryCodeRequest request)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue)
            return Unauthorized();

        var response = await _authService.GenerateRecoveryCodesAsync(userId.Value, request.Count, userId.Value);
        return response == null ? NotFound(new { message = "Không tìm thấy người dùng." }) : Ok(response);
    }

    private int? GetCurrentUserId()
    {
        var userIdClaim = User.FindFirstValue(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub);
        return int.TryParse(userIdClaim, out var userId) ? userId : null;
    }
}

