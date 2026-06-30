using API.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace API.Controllers;

[ApiController]
[Route("api/security-alerts")]
[Authorize]
public class SecurityAlertsController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public SecurityAlertsController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet("active")]
    public async Task<IActionResult> GetActiveAlerts()
    {
        var now = DateTime.UtcNow;
        var emergencyModes = await _context.EmergencyStates
            .AsNoTracking()
            .Where(item => item.IsActive)
            .OrderByDescending(item => item.StartedAtUtc)
            .Take(5)
            .Select(item => new
            {
                id = $"mode-{item.EmergencyStateId}",
                kind = "EmergencyMode",
                severity = "Critical",
                title = $"Chế độ khẩn cấp: {item.State}",
                message = item.Reason,
                occurredAtUtc = item.StartedAtUtc,
                route = "/policy-engine"
            })
            .ToListAsync();

        var emergencyPasses = await _context.EmergencyPasses
            .AsNoTracking()
            .Where(item => item.Status == "Active" && item.ValidToUtc > now)
            .OrderByDescending(item => item.ValidFromUtc)
            .Take(5)
            .Select(item => new
            {
                id = $"pass-{item.EmergencyPassId}",
                kind = "EmergencyPass",
                severity = "Critical",
                title = $"Thông hành khẩn cấp: {item.SubjectName}",
                message = item.Reason,
                occurredAtUtc = item.ValidFromUtc,
                route = "/soc-console"
            })
            .ToListAsync();

        var duressEvents = await _context.DuressEvents
            .AsNoTracking()
            .Where(item => !item.IsAcknowledged)
            .OrderByDescending(item => item.OccurredAtUtc)
            .Take(5)
            .Select(item => new
            {
                id = $"duress-{item.DuressEventId}",
                kind = "Duress",
                severity = "Critical",
                title = "Cảnh báo ép buộc tại điểm kiểm soát",
                message = item.Description ?? "Nhân viên an ninh đã phát tín hiệu ép buộc.",
                occurredAtUtc = item.OccurredAtUtc,
                route = "/soc-console"
            })
            .ToListAsync();

        var alarms = await _context.Alarms
            .AsNoTracking()
            .Where(item => item.State != "Closed" &&
                           item.Severity == "Critical" &&
                           item.AlarmType != "EmergencyPass" &&
                           item.AlarmType != "Duress")
            .OrderByDescending(item => item.CreatedAtUtc)
            .Take(10)
            .Select(item => new
            {
                id = $"alarm-{item.AlarmId}",
                kind = item.AlarmType,
                severity = item.Severity,
                title = $"Cảnh báo SOC: {item.AlarmType}",
                message = item.Summary,
                occurredAtUtc = item.CreatedAtUtc,
                route = "/soc-console"
            })
            .ToListAsync();

        var items = emergencyModes.Cast<object>()
            .Concat(emergencyPasses)
            .Concat(duressEvents)
            .Concat(alarms)
            .ToList();

        return Ok(new { generatedAtUtc = now, criticalCount = items.Count, items });
    }
}
