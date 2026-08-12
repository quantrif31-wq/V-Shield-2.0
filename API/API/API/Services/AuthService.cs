using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using API.Data;
using API.DTOs;
using API.Models;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.IdentityModel.Tokens;

namespace API.Services;

public class AuthenticationService : IAuthenticationService
{
    private const int MaxFailedAttempts = 5;
    private static readonly TimeSpan LockoutDuration = TimeSpan.FromMinutes(15);
    private const string LockoutCachePrefix = "auth:lockout:";
    private const string FailureCachePrefix = "auth:failures:";

    private readonly ApplicationDbContext _context;
    private readonly IConfiguration _config;
    private readonly IMemoryCache _cache;
    private readonly TotpService _totpService;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly UserOperationalScopeService _scopeService;

    public AuthenticationService(
        ApplicationDbContext context,
        IConfiguration config,
        IMemoryCache cache,
        TotpService totpService,
        IHttpContextAccessor httpContextAccessor,
        UserOperationalScopeService scopeService)
    {
        _context = context;
        _config = config;
        _cache = cache;
        _totpService = totpService;
        _httpContextAccessor = httpContextAccessor;
        _scopeService = scopeService;
    }

    public async Task<LoginResponse?> LoginAsync(LoginRequest request)
    {
        var normalizedUsername = NormalizeUsername(request.Username);
        if (string.IsNullOrEmpty(normalizedUsername))
            return null;

        if (IsLoginTemporarilyLocked(normalizedUsername))
            return null;

        var user = await _context.AppUsers
            .FirstOrDefaultAsync(u => u.IsActive && u.Username.Trim().ToUpper() == normalizedUsername);

        if (user == null)
        {
            RegisterFailedAttempt(normalizedUsername);
            return null;
        }

        if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
        {
            RegisterFailedAttempt(normalizedUsername);
            return null;
        }

        if (RequiresMfa(user))
        {
            var mfaResponse = await HandleMfaAsync(user, request.MfaCode);
            if (mfaResponse != null)
            {
                ResetFailedAttempts(normalizedUsername);
                return mfaResponse;
            }
        }

        ResetFailedAttempts(normalizedUsername);
        return await IssueSessionAsync(user);
    }

    public async Task<LoginResponse?> RefreshAsync(string refreshToken)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
            return null;

        var tokenHash = HashToken(refreshToken);
        var storedToken = await _context.UserRefreshTokens
            .Include(t => t.User)
            .FirstOrDefaultAsync(t => t.TokenHash == tokenHash);

        if (storedToken == null ||
            storedToken.RevokedAtUtc != null ||
            storedToken.ExpiresAtUtc <= DateTime.UtcNow ||
            !storedToken.User.IsActive)
        {
            return null;
        }

        var replacementRefreshToken = GenerateRefreshToken();
        var replacementHash = HashToken(replacementRefreshToken);
        storedToken.RevokedAtUtc = DateTime.UtcNow;
        storedToken.RevocationReason = "Rotated";
        storedToken.ReplacedByTokenHash = replacementHash;

