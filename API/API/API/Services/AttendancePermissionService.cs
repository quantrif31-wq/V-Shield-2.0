using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using API.Data;
using Microsoft.EntityFrameworkCore;

namespace API.Services;

public interface IAttendancePermissionService
{
    int? GetCurrentUserId(ClaimsPrincipal user);
    int? GetCurrentEmployeeId(ClaimsPrincipal user);
    string? GetCurrentRole(ClaimsPrincipal user);
    bool IsAdmin(ClaimsPrincipal user);
    bool IsSecurity(ClaimsPrincipal user);
    Task<bool> IsManagerAsync(ClaimsPrincipal user, CancellationToken cancellationToken = default);
    Task<int?> GetUserDepartmentIdAsync(ClaimsPrincipal user, CancellationToken cancellationToken = default);
    Task<bool> CanManageEmployeeAsync(ClaimsPrincipal user, int targetEmployeeId, CancellationToken cancellationToken = default);
}

public class AttendancePermissionService : IAttendancePermissionService
{
    private readonly ApplicationDbContext _context;

    public AttendancePermissionService(ApplicationDbContext context)
    {
        _context = context;
    }

    public int? GetCurrentUserId(ClaimsPrincipal user)
    {
        var raw = user.FindFirstValue(JwtRegisteredClaimNames.Sub);
        return int.TryParse(raw, out var id) ? id : null;
    }

    public int? GetCurrentEmployeeId(ClaimsPrincipal user)
    {
        var raw = user.FindFirstValue("employeeId");
        return int.TryParse(raw, out var id) ? id : null;
    }

    public string? GetCurrentRole(ClaimsPrincipal user) =>
        user.FindFirstValue(ClaimTypes.Role);

    public bool IsAdmin(ClaimsPrincipal user) =>
        string.Equals(GetCurrentRole(user), "Admin", StringComparison.OrdinalIgnoreCase);

    public bool IsSecurity(ClaimsPrincipal user) =>
        string.Equals(GetCurrentRole(user), "BaoVe", StringComparison.OrdinalIgnoreCase);

    public async Task<bool> IsManagerAsync(ClaimsPrincipal user, CancellationToken cancellationToken = default)
    {
        if (IsAdmin(user)) return true;

        var role = GetCurrentRole(user);
        if (!string.Equals(role, "Staff", StringComparison.OrdinalIgnoreCase))
            return false;

        var employeeId = GetCurrentEmployeeId(user);
        if (!employeeId.HasValue) return false;

        var positionName = await _context.Employees
            .AsNoTracking()
            .Where(e => e.EmployeeId == employeeId.Value)
            .Select(e => e.Position != null ? e.Position.Name : null)
            .FirstOrDefaultAsync(cancellationToken);

        if (string.IsNullOrWhiteSpace(positionName))
            return false;

        var normalized = positionName.Trim().ToLowerInvariant();
        var managerKeywords = new[] { "truong", "trưởng", "manager", "lead", "giam sat", "giám sát", "supervisor" };
        return managerKeywords.Any(keyword => normalized.Contains(keyword));
    }

    public async Task<int?> GetUserDepartmentIdAsync(ClaimsPrincipal user, CancellationToken cancellationToken = default)
    {
        var employeeId = GetCurrentEmployeeId(user);
        if (!employeeId.HasValue) return null;

        return await _context.Employees
            .AsNoTracking()
            .Where(e => e.EmployeeId == employeeId.Value)
            .Select(e => e.DepartmentId)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<bool> CanManageEmployeeAsync(ClaimsPrincipal user, int targetEmployeeId, CancellationToken cancellationToken = default)
    {
        if (IsAdmin(user)) return true;
        if (!await IsManagerAsync(user, cancellationToken)) return false;

        var managerDepartmentId = await GetUserDepartmentIdAsync(user, cancellationToken);
        if (!managerDepartmentId.HasValue) return false;

        var targetDepartmentId = await _context.Employees
            .AsNoTracking()
            .Where(e => e.EmployeeId == targetEmployeeId)
            .Select(e => e.DepartmentId)
            .FirstOrDefaultAsync(cancellationToken);

        return managerDepartmentId == targetDepartmentId;
    }
}

