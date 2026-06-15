using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using API.Data;
using API.Middleware;
using API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace API.Controllers;

[ApiController]
[Route("api/enterprise/identity")]
[Authorize(Roles = "Admin")]
public class EnterpriseIdentityController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;

    public EnterpriseIdentityController(ApplicationDbContext context, IHttpClientFactory httpClientFactory, IConfiguration configuration)
    {
        _context = context;
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
    }

    [HttpGet("overview")]
    public async Task<IActionResult> GetOverview()
    {
        return Ok(new
        {
            Providers = await _context.ExternalIdentityProviders.CountAsync(),
            EnabledProviders = await _context.ExternalIdentityProviders.CountAsync(item => item.IsEnabled),
            Mappings = await _context.ExternalIdentityMappings.CountAsync(),
            ActiveMappings = await _context.ExternalIdentityMappings.CountAsync(item => item.IsActive),
            ActiveEmployees = await _context.Employees.CountAsync(item => item.LifecycleStatus == EmployeeLifecycleStates.Active),
            SuspendedEmployees = await _context.Employees.CountAsync(item => item.LifecycleStatus == EmployeeLifecycleStates.Suspended),
            TerminatedEmployees = await _context.Employees.CountAsync(item => item.LifecycleStatus == EmployeeLifecycleStates.Terminated),
            RecertificationCampaigns = await _context.AccessRecertificationCampaigns.CountAsync()
        });
    }

    [HttpGet("providers")]
    public async Task<IActionResult> GetProviders()
    {
        var providers = await _context.ExternalIdentityProviders
            .OrderBy(item => item.Name)
            .ToListAsync();
        return Ok(providers);
    }

    [HttpPost("providers")]
    public async Task<IActionResult> UpsertProvider([FromBody] IdentityProviderUpsertRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            return BadRequest(new { message = "Name is required." });
        if (string.IsNullOrWhiteSpace(request.Authority))
            return BadRequest(new { message = "Authority is required." });

        var name = request.Name.Trim();
        var provider = await _context.ExternalIdentityProviders
            .FirstOrDefaultAsync(item => item.Name == name);

        if (provider == null)
        {
            provider = new ExternalIdentityProvider { Name = name };
            _context.ExternalIdentityProviders.Add(provider);
        }

        provider.Protocol = string.IsNullOrWhiteSpace(request.Protocol) ? "OIDC" : request.Protocol.Trim();
        provider.Authority = request.Authority.Trim();
        provider.ClientId = request.ClientId?.Trim();
        provider.ClientSecret = request.ClientSecret?.Trim();
        provider.RedirectUrl = request.RedirectUrl?.Trim();
        provider.Scopes = string.IsNullOrWhiteSpace(request.Scopes) ? "openid profile email" : request.Scopes.Trim();
        provider.IsEnabled = request.IsEnabled;

        await _context.SaveChangesAsync();
        return Ok(provider);
    }

    [HttpGet("providers/{providerId:int}/oidc-challenge")]
    public async Task<IActionResult> BuildOidcChallenge(int providerId, [FromQuery] string redirectUri, [FromQuery] string? state)
    {
        var provider = await _context.ExternalIdentityProviders.FindAsync(providerId);
        if (provider == null || !provider.IsEnabled)
            return NotFound(new { message = "Enabled identity provider not found." });
        if (!string.Equals(provider.Protocol, "OIDC", StringComparison.OrdinalIgnoreCase))
            return BadRequest(new { message = "Provider is not configured for OIDC." });
        if (string.IsNullOrWhiteSpace(provider.ClientId) || string.IsNullOrWhiteSpace(redirectUri))
            return BadRequest(new { message = "ClientId and redirectUri are required." });

        var challengeUrl = $"{provider.Authority.TrimEnd('/')}/authorize" +
                           $"?client_id={Uri.EscapeDataString(provider.ClientId)}" +
                           $"&redirect_uri={Uri.EscapeDataString(redirectUri)}" +
                           "&response_type=code" +
                           "&scope=openid%20profile%20email" +
                           $"&state={Uri.EscapeDataString(state ?? Guid.NewGuid().ToString("N"))}";

        return Ok(new
        {
            Provider = provider.Name,
            Protocol = provider.Protocol,
            ChallengeUrl = challengeUrl,
            Note = "OIDC boundary is generated by API. Token exchange can be wired to a provider-specific secret reference without editing protected runtime folders."
        });
    }

    [HttpPost("providers/{providerId:int}/oidc-callback")]
    [AllowAnonymous]
    public async Task<IActionResult> HandleOidcCallback(int providerId, [FromBody] OidcCallbackRequest request)
    {
        var provider = await _context.ExternalIdentityProviders.FindAsync(providerId);
        if (provider == null || !provider.IsEnabled || string.IsNullOrWhiteSpace(provider.ClientSecret))
            return NotFound(new { message = "Enabled OIDC provider with ClientSecret not found." });

        var redirectUrl = request.RedirectUri ?? provider.RedirectUrl ?? "https://localhost:5173/login";
        var scopes = provider.Scopes ?? "openid profile email";

        using var httpClient = _httpClientFactory.CreateClient();
        var tokenPayload = new Dictionary<string, string>
        {
            ["grant_type"] = "authorization_code",
            ["code"] = request.Code,
            ["redirect_uri"] = redirectUrl,
            ["client_id"] = provider.ClientId ?? "",
            ["client_secret"] = provider.ClientSecret
        };

        HttpResponseMessage tokenResponse;
        try
        {
            tokenResponse = await httpClient.PostAsync(
                $"{provider.Authority.TrimEnd('/')}/token",
                new FormUrlEncodedContent(tokenPayload));
            tokenResponse.EnsureSuccessStatusCode();
        }
        catch (Exception ex)
        {
            return Unauthorized(new { message = "Token exchange failed.", detail = ex.Message });
        }

        var tokenJson = await tokenResponse.Content.ReadAsStringAsync();
        using var tokenDoc = JsonDocument.Parse(tokenJson);
        var idToken = tokenDoc.RootElement.TryGetProperty("id_token", out var idProp) ? idProp.GetString() : null;
        var accessToken = tokenDoc.RootElement.TryGetProperty("access_token", out var atProp) ? atProp.GetString() : null;

        if (string.IsNullOrWhiteSpace(idToken))
            return Unauthorized(new { message = "No id_token received from provider." });

        JwtSecurityToken? jwt;
        try
        {
            var handler = new JwtSecurityTokenHandler();
            jwt = handler.ReadJwtToken(idToken);
        }
        catch
        {
            return Unauthorized(new { message = "Failed to parse id_token." });
        }

        var sub = jwt.Claims.FirstOrDefault(c => c.Type == "sub")?.Value;
        var email = jwt.Claims.FirstOrDefault(c => c.Type == "email" || c.Type == "preferred_username")?.Value;
        var name = jwt.Claims.FirstOrDefault(c => c.Type == "name" || c.Type == "given_name")?.Value ?? email ?? sub;

        if (string.IsNullOrWhiteSpace(sub))
            return Unauthorized(new { message = "id_token missing 'sub' claim." });

        var mapping = await _context.ExternalIdentityMappings
            .Include(m => m.User)
            .FirstOrDefaultAsync(m =>
                m.ExternalIdentityProviderId == providerId &&
                m.ExternalSubject == sub);

        if (mapping?.User == null)
        {
            var username = email?.Split('@')[0] ?? $"ext-{sub[..Math.Min(8, sub.Length)]}";
            var existingUser = await _context.AppUsers.FirstOrDefaultAsync(u =>
                u.Username.Trim().ToUpper() == username.ToUpperInvariant());

            var user = existingUser ?? new AppUser
            {
                Username = username,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(Convert.ToHexString(RandomNumberGenerator.GetBytes(18))),
                FullName = name ?? username,
                Role = "Staff",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                LastPasswordChangedAtUtc = DateTime.UtcNow
            };

            if (existingUser == null)
                _context.AppUsers.Add(user);

            if (mapping == null)
            {
                mapping = new ExternalIdentityMapping
                {
                    ExternalIdentityProviderId = providerId,
                    ExternalSubject = sub,
                    ExternalUsername = username,
                    User = user,
                    IsActive = true,
                    LastSyncedAtUtc = DateTime.UtcNow
                };
                _context.ExternalIdentityMappings.Add(mapping);
            }
            else
            {
                mapping.User = user;
                mapping.LastSyncedAtUtc = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();
        }

        var jwtSecret = _configuration["JwtSettings:SecretKey"]
            ?? throw new InvalidOperationException("JWT secret not configured");
        var jwtIssuer = _configuration["JwtSettings:Issuer"] ?? "V-Shield";
        var jwtAudience = _configuration["JwtSettings:Audience"] ?? "V-Shield-API";
        var jwtExpiryHours = _configuration.GetValue<int>("JwtSettings:TokenExpiryHours", 8);

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, mapping!.User!.UserId.ToString()),
            new(ClaimTypes.Name, mapping.User.Username),
            new(ClaimTypes.Role, mapping.User.Role),
            new("token_version", mapping.User.TokenVersion.ToString()),
            new("auth_method", "oidc"),
            new("ext_sub", sub)
        };

        var token = new JwtSecurityToken(
            issuer: jwtIssuer,
            audience: jwtAudience,
            claims: claims,
            expires: DateTime.UtcNow.AddHours(jwtExpiryHours),
            signingCredentials: credentials);

        var tokenString = new JwtSecurityTokenHandler().WriteToken(token);

        return Ok(new
        {
            access_token = tokenString,
            token_type = "Bearer",
            expires_in = (int)TimeSpan.FromHours(jwtExpiryHours).TotalSeconds,
            user = new
            {
                id = mapping.User.UserId,
                username = mapping.User.Username,
                fullName = mapping.User.FullName,
                role = mapping.User.Role
            }
        });
    }

    [HttpPost("import/users")]
    public async Task<IActionResult> ImportUsers([FromBody] IdentityUserImportRequest request)
    {
        if (!await _context.ExternalIdentityProviders.AnyAsync(item => item.ExternalIdentityProviderId == request.ProviderId && item.IsEnabled))
            return BadRequest(new { message = "Enabled identity provider not found." });
        if (request.Users.Count == 0)
            return BadRequest(new { message = "At least one user import row is required." });

        var results = new List<object>();
        foreach (var row in request.Users)
        {
            if (string.IsNullOrWhiteSpace(row.ExternalSubject) || string.IsNullOrWhiteSpace(row.Username))
            {
                results.Add(new { row.ExternalSubject, row.Username, status = "Skipped", reason = "ExternalSubject and Username are required." });
                continue;
            }

            var result = await UpsertImportedUserAsync(request.ProviderId, row);
            results.Add(result);
        }

        await _context.SaveChangesAsync();
        return Ok(new
        {
            Imported = results.Count(item => item.ToString()?.Contains("Imported", StringComparison.OrdinalIgnoreCase) == true),
            Results = results
        });
    }

    [HttpPost("import/groups")]
    public async Task<IActionResult> ImportGroups([FromBody] IdentityGroupImportRequest request)
    {
        if (request.Groups.Count == 0)
            return BadRequest(new { message = "At least one group import row is required." });

        var results = new List<object>();
        foreach (var row in request.Groups)
        {
            if (string.IsNullOrWhiteSpace(row.Code) || string.IsNullOrWhiteSpace(row.Name))
            {
                results.Add(new { row.Code, status = "Skipped", reason = "Code and Name are required." });
                continue;
            }

            var code = row.Code.Trim();
            var group = await _context.AccessGroups.FirstOrDefaultAsync(item => item.Code == code);
            if (group == null)
            {
                group = new AccessGroup { Code = code };
                _context.AccessGroups.Add(group);
            }

            group.Name = row.Name.Trim();
            results.Add(new { code, status = "Imported", group.Name });
        }

        await _context.SaveChangesAsync();
        return Ok(new { Results = results });
    }

    [HttpPatch("employees/{employeeId:int}/offboard")]
    [RequireStepUp(PrivilegedActions.UserAdministration)]
    public async Task<IActionResult> OffboardEmployee(int employeeId, [FromBody] OffboardingRequest request)
    {
        var employee = await _context.Employees.FindAsync(employeeId);
        if (employee == null)
            return NotFound(new { message = "Employee not found." });

        await ApplyLifecycleAsync(employee, EmployeeLifecycleStates.Terminated, request.Reason ?? "Offboarding requested");
        await _context.SaveChangesAsync();
        return Ok(await BuildRevocationProofAsync(employeeId));
    }

    [HttpGet("employees/{employeeId:int}/revocation-proof")]
    public async Task<IActionResult> GetRevocationProof(int employeeId)
    {
        if (!await _context.Employees.AnyAsync(item => item.EmployeeId == employeeId))
            return NotFound(new { message = "Employee not found." });

        return Ok(await BuildRevocationProofAsync(employeeId));
    }

    private async Task<object> UpsertImportedUserAsync(int providerId, ImportedIdentityUser row)
    {
        var username = row.Username.Trim();
        var normalizedUsername = username.ToUpperInvariant();
        var employee = await _context.Employees.FirstOrDefaultAsync(item => item.Email == row.Email);
        if (employee == null)
        {
            employee = new Employee
            {
                FullName = string.IsNullOrWhiteSpace(row.DisplayName) ? username : row.DisplayName.Trim(),
                Email = row.Email?.Trim(),
                Phone = row.Phone?.Trim(),
                Status = true,
                LifecycleStatus = EmployeeLifecycleStates.Active,
                PrimarySiteId = row.PrimarySiteId
            };
            _context.Employees.Add(employee);
        }
        else
        {
            employee.FullName = string.IsNullOrWhiteSpace(row.DisplayName) ? employee.FullName : row.DisplayName.Trim();
            employee.Phone = row.Phone?.Trim() ?? employee.Phone;
            employee.PrimarySiteId = row.PrimarySiteId ?? employee.PrimarySiteId;
        }

        var requestedLifecycle = string.IsNullOrWhiteSpace(row.LifecycleStatus)
            ? EmployeeLifecycleStates.Active
            : row.LifecycleStatus.Trim();
        await ApplyLifecycleAsync(employee, requestedLifecycle, "External identity import");

        var user = await _context.AppUsers.FirstOrDefaultAsync(item => item.Username.Trim().ToUpper() == normalizedUsername);
        if (user == null)
        {
            user = new AppUser
            {
                Username = username,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(GenerateBootstrapPassword()),
                FullName = employee.FullName,
                Role = NormalizeRole(row.Role),
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                LastPasswordChangedAtUtc = DateTime.UtcNow,
                Employee = employee
            };
            _context.AppUsers.Add(user);
        }
        else
        {
            user.FullName = employee.FullName;
            user.Role = NormalizeRole(row.Role ?? user.Role);
            user.Employee = employee;
        }

        if (requestedLifecycle is EmployeeLifecycleStates.Terminated or EmployeeLifecycleStates.Suspended or EmployeeLifecycleStates.ContractorExpired)
        {
            user.IsActive = false;
            user.TokenVersion++;
        }

        var mapping = await _context.ExternalIdentityMappings.FirstOrDefaultAsync(item =>
            item.ExternalIdentityProviderId == providerId &&
            item.ExternalSubject == row.ExternalSubject.Trim());

        if (mapping == null)
        {
            mapping = new ExternalIdentityMapping
            {
                ExternalIdentityProviderId = providerId,
                ExternalSubject = row.ExternalSubject.Trim()
            };
            _context.ExternalIdentityMappings.Add(mapping);
        }

        mapping.User = user;
        mapping.Employee = employee;
        mapping.ExternalUsername = username;
        mapping.LastSyncedAtUtc = DateTime.UtcNow;
        mapping.IsActive = requestedLifecycle == EmployeeLifecycleStates.Active ||
                           requestedLifecycle == EmployeeLifecycleStates.ContractorActive;

        return new { row.ExternalSubject, username, status = "Imported", lifecycle = employee.LifecycleStatus };
    }

    private async Task ApplyLifecycleAsync(Employee employee, string lifecycleStatus, string reason)
    {
        var previousState = employee.LifecycleStatus;
        employee.LifecycleStatus = lifecycleStatus;
        employee.LifecycleUpdatedAtUtc = DateTime.UtcNow;
        employee.Status = lifecycleStatus is EmployeeLifecycleStates.Active or EmployeeLifecycleStates.ContractorActive;

        if (previousState != lifecycleStatus || lifecycleStatus != EmployeeLifecycleStates.Active)
        {
            _context.EmployeeLifecycleEvents.Add(new EmployeeLifecycleEvent
            {
                Employee = employee,
                PreviousState = previousState,
                NewState = lifecycleStatus,
                Reason = reason,
                ChangedByUserId = GetCurrentUserId()
            });
        }

        if (lifecycleStatus is not (EmployeeLifecycleStates.Terminated or EmployeeLifecycleStates.Suspended or EmployeeLifecycleStates.ContractorExpired))
            return;

        var user = await _context.AppUsers.FirstOrDefaultAsync(item => item.EmployeeId == employee.EmployeeId);
        if (user != null)
        {
            user.IsActive = false;
            user.TokenVersion++;
            var tokens = await _context.UserRefreshTokens
                .Where(token => token.UserId == user.UserId && token.RevokedAtUtc == null)
                .ToListAsync();
            foreach (var token in tokens)
            {
                token.RevokedAtUtc = DateTime.UtcNow;
                token.RevocationReason = $"Lifecycle changed to {lifecycleStatus}";
            }
        }

        var accessRules = await _context.AccessRules
            .Where(rule => rule.SubjectType == "Employee" && rule.SubjectId == employee.EmployeeId && rule.IsActive)
            .ToListAsync();
        foreach (var rule in accessRules)
        {
            rule.IsActive = false;
            rule.ValidToUtc = DateTime.UtcNow;
        }
    }

    private async Task<object> BuildRevocationProofAsync(int employeeId)
    {
        var user = await _context.AppUsers.FirstOrDefaultAsync(item => item.EmployeeId == employeeId);
        var activeRefreshTokens = user == null
            ? 0
            : await _context.UserRefreshTokens.CountAsync(item =>
                item.UserId == user.UserId &&
                item.RevokedAtUtc == null &&
                item.ExpiresAtUtc > DateTime.UtcNow);
        var activeAccessRules = await _context.AccessRules.CountAsync(item =>
            item.SubjectType == "Employee" &&
            item.SubjectId == employeeId &&
            item.IsActive);

        return new
        {
            EmployeeId = employeeId,
            UserId = user?.UserId,
            UserDisabled = user == null || !user.IsActive,
            TokenVersion = user?.TokenVersion,
            ActiveRefreshTokens = activeRefreshTokens,
            ActiveAccessRules = activeAccessRules,
            ProofGeneratedAtUtc = DateTime.UtcNow
        };
    }

    private int? GetCurrentUserId()
    {
        var userIdClaim = User.FindFirstValue(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub);
        return int.TryParse(userIdClaim, out var userId) ? userId : null;
    }

    private static string NormalizeRole(string? role)
    {
        return role?.Trim() switch
        {
            "Admin" => "Admin",
            "BaoVe" => "BaoVe",
            "QuanLy" => "QuanLy",
            _ => "Staff"
        };
    }

    private static string GenerateBootstrapPassword()
    {
        return Convert.ToHexString(RandomNumberGenerator.GetBytes(18));
    }

    public sealed record IdentityProviderUpsertRequest(string Name, string? Protocol, string Authority, string? ClientId, string? ClientSecret, string? RedirectUrl, string? Scopes, bool IsEnabled);
    public sealed record OidcCallbackRequest(string Code, string? RedirectUri);
    public sealed record IdentityUserImportRequest(int ProviderId, List<ImportedIdentityUser> Users);
    public sealed record ImportedIdentityUser(string ExternalSubject, string Username, string? DisplayName, string? Email, string? Phone, string? Role, string? LifecycleStatus, int? PrimarySiteId);
    public sealed record IdentityGroupImportRequest(List<ImportedIdentityGroup> Groups);
    public sealed record ImportedIdentityGroup(string Code, string Name);
    public sealed record OffboardingRequest(string? Reason);
}
