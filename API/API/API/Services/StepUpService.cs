using API.Data;
using API.DTOs;
using API.Models;
using Microsoft.EntityFrameworkCore;

namespace API.Services;

public class StepUpService : IStepUpService
{
    private static readonly TimeSpan SessionTtl = TimeSpan.FromMinutes(10);
    private readonly ApplicationDbContext _context;
    private readonly TotpService _totpService;

    public StepUpService(ApplicationDbContext context, TotpService totpService)
    {
        _context = context;
        _totpService = totpService;
    }

    public async Task<StepUpSessionResponse?> StartAsync(
        int userId,
        string action,
        string? reason,
        string? ipAddress,
        string? userAgent)
    {
        var userExists = await _context.AppUsers.AnyAsync(user => user.UserId == userId && user.IsActive);
        if (!userExists)
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
        return ToResponse(session, now, "Step-up challenge created.");
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

        var passwordAccepted = !string.IsNullOrWhiteSpace(password) &&
                               BCrypt.Net.BCrypt.Verify(password, user.PasswordHash);
        var mfaAccepted = !string.IsNullOrWhiteSpace(user.MfaSecretProtected) &&
                          _totpService.VerifyCode(user.MfaSecretProtected, mfaCode);

        if (!passwordAccepted && !mfaAccepted)
        {
            session.Status = "Failed";
            session.RevokedAtUtc = now;
            await _context.SaveChangesAsync();
            return null;
        }

        session.Status = "Verified";
        session.VerifiedAtUtc = now;
        session.ExpiresAtUtc = now.Add(SessionTtl);
        await _context.SaveChangesAsync();
        return ToResponse(session, now, "Privileged action session verified.");
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

        return session == null ? null : ToResponse(session, now, null);
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

    private static string NormalizeAction(string? action) =>
        string.IsNullOrWhiteSpace(action) ? PrivilegedActions.All : action.Trim();

    private static StepUpSessionResponse ToResponse(PrivilegedActionSession session, DateTime now, string? message) => new()
    {
        SessionId = session.PrivilegedActionSessionId,
        Action = session.Action,
        Status = session.Status,
        ChallengeNonce = session.ChallengeNonce,
        CreatedAtUtc = session.CreatedAtUtc,
        VerifiedAtUtc = session.VerifiedAtUtc,
        ExpiresAtUtc = session.ExpiresAtUtc,
        Active = session.Status == "Verified" && session.RevokedAtUtc == null && session.ExpiresAtUtc > now,
        Message = message
    };
}
