using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using API.Data;
using API.Models;
using API.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace API.Tests;

public sealed class UserOperationalScopeServiceTests
{
    private static ApplicationDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"scope_{Guid.NewGuid():N}")
            .Options;
        return new ApplicationDbContext(options);
    }

    private static ClaimsPrincipal Principal(int userId, string role) =>
        new(new ClaimsIdentity(new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new Claim(ClaimTypes.Role, role)
        }, "test"));

    private static async Task SeedUserAsync(ApplicationDbContext db, int userId, string role = "BaoVe", bool active = true)
    {
        db.AppUsers.Add(new AppUser { UserId = userId, Username = $"u{userId}", PasswordHash = "x", Role = role, IsActive = active });
        await db.SaveChangesAsync();
    }

    [Fact]
    public async Task HasScopedAssignmentsAsync_TrueAndFalse()
    {
        var db = CreateDb();
        db.UserOperationalScopes.Add(new UserOperationalScope { UserId = 1, TaskKey = UserOperationalScopeService.TaskQrAccess });
        await db.SaveChangesAsync();
        var svc = new UserOperationalScopeService(db);

        Assert.True(await svc.HasScopedAssignmentsAsync(1));
        Assert.False(await svc.HasScopedAssignmentsAsync(2));
    }

    [Fact]
    public async Task GetActiveTaskKeysAsync_NoUser_Empty()
    {
        var db = CreateDb();
        var svc = new UserOperationalScopeService(db);
        Assert.Empty(await svc.GetActiveTaskKeysAsync(999));
    }

    [Fact]
    public async Task GetActiveTaskKeysAsync_BaoVe_IncludesQrAccess()
    {
        var db = CreateDb();
        await SeedUserAsync(db, 1, "BaoVe");
        var svc = new UserOperationalScopeService(db);

        var tasks = await svc.GetActiveTaskKeysAsync(1);

        Assert.Contains(UserOperationalScopeService.TaskQrAccess, tasks);
        Assert.Contains(UserOperationalScopeService.TaskMonitoring, tasks);
    }

    [Fact]
    public void BuildStaticTasksByRole_HasRolesAndTasks()
    {
        var byRole = UserOperationalScopeService.BuildStaticTasksByRole();

        Assert.True(byRole[UserOperationalScopeService.SupportedRoles[0]].Contains(UserOperationalScopeService.TaskDashboard));
        Assert.Contains(UserOperationalScopeService.TaskLostFound, byRole["LeTan"]);
        Assert.DoesNotContain(UserOperationalScopeService.TaskDeviceManagement, byRole["NhanVien"]);
    }

    [Fact]
    public async Task GetTasksByRoleAsync_NoAssignments_UsesStatic()
    {
        var db = CreateDb();
        var svc = new UserOperationalScopeService(db);

        var result = await svc.GetTasksByRoleAsync();

        Assert.Contains("Admin", result.Keys);
        Assert.Contains(UserOperationalScopeService.TaskQrAccess, result["BaoVe"]);
    }

    [Fact]
    public async Task GetTasksByRoleAsync_WithAssignments_ReturnsFiltered()
    {
        var db = CreateDb();
        db.RoleOperationalPermissions.Add(new RoleOperationalPermission
        {
            Role = "BaoVe",
            TaskKey = UserOperationalScopeService.TaskQrAccess,
            IsAllowed = true,
            UpdatedAtUtc = DateTime.UtcNow
        });
        db.RoleOperationalPermissions.Add(new RoleOperationalPermission
        {
            Role = "Admin",
            TaskKey = UserOperationalScopeService.TaskDashboard,
            IsAllowed = false,
            UpdatedAtUtc = DateTime.UtcNow
        });
        await db.SaveChangesAsync();
        var svc = new UserOperationalScopeService(db);

        var result = await svc.GetTasksByRoleAsync();

        Assert.Equal([UserOperationalScopeService.TaskQrAccess], result["BaoVe"]);
        Assert.Empty(result["Admin"]);
    }

    [Fact]
    public async Task GetDefaultTaskKeysForRoleAsync_NullRole_Empty()
    {
        var db = CreateDb();
        var svc = new UserOperationalScopeService(db);
        Assert.Empty(await svc.GetDefaultTaskKeysForRoleAsync(null));
        Assert.Contains(UserOperationalScopeService.TaskParking, await svc.GetDefaultTaskKeysForRoleAsync("BaoVe"));
    }

    [Fact]
    public async Task ReplaceRolePermissionsAsync_InvalidRole_Throws()
    {
        var db = CreateDb();
        var svc = new UserOperationalScopeService(db);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            svc.ReplaceRolePermissionsAsync([new UserOperationalScopeService.RoleTaskPermissionAssignment("Nope", UserOperationalScopeService.TaskQrAccess, true)], 1));
    }

    [Fact]
    public async Task ReplaceRolePermissionsAsync_InvalidTask_Throws()
    {
        var db = CreateDb();
        var svc = new UserOperationalScopeService(db);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            svc.ReplaceRolePermissionsAsync([new UserOperationalScopeService.RoleTaskPermissionAssignment("Admin", "not-a-task", true)], 1));
    }

    [Fact]
    public async Task ReplaceRolePermissionsAsync_ReplacesExisting()
    {
        var db = CreateDb();
        db.RoleOperationalPermissions.Add(new RoleOperationalPermission
        {
            Role = "Admin",
            TaskKey = UserOperationalScopeService.TaskDashboard,
            IsAllowed = true,
            UpdatedAtUtc = DateTime.UtcNow
        });
        await db.SaveChangesAsync();
        var svc = new UserOperationalScopeService(db);

        await svc.ReplaceRolePermissionsAsync(
            [new UserOperationalScopeService.RoleTaskPermissionAssignment("BaoVe", UserOperationalScopeService.TaskQrAccess, true)],
            7);

        Assert.Single(db.RoleOperationalPermissions);
        var row = db.RoleOperationalPermissions.Single();
        Assert.Equal("BaoVe", row.Role);
        Assert.Equal(UserOperationalScopeService.TaskQrAccess, row.TaskKey);
        Assert.Equal(7, row.UpdatedByUserId);
    }

    [Fact]
    public async Task CanAccessAsync_Admin_AlwaysTrue()
    {
        var db = CreateDb();
        var svc = new UserOperationalScopeService(db);

        Assert.True(await svc.CanAccessAsync(Principal(1, "Admin"), UserOperationalScopeService.TaskQrAccess));
    }

    [Fact]
    public async Task CanAccessAsync_NonNumericUserId_False()
    {
        var db = CreateDb();
        var svc = new UserOperationalScopeService(db);
        var principal = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, "not-a-number"),
            new Claim(ClaimTypes.Role, "BaoVe")
        }, "test"));

        Assert.False(await svc.CanAccessAsync(principal, UserOperationalScopeService.TaskParking));
    }

    [Fact]
    public async Task CanAccessAsync_ValidRole_NoScopes_True()
    {
        var db = CreateDb();
        await SeedUserAsync(db, 1, "BaoVe");
        var svc = new UserOperationalScopeService(db);

        Assert.True(await svc.CanAccessAsync(Principal(1, "BaoVe"), UserOperationalScopeService.TaskQrAccess));
    }

    [Fact]
    public async Task CanAccessAsync_TaskNotGranted_False()
    {
        var db = CreateDb();
        await SeedUserAsync(db, 1, "NhanVien");
        var svc = new UserOperationalScopeService(db);

        Assert.False(await svc.CanAccessAsync(Principal(1, "NhanVien"), UserOperationalScopeService.TaskQrAccess));
    }

    [Fact]
    public async Task CanAccessAsync_LocationScope_MustMatchGate()
    {
        var db = CreateDb();
        await SeedUserAsync(db, 1, "BaoVe");
        db.UserOperationalScopes.Add(new UserOperationalScope
        {
            UserId = 1,
            TaskKey = UserOperationalScopeService.TaskQrAccess,
            GateId = 3,
            CanManage = true,
            ValidFromUtc = DateTime.UtcNow.AddMinutes(-5)
        });
        await db.SaveChangesAsync();
        var svc = new UserOperationalScopeService(db);

        Assert.True(await svc.CanAccessAsync(Principal(1, "BaoVe"), UserOperationalScopeService.TaskQrAccess, gateId: 3, requireManage: true));
        Assert.False(await svc.CanAccessAsync(Principal(1, "BaoVe"), UserOperationalScopeService.TaskQrAccess, gateId: 5, requireManage: true));
    }

    [Fact]
    public async Task CanAccessAsync_ExpiredScope_StillGrantsViaDefault()
    {
        var db = CreateDb();
        await SeedUserAsync(db, 1, "BaoVe");
        db.UserOperationalScopes.Add(new UserOperationalScope
        {
            UserId = 1,
            TaskKey = UserOperationalScopeService.TaskQrAccess,
            GateId = 3,
            ValidToUtc = DateTime.UtcNow.AddMinutes(-5)
        });
        await db.SaveChangesAsync();
        var svc = new UserOperationalScopeService(db);

        Assert.True(await svc.CanAccessAsync(Principal(1, "BaoVe"), UserOperationalScopeService.TaskQrAccess, gateId: 9));
    }

    [Fact]
    public async Task GetEffectiveTaskKeysAsync_GlobalDenyOverride_RemovesTask()
    {
        var db = CreateDb();
        await SeedUserAsync(db, 1, "BaoVe");
        db.UserOperationalScopes.Add(new UserOperationalScope
        {
            UserId = 1,
            TaskKey = UserOperationalScopeService.TaskQrAccess,
            CanView = false,
            CanManage = false,
            ValidFromUtc = DateTime.UtcNow.AddMinutes(-5)
        });
        await db.SaveChangesAsync();
        var svc = new UserOperationalScopeService(db);

        var tasks = await svc.GetEffectiveTaskKeysAsync(1, "BaoVe");

        Assert.DoesNotContain(UserOperationalScopeService.TaskQrAccess, tasks);
    }
}