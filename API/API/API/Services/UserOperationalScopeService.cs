using System.Security.Claims;
using API.Data;
using API.Models;
using Microsoft.EntityFrameworkCore;

namespace API.Services;

public class UserOperationalScopeService
{
    public sealed record TaskAccessDefinition(string TaskKey, string Label, string[] DefaultRoles, string[] Routes);

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
    public const string TaskDashboard = "dashboard";
    public const string TaskUserAdministration = "user-admin";
    public const string TaskEmployeeDirectory = "employee-directory";
    public const string TaskAccessLogs = "access-logs";
    public const string TaskDeviceManagement = "device-mgmt";
    public const string TaskEvidenceManagement = "evidence-mgmt";
    public const string TaskSystemConfig = "system-config";
    public const string TaskIdentityManagement = "identity-mgmt";
    public const string TaskContractorManagement = "contractor-mgmt";

    public static readonly IReadOnlyList<TaskAccessDefinition> TaskCatalog =
    [
        new(TaskDashboard, "Dashboard tổng quan", ["Admin", "QuanLy"], ["/dashboard", "/operations-dashboard"]),
        new(TaskMonitoring, "Giám sát an ninh", ["Admin", "BaoVe"], ["/monitoring", "/soc-console", "/incident-map", "/ueba", "/campus-map", "/video-search", "/ai-review-queue", "/correlation-view", "/watchlist", "/exceptions"]),
        new(TaskGateTransit, "Thông hành cổng/làn", ["Admin", "BaoVe"], ["/gate-transit-monitor", "/lane-dashboard"]),
        new(TaskQrAccess, "Quét QR vào cổng", ["Admin", "BaoVe"], ["/qr-access-monitor", "/kiosk", "/dynamic-qr-generator"]),
        new(TaskParking, "Gửi xe/tra xe", ["Admin", "BaoVe"], ["/vehicles", "/parking-kiosk", "/barrier-panel", "/license-plate-security"]),
        new(TaskRestrictedZone, "Khu vực giới hạn", ["Admin", "BaoVe"], ["/access-permission-manager"]),
        new(TaskReception, "Lễ tân/tiếp đón", ["Admin", "LeTan"], ["/reception", "/campus-map", "/kiosk-checkin"]),
        new(TaskGuestSupport, "Hỗ trợ khách", ["Admin", "LeTan"], ["/guest-profiles", "/host-visitor", "/pre-registrations", "/registration-links"]),
        new(TaskLostFound, "Đồ thất lạc", ["Admin", "BaoVe", "LeTan"], ["/lost-found", "/found-items", "/lost-items", "/claim-approval", "/locker-manager"]),
        new(TaskReports, "Báo cáo", ["Admin", "QuanLy"], ["/attendance/reports", "/attendance/records"]),
        new(TaskApprovals, "Phê duyệt", ["Admin", "QuanLy", "NhanSu"], ["/attendance/leave-approvals"]),
        new(TaskMetadata, "Metadata/danh mục", ["Admin", "QuanLy"], ["/site-hierarchy", "/system-catalog", "/attendance/work-schedules", "/attendance/shifts", "/departments-positions", "/import-export-history"]),
        new(TaskEmployeeDirectory, "Hồ sơ nhân viên", ["Admin", "NhanSu"], ["/employees"]),
        new(TaskUserAdministration, "Tài khoản và phân quyền", ["Admin", "NhanSu"], ["/role-permissions", "/users"]),
        new(TaskAccessLogs, "Nhật ký hệ thống", ["Admin", "BaoVe", "QuanLy"], ["/access-logs", "/system-audit-logs"]),
        new(TaskDeviceManagement, "Quản lý thiết bị", ["Admin"], ["/device-management", "/device-topology", "/provisioning-wizard", "/offline-packages", "/device-health", "/simulator-panel"]),
        new(TaskEvidenceManagement, "Quản lý bằng chứng", ["Admin"], ["/evidence-repository", "/export-approval-queue", "/redaction-queue", "/compliance-reports"]),
        new(TaskSystemConfig, "Cấu hình hệ thống", ["Admin", "QuanLy"], ["/settings", "/notification-rules", "/siem-export-status"]),
        new(TaskIdentityManagement, "Đồng bộ danh tính", ["Admin"], ["/identity-management", "/enterprise-security"]),
        new(TaskContractorManagement, "Quản lý nhà thầu", ["Admin"], ["/contractors"])
    ];

