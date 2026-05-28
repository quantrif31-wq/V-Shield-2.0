using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;

namespace API.Services;

public class HttpCurrentUserContext : ICurrentUserContext
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public HttpCurrentUserContext(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public int? UserId
    {
        get
        {
            var raw = _httpContextAccessor.HttpContext?.User?.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
            return int.TryParse(raw, out var id) ? id : null;
        }
    }

    public string? Username
    {
        get
        {
            var user = _httpContextAccessor.HttpContext?.User;
            if (user == null) return null;

            return user.Identity?.Name
                ?? user.FindFirst(JwtRegisteredClaimNames.UniqueName)?.Value
                ?? user.FindFirst("unique_name")?.Value
                ?? user.FindFirst(ClaimTypes.Name)?.Value
                ?? user.FindFirst("username")?.Value
                ?? user.FindFirst("preferred_username")?.Value
                ?? user.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
        }
    }
}
