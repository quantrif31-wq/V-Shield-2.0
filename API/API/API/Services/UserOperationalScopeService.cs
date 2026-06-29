using System.Security.Claims;
using API.Data;
using API.Models;
using Microsoft.EntityFrameworkCore;

namespace API.Services;

public class UserOperationalScopeService
{
    public const string TaskMonitoring = "monitoring";
    public const string TaskGateTransit = "gate-transit";
    public const string TaskQrAccess = "qr-access";
    public const string TaskParking = "parking";
    public const string TaskRestrictedZone = "restricted-zone";
    public const string TaskReception = "reception";
    public const string TaskGuestSupport = "guest-support";
    public const string TaskLostFound = "lost-found";
    public const string TaskReports = "reports";
    public const string TaskApprovals = "approvals";
    public const string TaskMetadata = "metadata";

    public static readonly IReadOnlyDictionary<string, string[]> TasksByRole = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
    {
        ["Admin"] = [],
        ["BaoVe"] = [TaskMonitoring, TaskGateTransit, TaskQrAccess, TaskParking, TaskRestrictedZone, TaskLostFound],
        ["LeTan"] = [TaskReception, TaskGuestSupport, TaskLostFound],
        ["QuanLy"] = [TaskReports, TaskApprovals, TaskMetadata]
    };

    private readonly ApplicationDbContext _context;

    public UserOperationalScopeService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<bool> HasScopedAssignmentsAsync(int userId, CancellationToken cancellationToken = default)
    {
        return await _context.UserOperationalScopes
            .AsNoTracking()
            .AnyAsync(scope => scope.UserId == userId, cancellationToken);
    }

    public async Task<List<string>> GetActiveTaskKeysAsync(int userId, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        return await _context.UserOperationalScopes
            .AsNoTracking()
            .Where(scope =>
                scope.UserId == userId &&
                scope.ValidFromUtc <= now &&
                (!scope.ValidToUtc.HasValue || scope.ValidToUtc >= now) &&
                (scope.CanView || scope.CanManage))
            .Select(scope => scope.TaskKey)
            .Distinct()
            .OrderBy(scope => scope)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> CanAccessAsync(
        ClaimsPrincipal principal,
        string taskKey,
        int? siteId = null,
        int? gateId = null,
        int? laneId = null,
        int? securityZoneId = null,
        bool requireManage = false,
        CancellationToken cancellationToken = default)
    {
        var role = principal.FindFirstValue(ClaimTypes.Role);
        if (string.Equals(role, "Admin", StringComparison.OrdinalIgnoreCase))
            return true;

        var userIdClaim = principal.FindFirstValue(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub);
        if (!int.TryParse(userIdClaim, out var userId))
            return false;

        var hasAssignments = await HasScopedAssignmentsAsync(userId, cancellationToken);
        if (!hasAssignments)
            return true;

        var now = DateTime.UtcNow;
        var scopes = await _context.UserOperationalScopes
            .AsNoTracking()
            .Where(scope =>
                scope.UserId == userId &&
                scope.TaskKey == taskKey &&
                scope.ValidFromUtc <= now &&
                (!scope.ValidToUtc.HasValue || scope.ValidToUtc >= now) &&
                (requireManage ? scope.CanManage : (scope.CanView || scope.CanManage)))
            .ToListAsync(cancellationToken);

        if (scopes.Count == 0)
            return false;

        return scopes.Any(scope => MatchesScope(scope, siteId, gateId, laneId, securityZoneId));
    }

    private static bool MatchesScope(
        UserOperationalScope scope,
        int? siteId,
        int? gateId,
        int? laneId,
        int? securityZoneId)
    {
        if (!siteId.HasValue && !gateId.HasValue && !laneId.HasValue && !securityZoneId.HasValue)
        {
            return !scope.SiteId.HasValue &&
                   !scope.GateId.HasValue &&
                   !scope.LaneId.HasValue &&
                   !scope.SecurityZoneId.HasValue;
        }

        if (scope.SiteId.HasValue && scope.SiteId != siteId)
            return false;
        if (scope.GateId.HasValue && scope.GateId != gateId)
            return false;
        if (scope.LaneId.HasValue && scope.LaneId != laneId)
            return false;
        if (scope.SecurityZoneId.HasValue && scope.SecurityZoneId != securityZoneId)
            return false;

        return true;
    }
}
