using System.Security.Claims;
using API.Data;
using API.Models;
using Microsoft.EntityFrameworkCore;

namespace API.Services;

public class UserOperationalScopeService
{
    public sealed record TaskAccessDefinition(string TaskKey, string Label, string[] DefaultRoles, string[] Routes);
    public sealed record RoleTaskPermissionAssignment(string Role, string TaskKey, bool IsAllowed);

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

    public static readonly string[] SupportedRoles = ["Admin", "QuanLy", "BaoVe", "LeTan", "NhanSu", "NhanVien"];

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

    public static IReadOnlyDictionary<string, string[]> BuildStaticTasksByRole()
    {
        return SupportedRoles.ToDictionary(
            role => role,
            role => TaskCatalog
                .Where(item => item.DefaultRoles.Contains(role, StringComparer.OrdinalIgnoreCase))
                .Select(item => item.TaskKey)
                .OrderBy(item => item)
                .ToArray(),
            StringComparer.OrdinalIgnoreCase);
    }

    public async Task<IReadOnlyDictionary<string, string[]>> GetTasksByRoleAsync(CancellationToken cancellationToken = default)
    {
        var assignments = await _context.RoleOperationalPermissions
            .AsNoTracking()
            .OrderBy(item => item.Role)
            .ThenBy(item => item.TaskKey)
            .ToListAsync(cancellationToken);

        if (assignments.Count == 0)
            return BuildStaticTasksByRole();

        return SupportedRoles.ToDictionary(
            role => role,
            role => assignments
                .Where(item => string.Equals(item.Role, role, StringComparison.OrdinalIgnoreCase) && item.IsAllowed)
                .Select(item => item.TaskKey)
                .OrderBy(item => item)
                .ToArray(),
            StringComparer.OrdinalIgnoreCase);
    }

    public async Task<IReadOnlyList<string>> GetDefaultTaskKeysForRoleAsync(string? role, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(role))
            return [];

        var tasksByRole = await GetTasksByRoleAsync(cancellationToken);
        return tasksByRole.TryGetValue(role, out var taskKeys)
            ? taskKeys
            : [];
    }

    public async Task ReplaceRolePermissionsAsync(
        IReadOnlyCollection<RoleTaskPermissionAssignment> assignments,
        int? updatedByUserId,
        CancellationToken cancellationToken = default)
    {
        var allowedRoles = SupportedRoles.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var allowedTasks = TaskCatalog
            .Select(item => item.TaskKey)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var item in assignments)
        {
            if (string.IsNullOrWhiteSpace(item.Role) || !allowedRoles.Contains(item.Role))
                throw new ArgumentException($"Role '{item.Role}' khong hop le.", nameof(assignments));

            if (string.IsNullOrWhiteSpace(item.TaskKey) || !allowedTasks.Contains(item.TaskKey))
                throw new ArgumentException($"TaskKey '{item.TaskKey}' khong hop le.", nameof(assignments));
        }

        var existing = await _context.RoleOperationalPermissions.ToListAsync(cancellationToken);
        _context.RoleOperationalPermissions.RemoveRange(existing);

        var normalizedAssignments = assignments
            .Select(item => new RoleTaskPermissionAssignment(item.Role.Trim(), item.TaskKey.Trim(), item.IsAllowed))
            .GroupBy(item => $"{item.Role}\u001f{item.TaskKey}", StringComparer.OrdinalIgnoreCase)
            .Select(group => group.Last())
            .ToList();

        if (normalizedAssignments.Count > 0)
        {
            var timestamp = DateTime.UtcNow;
            _context.RoleOperationalPermissions.AddRange(normalizedAssignments.Select(item => new RoleOperationalPermission
            {
                Role = item.Role.Trim(),
                TaskKey = item.TaskKey.Trim(),
                IsAllowed = item.IsAllowed,
                UpdatedAtUtc = timestamp,
                UpdatedByUserId = updatedByUserId
            }));
        }

        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<List<string>> GetEffectiveTaskKeysAsync(int userId, string? role, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var defaultTasks = new HashSet<string>(await GetDefaultTaskKeysForRoleAsync(role, cancellationToken), StringComparer.OrdinalIgnoreCase);

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
