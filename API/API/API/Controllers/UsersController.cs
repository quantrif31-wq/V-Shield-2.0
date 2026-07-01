using System.Security.Claims;
using API.Data;
using API.DTOs;
using API.Middleware;
using API.Models;
using API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
[RequireOperationalTask(UserOperationalScopeService.TaskUserAdministration)]
public class UsersController : ControllerBase
{
    private static readonly HashSet<string> SupportedRoles = new(StringComparer.OrdinalIgnoreCase)
    {
        "Admin",
        "QuanLy",
        "BaoVe",
        "LeTan",
        "NhanVien",
        "NhanSu"
    };

    private readonly ApplicationDbContext _context;
    private readonly Services.IAuthenticationService _authService;
    private readonly UserOperationalScopeService _scopeService;

    public UsersController(ApplicationDbContext context, Services.IAuthenticationService authService, UserOperationalScopeService scopeService)
    {
        _context = context;
        _authService = authService;
        _scopeService = scopeService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var users = await _context.AppUsers
            .OrderBy(u => u.UserId)
            .Select(u => new UserResponse
            {
                UserId = u.UserId,
                Username = u.Username,
                FullName = u.FullName,
                Role = u.Role,
                IsActive = u.IsActive,
                CreatedAt = u.CreatedAt,
                EmployeeId = u.EmployeeId,
                MfaEnabled = u.MfaEnabled,
                MfaRequired = u.Role == "Admin" || u.Role == "BaoVe",
                LastLoginAtUtc = u.LastLoginAtUtc
            })
            .ToListAsync();

        foreach (var user in users)
        {
            user.HasOperationalScopeAssignments = await _scopeService.HasScopedAssignmentsAsync(user.UserId);
            user.OperationalTaskKeys = await _scopeService.GetEffectiveTaskKeysAsync(user.UserId, user.Role);
        }

        return Ok(users);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var user = await _context.AppUsers.FindAsync(id);
        if (user == null)
            return NotFound(new { message = $"Khong tim thay tai khoan ID {id}" });

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
            LastLoginAtUtc = user.LastLoginAtUtc,
            HasOperationalScopeAssignments = await _scopeService.HasScopedAssignmentsAsync(user.UserId),
            OperationalTaskKeys = await _scopeService.GetEffectiveTaskKeysAsync(user.UserId, user.Role)
        });
    }

    [HttpPost]
    [RequireStepUp(PrivilegedActions.UserAdministration)]
    public async Task<IActionResult> Create([FromBody] CreateUserRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var username = request.Username.Trim();
        if (string.IsNullOrWhiteSpace(username))
            return BadRequest(new { message = "Ten dang nhap khong duoc de trong" });

        var normalizedUsername = NormalizeUsernameInvariant(username);

        if (await _context.AppUsers.AnyAsync(u => u.Username.Trim().ToUpper() == normalizedUsername))
            return Conflict(new { message = $"Ten dang nhap '{username}' da ton tai" });

        if (!SupportedRoles.Contains(request.Role))
            return BadRequest(new { message = "Vai tro khong hop le. Chi chap nhan Admin, QuanLy, BaoVe hoac LeTan." });

        if (request.EmployeeId.HasValue && request.EmployeeId.Value > 0)
        {
            if (!await _context.Employees.AnyAsync(e => e.EmployeeId == request.EmployeeId))
                return BadRequest(new { message = $"EmployeeID {request.EmployeeId} khong ton tai" });

            if (await _context.AppUsers.AnyAsync(u => u.EmployeeId == request.EmployeeId))
                return Conflict(new { message = $"Nhan vien {request.EmployeeId} da duoc gan voi mot tai khoan khac." });
        }

        var user = new AppUser
        {
            Username = username,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            FullName = request.FullName,
            Role = request.Role,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            LastPasswordChangedAtUtc = DateTime.UtcNow,
            EmployeeId = (request.EmployeeId.HasValue && request.EmployeeId.Value > 0) ? request.EmployeeId : null
        };

        _context.AppUsers.Add(user);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = user.UserId }, new UserResponse
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

    [HttpPut("{id}")]
    [RequireStepUp(PrivilegedActions.UserAdministration)]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateUserRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var user = await _context.AppUsers.FindAsync(id);
        if (user == null)
            return NotFound(new { message = $"Khong tim thay tai khoan ID {id}" });

        if (request.FullName != null)
            user.FullName = request.FullName;

        if (request.Role != null)
        {
            if (!SupportedRoles.Contains(request.Role))
                return BadRequest(new { message = "Vai tro khong hop le. Chi chap nhan Admin, QuanLy, BaoVe hoac LeTan." });

            user.Role = request.Role;
        }

        if (request.IsActive.HasValue)
            user.IsActive = request.IsActive.Value;

        if (!string.IsNullOrWhiteSpace(request.Password))
        {
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);
            user.LastPasswordChangedAtUtc = DateTime.UtcNow;
            user.TokenVersion++;
        }

        if (request.EmployeeId.HasValue)
        {
            if (request.EmployeeId.Value > 0)
            {
                if (!await _context.Employees.AnyAsync(e => e.EmployeeId == request.EmployeeId))
                    return BadRequest(new { message = $"EmployeeID {request.EmployeeId} khong ton tai" });

                if (await _context.AppUsers.AnyAsync(u => u.UserId != id && u.EmployeeId == request.EmployeeId))
                    return Conflict(new { message = $"Nhan vien {request.EmployeeId} da duoc gan voi mot tai khoan khac." });

                user.EmployeeId = request.EmployeeId;
            }
            else
            {
                user.EmployeeId = null;
            }
        }

        await _context.SaveChangesAsync();

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
            LastLoginAtUtc = user.LastLoginAtUtc,
            HasOperationalScopeAssignments = await _scopeService.HasScopedAssignmentsAsync(user.UserId),
            OperationalTaskKeys = await _scopeService.GetEffectiveTaskKeysAsync(user.UserId, user.Role)
        });
    }

    [HttpGet("scope-reference")]
    public async Task<IActionResult> GetOperationalScopeReference()
    {
        var sites = await _context.Sites
            .AsNoTracking()
            .Where(item => item.IsActive)
            .OrderBy(item => item.Name)
            .Select(item => new { item.SiteId, item.Name, item.Code })
            .ToListAsync();

        var gates = await _context.Gates
            .AsNoTracking()
            .OrderBy(item => item.GateName)
            .Select(item => new { item.GateId, name = item.GateName, item.Location })
            .ToListAsync();

        var lanes = await _context.Lanes
            .AsNoTracking()
            .Where(item => item.IsActive)
            .OrderBy(item => item.Name)
            .Select(item => new { item.LaneId, item.Name, item.GateId, item.SiteId })
            .ToListAsync();

        var zones = await _context.SecurityZones
            .AsNoTracking()
            .Where(item => item.IsActive)
            .OrderBy(item => item.Name)
            .Select(item => new { item.SecurityZoneId, item.Name, item.SiteId, item.SecurityLevel })
            .ToListAsync();

        return Ok(new
        {
            tasksByRole = UserOperationalScopeService.TasksByRole,
            taskCatalog = UserOperationalScopeService.TaskCatalog.Select(item => new
            {
                item.TaskKey,
                item.Label,
                defaultRoles = item.DefaultRoles,
                routes = item.Routes
            }),
            sites,
            gates,
            lanes,
            zones
        });
    }

    [HttpGet("{id:int}/operational-scopes")]
    public async Task<IActionResult> GetOperationalScopes(int id)
    {
        if (!await _context.AppUsers.AnyAsync(user => user.UserId == id))
            return NotFound(new { message = $"Khong tim thay tai khoan ID {id}" });

        var scopes = await _context.UserOperationalScopes
            .AsNoTracking()
            .Where(scope => scope.UserId == id)
            .OrderBy(scope => scope.TaskKey)
            .ThenBy(scope => scope.SiteId)
            .ThenBy(scope => scope.GateId)
            .ThenBy(scope => scope.LaneId)
            .ThenBy(scope => scope.SecurityZoneId)
            .Select(scope => new
            {
                scope.UserOperationalScopeId,
                scope.UserId,
                scope.TaskKey,
                scope.SiteId,
                scope.GateId,
                scope.LaneId,
                scope.SecurityZoneId,
                scope.CanView,
                scope.CanManage,
                scope.ValidFromUtc,
                scope.ValidToUtc,
                scope.Note
            })
            .ToListAsync();

        return Ok(scopes);
    }

    [HttpPut("{id:int}/operational-scopes")]
    [RequireStepUp(PrivilegedActions.UserAdministration)]
    public async Task<IActionResult> ReplaceOperationalScopes(int id, [FromBody] List<OperationalScopeUpsertRequest>? request)
    {
        var user = await _context.AppUsers.FindAsync(id);
        if (user == null)
            return NotFound(new { message = $"Khong tim thay tai khoan ID {id}" });

        var allowedTasks = UserOperationalScopeService.TaskCatalog
            .Select(item => item.TaskKey)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        request ??= [];
        foreach (var item in request)
        {
            if (string.IsNullOrWhiteSpace(item.TaskKey) || !allowedTasks.Contains(item.TaskKey))
                return BadRequest(new { message = $"TaskKey '{item.TaskKey}' khong hop le." });
        }

        var siteIds = request.Where(item => item.SiteId.HasValue).Select(item => item.SiteId!.Value).Distinct().ToList();
        var gateIds = request.Where(item => item.GateId.HasValue).Select(item => item.GateId!.Value).Distinct().ToList();
        var laneIds = request.Where(item => item.LaneId.HasValue).Select(item => item.LaneId!.Value).Distinct().ToList();
        var zoneIds = request.Where(item => item.SecurityZoneId.HasValue).Select(item => item.SecurityZoneId!.Value).Distinct().ToList();

        if (siteIds.Count > 0 && await _context.Sites.CountAsync(site => siteIds.Contains(site.SiteId)) != siteIds.Count)
            return BadRequest(new { message = "Co Site khong ton tai trong pham vi duoc gan." });
        if (gateIds.Count > 0 && await _context.Gates.CountAsync(gate => gateIds.Contains(gate.GateId)) != gateIds.Count)
            return BadRequest(new { message = "Co Gate khong ton tai trong pham vi duoc gan." });
        if (laneIds.Count > 0 && await _context.Lanes.CountAsync(lane => laneIds.Contains(lane.LaneId)) != laneIds.Count)
            return BadRequest(new { message = "Co Lane khong ton tai trong pham vi duoc gan." });
        if (zoneIds.Count > 0 && await _context.SecurityZones.CountAsync(zone => zoneIds.Contains(zone.SecurityZoneId)) != zoneIds.Count)
            return BadRequest(new { message = "Co SecurityZone khong ton tai trong pham vi duoc gan." });

        var existing = await _context.UserOperationalScopes
            .Where(scope => scope.UserId == id)
            .ToListAsync();
        _context.UserOperationalScopes.RemoveRange(existing);

        var currentUserIdClaim = User.FindFirstValue(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub);
        var currentUserId = int.TryParse(currentUserIdClaim, out var parsedUserId) ? parsedUserId : (int?)null;

        var scopes = request.Select(item => new UserOperationalScope
        {
            UserId = id,
            TaskKey = item.TaskKey.Trim(),
            SiteId = item.SiteId,
            GateId = item.GateId,
            LaneId = item.LaneId,
            SecurityZoneId = item.SecurityZoneId,
            CanView = item.CanView,
            CanManage = item.CanManage,
            ValidFromUtc = item.ValidFromUtc ?? DateTime.UtcNow,
            ValidToUtc = item.ValidToUtc,
            Note = string.IsNullOrWhiteSpace(item.Note) ? null : item.Note.Trim(),
            CreatedByUserId = currentUserId
        }).ToList();

        if (scopes.Count > 0)
            _context.UserOperationalScopes.AddRange(scopes);

        await _context.SaveChangesAsync();
        return Ok(new
        {
            message = "Da cap nhat pham vi van hanh cho tai khoan.",
            count = scopes.Count
        });
    }

    public sealed class OperationalScopeUpsertRequest
    {
        public string TaskKey { get; set; } = string.Empty;
        public int? SiteId { get; set; }
        public int? GateId { get; set; }
        public int? LaneId { get; set; }
        public int? SecurityZoneId { get; set; }
        public bool CanView { get; set; } = true;
        public bool CanManage { get; set; } = true;
        public DateTime? ValidFromUtc { get; set; }
        public DateTime? ValidToUtc { get; set; }
        public string? Note { get; set; }
    }

    [HttpPost("{id}/mfa/reset")]
    [RequireStepUp(PrivilegedActions.UserAdministration)]
    public async Task<IActionResult> ResetMfa(int id)
    {
        var user = await _context.AppUsers.FindAsync(id);
        if (user == null)
            return NotFound(new { message = $"Khong tim thay tai khoan ID {id}" });

        user.MfaEnabled = false;
        user.MfaSecretProtected = null;
        user.MfaConfiguredAtUtc = null;
        user.TokenVersion++;

        var activeTokens = await _context.UserRefreshTokens
            .Where(t => t.UserId == id && t.RevokedAtUtc == null && t.ExpiresAtUtc > DateTime.UtcNow)
            .ToListAsync();

        foreach (var token in activeTokens)
        {
            token.RevokedAtUtc = DateTime.UtcNow;
            token.RevocationReason = "MFA reset";
        }

        await _context.SaveChangesAsync();
        return Ok(new { message = "Da dat lai MFA. Tai khoan se thiet lap lai o lan dang nhap tiep theo." });
    }

    [HttpDelete("{id}")]
    [RequireStepUp(PrivilegedActions.UserAdministration)]
    public async Task<IActionResult> Delete(int id)
    {
        var user = await _context.AppUsers.FindAsync(id);
        if (user == null)
            return NotFound(new { message = $"Khong tim thay tai khoan ID {id}" });

        var currentUserIdClaim = User.FindFirstValue(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub);
        if (currentUserIdClaim != null && int.TryParse(currentUserIdClaim, out var currentUserId) && currentUserId == id)
            return BadRequest(new { message = "Khong the xoa tai khoan dang dang nhap" });

        _context.AppUsers.Remove(user);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    [HttpPatch("{id}/lock")]
    public async Task<IActionResult> LockUser(int id)
    {
        var user = await _context.AppUsers.FindAsync(id);
        if (user == null)
            return NotFound(new { message = $"Khong tim thay tai khoan ID {id}" });

        user.IsActive = false;
        await _context.SaveChangesAsync();
        return Ok(new { message = "Da khoa tai khoan." });
    }

    [HttpPatch("{id}/unlock")]
    public async Task<IActionResult> UnlockUser(int id)
    {
        var user = await _context.AppUsers.FindAsync(id);
        if (user == null)
            return NotFound(new { message = $"Khong tim thay tai khoan ID {id}" });

        user.IsActive = true;
        await _context.SaveChangesAsync();
        return Ok(new { message = "Da mo khoa tai khoan." });
    }

    private static string NormalizeUsernameInvariant(string username) =>
        username.Trim().ToUpperInvariant();
}

