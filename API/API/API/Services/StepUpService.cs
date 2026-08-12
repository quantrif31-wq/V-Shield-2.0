using API.Data;
using API.DTOs;
using API.Models;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;

namespace API.Services;

public class StepUpService : IStepUpService
{
    private static readonly TimeSpan SessionTtl = TimeSpan.FromMinutes(10);
    private readonly ApplicationDbContext _context;
    private readonly TotpService _totpService;
    private readonly IAuthenticationService _authService;

    public StepUpService(ApplicationDbContext context, TotpService totpService, IAuthenticationService authService)
    {
        _context = context;
        _totpService = totpService;
        _authService = authService;
    }

    public async Task<StepUpSessionResponse?> StartAsync(
        int userId,
        string action,
        string? reason,
        string? ipAddress,
        string? userAgent)
    {
        var user = await _context.AppUsers.FirstOrDefaultAsync(item => item.UserId == userId && item.IsActive);
        if (user == null)
            return null;

        var now = DateTime.UtcNow;
        var normalizedAction = NormalizeAction(action);
        var session = new PrivilegedActionSession
        {
            UserId = userId,
            Action = normalizedAction,
            Reason = reason?.Trim(),
            Status = "Pending",
            IpAddress = ipAddress,
            UserAgent = userAgent,
            CreatedAtUtc = now,
            ExpiresAtUtc = now.AddMinutes(2)
        };

        _context.PrivilegedActionSessions.Add(session);
        await _context.SaveChangesAsync();
        return ToResponse(session, user, now, "Bắt đầu xác thực lại thao tác.");
    }

    public async Task<StepUpSessionResponse?> VerifyAsync(int userId, long sessionId, string? password, string? mfaCode)
    {
        var now = DateTime.UtcNow;
        var session = await _context.PrivilegedActionSessions
            .FirstOrDefaultAsync(item =>
                item.PrivilegedActionSessionId == sessionId &&
                item.UserId == userId &&
                item.RevokedAtUtc == null);

        if (session == null || session.ExpiresAtUtc <= now)
            return null;

        var user = await _context.AppUsers.FindAsync(userId);
        if (user == null || !user.IsActive)
            return null;

        // Luôn yêu cầu nhập lại mật khẩu của tài khoản hiện tại
        var passwordAccepted = !string.IsNullOrWhiteSpace(password) &&
                               BCrypt.Net.BCrypt.Verify(password, user.PasswordHash);
        if (!passwordAccepted)
        {
            return await Fail(session, now, "Mật khẩu không đúng.");
        }

        // Bước 2: MFA chỉ bắt buộc khi logic đăng nhập yêu cầu (tôn trọng cấu hình bypass)
        var mfaRequired = _authService.RequiresMfa(user);
        if (mfaRequired)
        {
            if (string.IsNullOrWhiteSpace(user.MfaSecretProtected))
            {
                return await Fail(session, now, "Tài khoản yêu cầu xác thực hai bước nhưng chưa được cấu hình MFA.");
            }

            var mfaAccepted = false;
            if (!string.IsNullOrWhiteSpace(mfaCode))
            {
                try
                {
                    mfaAccepted = _totpService.VerifyCode(user.MfaSecretProtected, mfaCode);
                }
                catch (CryptographicException)
                {
                    mfaAccepted = false;
                }
            }

            if (!mfaAccepted && user.MfaEnabled)
            {
                mfaAccepted = await TryConsumeRecoveryCodeAsync(user.UserId, mfaCode);
            }

            if (!mfaAccepted)
            {
                return await Fail(session, now, "Mã xác thực hai bước không đúng.");
            }
        }

        session.Status = "Verified";
        session.VerifiedAtUtc = now;
        session.ExpiresAtUtc = now.Add(SessionTtl);
        await _context.SaveChangesAsync();
        return ToResponse(session, user, now, mfaRequired
            ? "Đã xác thực mật khẩu và mã xác thực hai bước."
            : "Đã xác thực mật khẩu.");
    }

    public async Task<StepUpSessionResponse?> GetStatusAsync(int userId, string? action, long? sessionId)
    {
        var now = DateTime.UtcNow;
        var query = _context.PrivilegedActionSessions
            .Where(item => item.UserId == userId && item.RevokedAtUtc == null);

        if (sessionId.HasValue)
            query = query.Where(item => item.PrivilegedActionSessionId == sessionId.Value);
        else if (!string.IsNullOrWhiteSpace(action))
        {
            var normalizedAction = NormalizeAction(action);
            query = query.Where(item => item.Action == normalizedAction || item.Action == PrivilegedActions.All);
        }

        var session = await query
            .OrderByDescending(item => item.VerifiedAtUtc ?? item.CreatedAtUtc)
            .FirstOrDefaultAsync();

        if (session == null)
            return null;

        var user = await _context.AppUsers.FindAsync(userId);
        return ToResponse(session, user, now, null);
    }

    public async Task<bool> HasActiveSessionAsync(int userId, string action, long? sessionId)
    {
        var now = DateTime.UtcNow;
        var normalizedAction = NormalizeAction(action);
        var query = _context.PrivilegedActionSessions
            .AsNoTracking()
            .Where(item =>
                item.UserId == userId &&
                item.Status == "Verified" &&
                item.RevokedAtUtc == null &&
                item.ExpiresAtUtc > now &&
                (item.Action == normalizedAction || item.Action == PrivilegedActions.All));

        if (sessionId.HasValue)
            query = query.Where(item => item.PrivilegedActionSessionId == sessionId.Value);

        return await query.AnyAsync();
    }

    private async Task<StepUpSessionResponse?> Fail(PrivilegedActionSession session, DateTime now, string message)
    {
        session.Status = "Failed";
        session.RevokedAtUtc = now;
        _context.PrivilegedActionSessions.Update(session);
        await _context.SaveChangesAsync();
        return null;
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

    private static string HashToken(string value)
    {
        using var sha = System.Security.Cryptography.SHA256.Create();
        return Convert.ToBase64String(sha.ComputeHash(System.Text.Encoding.UTF8.GetBytes(value)));
    }

    private static string NormalizeAction(string? action) =>
        string.IsNullOrWhiteSpace(action) ? PrivilegedActions.All : action.Trim();

    private StepUpSessionResponse ToResponse(PrivilegedActionSession session, AppUser? user, DateTime now, string? message) => new()
    {
        SessionId = session.PrivilegedActionSessionId,
        Action = session.Action,
        Status = session.Status,
        ChallengeNonce = session.ChallengeNonce,
        CreatedAtUtc = session.CreatedAtUtc,
        VerifiedAtUtc = session.VerifiedAtUtc,
        ExpiresAtUtc = session.ExpiresAtUtc,
        Active = session.Status == "Verified" && session.RevokedAtUtc == null && session.ExpiresAtUtc > now,
        RequiresMfa = user != null && user.IsActive && _authService.RequiresMfa(user),
        Message = message
    };
}
