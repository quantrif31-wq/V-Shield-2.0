using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[ApiController]
[Route("api/realtime")]
[Authorize]
public sealed class RealtimeIceController(IConfiguration configuration) : ControllerBase
{
    private static readonly string[] DefaultStunUrls =
    [
        "stun:stun.l.google.com:19302",
        "stun:stun.cloudflare.com:3478"
    ];

    [HttpGet("ice-configuration")]
    public IActionResult GetIceConfiguration()
    {
        var servers = new List<object> { new { urls = DefaultStunUrls } };
        var turnUrls = SplitUrls(configuration["Realtime:TurnUrls"]);
        var sharedSecret = configuration["Realtime:TurnSharedSecret"]?.Trim();

        if (turnUrls.Length > 0 && !string.IsNullOrWhiteSpace(sharedSecret))
        {
            var ttl = Math.Clamp(configuration.GetValue<int?>("Realtime:TurnCredentialTtlSeconds") ?? 3600, 300, 86400);
            var username = $"{DateTimeOffset.UtcNow.AddSeconds(ttl).ToUnixTimeSeconds()}:{User.FindFirst("sub")?.Value ?? "viewer"}";
            var password = Convert.ToBase64String(HMACSHA1.HashData(Encoding.UTF8.GetBytes(sharedSecret), Encoding.UTF8.GetBytes(username)));
            servers.Add(new { urls = turnUrls, username, credential = password });
        }

        return Ok(new { iceServers = servers });
    }

    private static string[] SplitUrls(string? value) => (value ?? string.Empty)
        .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
        .Where(url => Uri.TryCreate(url, UriKind.Absolute, out _))
        .ToArray();
}
