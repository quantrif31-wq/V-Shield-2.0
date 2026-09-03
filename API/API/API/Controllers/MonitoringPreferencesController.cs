using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text.Json;
using API.Data;
using API.Middleware;
using API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace API.Controllers;

[ApiController]
[Route("api/monitoring-preferences")]
[Authorize]
[RequireOperationalTask("monitoring")]
public sealed class MonitoringPreferencesController(ApplicationDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        if (userId == null) return Unauthorized();
        var stored = await db.UserMonitoringPreferences.AsNoTracking()
            .FirstOrDefaultAsync(item => item.UserId == userId.Value, cancellationToken);
        return Ok(new { selectedCameraIds = ParseCameraIds(stored?.SelectedCameraIdsJson) });
    }

    [HttpPut]
    public async Task<IActionResult> Save([FromBody] SaveMonitoringPreferenceRequest? request, CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        if (userId == null) return Unauthorized();
        var cameraIds = (request?.SelectedCameraIds ?? [])
            .Where(id => id > 0).Distinct().Take(4).ToArray();
        if ((request?.SelectedCameraIds?.Distinct().Count() ?? 0) > 4)
            return BadRequest(new { message = "Chỉ được chọn tối đa 4 camera." });

        var preference = await db.UserMonitoringPreferences
            .FirstOrDefaultAsync(item => item.UserId == userId.Value, cancellationToken);
        if (preference == null)
        {
            preference = new UserMonitoringPreference { UserId = userId.Value };
            db.UserMonitoringPreferences.Add(preference);
        }
        preference.SelectedCameraIdsJson = JsonSerializer.Serialize(cameraIds);
        preference.UpdatedAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        return Ok(new { selectedCameraIds = cameraIds, updatedAtUtc = preference.UpdatedAtUtc });
    }

    private int? GetUserId()
    {
        var raw = User.FindFirstValue(JwtRegisteredClaimNames.Sub);
        return int.TryParse(raw, out var userId) ? userId : null;
    }

    private static int[] ParseCameraIds(string? raw)
    {
        try { return (JsonSerializer.Deserialize<int[]>(raw ?? "[]") ?? []).Where(id => id > 0).Distinct().Take(4).ToArray(); }
        catch (JsonException) { return []; }
    }
}

public sealed class SaveMonitoringPreferenceRequest
{
    public int[]? SelectedCameraIds { get; init; }
}