    public static readonly IReadOnlyDictionary<string, string[]> TasksByRole = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
    {
        ["Admin"] = TaskCatalog.Where(item => item.DefaultRoles.Contains("Admin", StringComparer.OrdinalIgnoreCase)).Select(item => item.TaskKey).ToArray(),
        ["BaoVe"] = TaskCatalog.Where(item => item.DefaultRoles.Contains("BaoVe", StringComparer.OrdinalIgnoreCase)).Select(item => item.TaskKey).ToArray(),
        ["LeTan"] = TaskCatalog.Where(item => item.DefaultRoles.Contains("LeTan", StringComparer.OrdinalIgnoreCase)).Select(item => item.TaskKey).ToArray(),
        ["QuanLy"] = TaskCatalog.Where(item => item.DefaultRoles.Contains("QuanLy", StringComparer.OrdinalIgnoreCase)).Select(item => item.TaskKey).ToArray(),
        ["NhanSu"] = TaskCatalog.Where(item => item.DefaultRoles.Contains("NhanSu", StringComparer.OrdinalIgnoreCase)).Select(item => item.TaskKey).ToArray(),
        ["NhanVien"] = Array.Empty<string>()
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
        var userRole = await _context.AppUsers
            .AsNoTracking()
            .Where(user => user.UserId == userId)
            .Select(user => user.Role)
            .FirstOrDefaultAsync(cancellationToken);

        if (string.IsNullOrWhiteSpace(userRole))
            return [];

        return await GetEffectiveTaskKeysAsync(userId, userRole, cancellationToken);
    }

    public IReadOnlyList<string> GetDefaultTaskKeysForRole(string? role)
    {
        if (string.IsNullOrWhiteSpace(role))
            return [];

        return TasksByRole.TryGetValue(role, out var taskKeys)
            ? taskKeys.OrderBy(item => item).ToArray()
            : [];
    }

    public async Task<List<string>> GetEffectiveTaskKeysAsync(int userId, string? role, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var defaultTasks = new HashSet<string>(GetDefaultTaskKeysForRole(role), StringComparer.OrdinalIgnoreCase);

        var overrides = await _context.UserOperationalScopes
            .AsNoTracking()
            .Where(scope =>
                scope.UserId == userId &&
                scope.ValidFromUtc <= now &&
                (!scope.ValidToUtc.HasValue || scope.ValidToUtc >= now))
            .Select(scope => new
            {
                scope.TaskKey,
                scope.CanView,
                scope.CanManage,
                scope.SiteId,
                scope.GateId,
                scope.LaneId,
                scope.SecurityZoneId
            })
            .ToListAsync(cancellationToken);

        foreach (var scope in overrides)
        {
            if (string.IsNullOrWhiteSpace(scope.TaskKey))
                continue;

            var isGlobalOverride = !scope.SiteId.HasValue &&
                                   !scope.GateId.HasValue &&
                                   !scope.LaneId.HasValue &&
                                   !scope.SecurityZoneId.HasValue;

            if (!isGlobalOverride)
            {
                if (scope.CanView || scope.CanManage)
                    defaultTasks.Add(scope.TaskKey);
                continue;
            }

            if (scope.CanView || scope.CanManage)
                defaultTasks.Add(scope.TaskKey);
            else
                defaultTasks.Remove(scope.TaskKey);
        }

        return defaultTasks.OrderBy(item => item).ToList();
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

        var effectiveTaskKeys = await GetEffectiveTaskKeysAsync(userId, role, cancellationToken);
        if (!effectiveTaskKeys.Contains(taskKey, StringComparer.OrdinalIgnoreCase))
            return false;

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

        var locationScoped = scopes
            .Where(scope => scope.SiteId.HasValue || scope.GateId.HasValue || scope.LaneId.HasValue || scope.SecurityZoneId.HasValue)
            .ToList();

        if (locationScoped.Count == 0)
            return true;

        return locationScoped.Any(scope => MatchesScope(scope, siteId, gateId, laneId, securityZoneId));
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