        return await IssueSessionAsync(storedToken.User, replacementRefreshToken, replacementHash);
    }

    public async Task LogoutAsync(int userId, string? refreshToken)
    {
        var user = await _context.AppUsers.FindAsync(userId);
        if (user == null)
            return;

        user.TokenVersion++;

        if (!string.IsNullOrWhiteSpace(refreshToken))
        {
            var tokenHash = HashToken(refreshToken);
            var storedToken = await _context.UserRefreshTokens
                .FirstOrDefaultAsync(t => t.UserId == userId && t.TokenHash == tokenHash && t.RevokedAtUtc == null);

            if (storedToken != null)
            {
                storedToken.RevokedAtUtc = DateTime.UtcNow;
                storedToken.RevocationReason = "Logout";
            }
        }
        else
        {
            var activeTokens = await _context.UserRefreshTokens
                .Where(t => t.UserId == userId && t.RevokedAtUtc == null && t.ExpiresAtUtc > DateTime.UtcNow)
                .ToListAsync();

            foreach (var token in activeTokens)
            {
                token.RevokedAtUtc = DateTime.UtcNow;
                token.RevocationReason = "Logout";
            }
        }

        await _context.SaveChangesAsync();
    }

    public async Task<bool> ValidateAccessTokenVersionAsync(int userId, int tokenVersion)
    {
        return await _context.AppUsers
            .AsNoTracking()
            .AnyAsync(u => u.UserId == userId && u.IsActive && u.TokenVersion == tokenVersion);
    }

    public bool IsLoginTemporarilyLocked(string? username)
    {
        var normalizedUsername = NormalizeUsername(username);
        if (string.IsNullOrEmpty(normalizedUsername))
            return false;

        return _cache.TryGetValue(GetLockoutCacheKey(normalizedUsername), out _);
    }

    public bool RequiresMfa(AppUser user)
    {
        if (_config.GetValue("Authentication:DemoBypassMfa", false) &&
            _config.GetValue("DemoData:Enabled", false))
            return false;

        var configuredRoles = _config.GetSection("Authentication:RequireMfaForRoles").Get<string[]>();
        var roles = configuredRoles is { Length: > 0 }
            ? configuredRoles
            : new[] { "Admin", "BaoVe" };

        return roles.Any(role => string.Equals(role, user.Role, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Bắt buộc đổi mật khẩu sau khi tài khoản kích hoạt MFA lần đầu:
    /// đúng khi mật khẩu chưa từng được đổi sau thời điểm MFA được định cấu hình.
    /// Sau khi người dùng đổi mật khẩu (LastPasswordChangedAtUtc mới hơn), cờ tự được gỡ — chỉ áp dụng lần đầu.
    /// </summary>
    public bool RequiresPasswordChange(AppUser user)
    {
        if (!user.MfaEnabled || !user.MfaConfiguredAtUtc.HasValue)
            return false;

        return !user.LastPasswordChangedAtUtc.HasValue || user.LastPasswordChangedAtUtc < user.MfaConfiguredAtUtc;
    }

    public async Task<ChangePasswordResult?> ChangePasswordAsync(int userId, string? currentPassword, string? newPassword)
    {
        if (string.IsNullOrWhiteSpace(currentPassword) || string.IsNullOrWhiteSpace(newPassword))
        {
            return new ChangePasswordResult { Success = false, Message = "Vui lòng nhập đầy đủ mật khẩu hiện tại và mật khẩu mới." };
        }

        var user = await _context.AppUsers.FindAsync(userId);
        if (user == null || !user.IsActive)
            return null;

        if (!BCrypt.Net.BCrypt.Verify(currentPassword, user.PasswordHash))
        {
            return new ChangePasswordResult { Success = false, Message = "Mật khẩu hiện tại không chính xác." };
        }

        if (newPassword.Length < 6)
        {
            return new ChangePasswordResult { Success = false, Message = "Mật khẩu mới phải có ít nhất 6 ký tự." };
        }

        if (BCrypt.Net.BCrypt.Verify(newPassword, user.PasswordHash))
        {
            return new ChangePasswordResult { Success = false, Message = "Mật khẩu mới phải khác mật khẩu hiện tại." };
        }

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
        user.LastPasswordChangedAtUtc = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return new ChangePasswordResult { Success = true };
    }

    private async Task<LoginResponse?> HandleMfaAsync(AppUser user, string? submittedCode)
    {
        if (string.IsNullOrWhiteSpace(user.MfaSecretProtected))
        {
            var secret = _totpService.GenerateSecret();
            user.MfaSecretProtected = _totpService.ProtectSecret(secret);
            user.MfaEnabled = false;
            await _context.SaveChangesAsync();

            return BuildMfaSetupResponse(user, secret);
        }

        bool isValidCode;
        try
        {
            isValidCode = _totpService.VerifyCode(user.MfaSecretProtected, submittedCode);
        }
        catch (CryptographicException)
        {
            var rotatedSecret = _totpService.GenerateSecret();
            user.MfaSecretProtected = _totpService.ProtectSecret(rotatedSecret);
            user.MfaEnabled = false;
            user.MfaConfiguredAtUtc = null;
            await _context.SaveChangesAsync();

            return BuildMfaSetupResponse(user, rotatedSecret);
        }

        if (!isValidCode)
        {
            if (user.MfaEnabled && await TryConsumeRecoveryCodeAsync(user.UserId, submittedCode))
                return null;

            string? secret = null;
            if (!user.MfaEnabled)
            {
                try
                {
                    secret = _totpService.UnprotectSecret(user.MfaSecretProtected);
                }
                catch (CryptographicException)
                {
                    var rotatedSecret = _totpService.GenerateSecret();
                    user.MfaSecretProtected = _totpService.ProtectSecret(rotatedSecret);
                    user.MfaConfiguredAtUtc = null;
                    await _context.SaveChangesAsync();
                    secret = rotatedSecret;
                }
            }

            return user.MfaEnabled
                ? BuildMfaRequiredResponse(user)
                : BuildMfaSetupResponse(user, secret);
        }

        if (!user.MfaEnabled)
        {
            user.MfaEnabled = true;
            user.MfaConfiguredAtUtc = DateTime.UtcNow;
        }

        return null;
    }

    public async Task<MfaRecoveryCodeResponse?> GenerateRecoveryCodesAsync(int userId, int requestedCount, int? createdByUserId)
    {
        var user = await _context.AppUsers.FindAsync(userId);
        if (user == null || !user.IsActive)
            return null;

        var count = Math.Clamp(requestedCount, 4, 12);
        var now = DateTime.UtcNow;
        var expiresAt = now.AddDays(180);

        var activeCodes = await _context.MfaRecoveryCodes
            .Where(code => code.UserId == userId && code.UsedAtUtc == null && code.ExpiresAtUtc > now)
            .ToListAsync();
        foreach (var code in activeCodes)
        {
            code.UsedAtUtc = now;
        }

        var plainCodes = new List<string>();
        for (var i = 0; i < count; i++)
        {
            var plainCode = GenerateRecoveryCode();
            plainCodes.Add(plainCode);
            _context.MfaRecoveryCodes.Add(new MfaRecoveryCode
            {
                UserId = userId,
                CodeHash = HashToken(plainCode),
                CreatedAtUtc = now,
                ExpiresAtUtc = expiresAt,
                CreatedByUserId = createdByUserId
            });
        }

        await _context.SaveChangesAsync();
        return new MfaRecoveryCodeResponse
        {
            UserId = userId,
            ExpiresAtUtc = expiresAt,
            Codes = plainCodes
        };
    }

    private async Task<bool> TryConsumeRecoveryCodeAsync(int userId, string? submittedCode)
    {
        if (string.IsNullOrWhiteSpace(submittedCode))
            return false;

        var hash = HashToken(submittedCode.Trim());
        var now = DateTime.UtcNow;
        var code = await _context.MfaRecoveryCodes.FirstOrDefaultAsync(item =>
            item.UserId == userId &&
            item.CodeHash == hash &&
            item.UsedAtUtc == null &&
            item.ExpiresAtUtc > now);

        if (code == null)
            return false;

        code.UsedAtUtc = now;
        await _context.SaveChangesAsync();
        return true;
    }

    private async Task<LoginResponse> IssueSessionAsync(AppUser user, string? refreshToken = null, string? refreshTokenHash = null)
    {
        var now = DateTime.UtcNow;
        var jwtSettings = _config.GetSection("JwtSettings");
        var secret = ResolveJwtSecret(jwtSettings);
        var issuer = jwtSettings["Issuer"]!;
        var audience = jwtSettings["Audience"]!;
        var expiresInMinutes = int.Parse(jwtSettings["ExpiresInMinutes"]!);
        var refreshTokenDays = int.TryParse(jwtSettings["RefreshTokenDays"], out var days) ? days : 7;

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var expiresAt = now.AddMinutes(expiresInMinutes);
        var jwtId = Guid.NewGuid().ToString("N");

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.UserId.ToString()),
            new Claim(JwtRegisteredClaimNames.UniqueName, user.Username),
            new Claim(ClaimTypes.Role, user.Role),
            new Claim("fullName", user.FullName ?? string.Empty),
            new Claim("employeeId", user.EmployeeId?.ToString() ?? string.Empty),
            new Claim("token_version", user.TokenVersion.ToString()),
            new Claim(JwtRegisteredClaimNames.Jti, jwtId)
        };

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: expiresAt,
            signingCredentials: creds
        );

        refreshToken ??= GenerateRefreshToken();
        refreshTokenHash ??= HashToken(refreshToken);
        var refreshExpiresAt = now.AddDays(refreshTokenDays);

        _context.UserRefreshTokens.Add(new UserRefreshToken
        {
            UserId = user.UserId,
            TokenHash = refreshTokenHash,
            JwtId = jwtId,
            CreatedAtUtc = now,
            ExpiresAtUtc = refreshExpiresAt,
            CreatedByIp = _httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString()
        });

        user.LastLoginAtUtc = now;
        await _context.SaveChangesAsync();

        var hasOperationalScopeAssignments = await _scopeService.HasScopedAssignmentsAsync(user.UserId);
        var operationalTaskKeys = await _scopeService.GetEffectiveTaskKeysAsync(user.UserId, user.Role);

        return new LoginResponse
        {
            UserId = user.UserId,
            Token = new JwtSecurityTokenHandler().WriteToken(token),
            RefreshToken = refreshToken,
            Username = user.Username,
            FullName = user.FullName ?? user.Username,
            Role = user.Role,
            EmployeeId = user.EmployeeId,
            ExpiresAt = expiresAt,
            RefreshTokenExpiresAt = refreshExpiresAt,
            RequiresPasswordChange = RequiresPasswordChange(user),
            HasOperationalScopeAssignments = hasOperationalScopeAssignments,
            OperationalTaskKeys = operationalTaskKeys
        };
    }

    private LoginResponse BuildMfaRequiredResponse(AppUser user) => new()
    {
        UserId = user.UserId,
        Username = user.Username,
        FullName = user.FullName ?? user.Username,
        Role = user.Role,
        EmployeeId = user.EmployeeId,
        RequiresMfa = true,
        RequiresMfaSetup = false,
        Message = "Mã xác thực hai lớp là bắt buộc."
    };

    private LoginResponse BuildMfaSetupResponse(AppUser user, string? secret) => new()
    {
        UserId = user.UserId,
        Username = user.Username,
        FullName = user.FullName ?? user.Username,
        Role = user.Role,
        EmployeeId = user.EmployeeId,
        RequiresMfa = true,
        RequiresMfaSetup = true,
        MfaSetupSecret = secret,
        MfaSetupUri = secret == null ? null : _totpService.BuildOtpAuthUri("V-Shield", user.Username, secret),
        Message = "Tài khoản cần thiết lập xác thực hai lớp trước khi vào hệ thống."
    };

    private void RegisterFailedAttempt(string normalizedUsername)
    {
        var failureKey = GetFailureCacheKey(normalizedUsername);
        var attemptCount = _cache.Get<int?>(failureKey) ?? 0;
        attemptCount++;

        _cache.Set(failureKey, attemptCount, new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = LockoutDuration
        });

        if (attemptCount >= MaxFailedAttempts)
        {
            _cache.Set(GetLockoutCacheKey(normalizedUsername), true, new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = LockoutDuration
            });

            _cache.Remove(failureKey);
        }
    }

    private void ResetFailedAttempts(string normalizedUsername)
    {
        _cache.Remove(GetFailureCacheKey(normalizedUsername));
        _cache.Remove(GetLockoutCacheKey(normalizedUsername));
    }

    private static string GetLockoutCacheKey(string normalizedUsername) =>
        $"{LockoutCachePrefix}{normalizedUsername}";

    private static string GetFailureCacheKey(string normalizedUsername) =>
        $"{FailureCachePrefix}{normalizedUsername}";

    private static string NormalizeUsername(string? username) =>
        string.IsNullOrWhiteSpace(username) ? string.Empty : username.Trim().ToUpperInvariant();

    private static string GenerateRefreshToken() =>
        WebEncoders.Base64UrlEncode(RandomNumberGenerator.GetBytes(64));

    private static string GenerateRecoveryCode()
    {
        var raw = Convert.ToHexString(RandomNumberGenerator.GetBytes(6));
        return $"{raw[..4]}-{raw[4..8]}-{raw[8..12]}";
    }

    private static string HashToken(string token) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));

    private static string ResolveJwtSecret(IConfigurationSection jwtSettings)
    {
        var secret = (Environment.GetEnvironmentVariable("VSHIELD_JWT_SECRET") ?? jwtSettings["Secret"] ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(secret) || secret.Length < 32)
        {
            throw new InvalidOperationException("JWT secret must be configured and at least 32 characters long.");
        }

        return secret;
    }
}
