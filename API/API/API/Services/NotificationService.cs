using API.Data;
using API.Hubs;
using API.Models;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace API.Services;

public class NotificationService : INotificationService
{
    private readonly ApplicationDbContext _db;
    private readonly IHubContext<NotificationHub> _hubContext;
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(
        ApplicationDbContext db,
        IHubContext<NotificationHub> hubContext,
        ILogger<NotificationService> logger)
    {
        _db = db;
        _hubContext = hubContext;
        _logger = logger;
    }

    public async Task NotifyEventAsync(
        string eventType, string title, string? body = null,
        string? referenceType = null, string? referenceId = null, string? actionUrl = null,
        decimal? latitude = null, decimal? longitude = null, string? locationLabel = null)
    {
        try
        {
            var rules = await _db.NotificationRules
                .Where(r => r.IsActive && (r.EventType == eventType || r.EventType == "*"))
                .ToListAsync();

            if (rules.Count == 0) return;

            var userIds = new HashSet<int>();
            foreach (var rule in rules)
            {
                if (rule.RecipientUserId.HasValue)
                    userIds.Add(rule.RecipientUserId.Value);
                else if (!string.IsNullOrEmpty(rule.RecipientRole))
                {
                    var roleUserIds = await _db.AppUsers
                        .Where(u => u.Role == rule.RecipientRole && u.IsActive)
                        .Select(u => u.UserId).ToListAsync();
                    foreach (var id in roleUserIds) userIds.Add(id);
                }
            }

            if (userIds.Count == 0) return;

            var category = eventType.StartsWith("Alarm.") ? "Alarm"
                : eventType.StartsWith("Approval.") ? "Approval" : "System";
            var severity = ResolveSeverity(eventType, category, title);

            var notifications = userIds.Select(uid => new Notification
            {
                RecipientUserId = uid, Title = title, Body = body, Category = category ?? "System",
                Severity = severity,
                ReferenceType = referenceType, ReferenceId = referenceId, ActionUrl = actionUrl,
                Latitude = latitude, Longitude = longitude, LocationLabel = locationLabel,
                CreatedAt = DateTime.UtcNow
            }).ToList();

            _db.Notifications.AddRange(notifications);
            await _db.SaveChangesAsync();

            foreach (var notif in notifications)
            {
                await _hubContext.Clients.Group($"notif_user_{notif.RecipientUserId}")
                    .SendAsync("NewNotification", new
                    {
                        notif.Id, notif.Title, notif.Body, notif.Category,
                        notif.Severity,
                        notif.ReferenceType, notif.ReferenceId, notif.ActionUrl,
                        notif.Latitude, notif.Longitude, notif.LocationLabel,
                        notif.CreatedAt, notif.IsRead
                    });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send notification for event {EventType}", eventType);
        }
    }

    public async Task NotifyUsersAsync(
        List<int> userIds, string title, string? body = null,
        string? category = "System", string? referenceType = null, string? referenceId = null, string? actionUrl = null,
        decimal? latitude = null, decimal? longitude = null, string? locationLabel = null)
    {
        if (userIds.Count == 0) return;
        try
        {
            var notifications = userIds.Select(uid => new Notification
            {
                RecipientUserId = uid, Title = title, Body = body, Category = category ?? "System",
                Severity = ResolveSeverity(null, category, title),
                ReferenceType = referenceType, ReferenceId = referenceId, ActionUrl = actionUrl,
                Latitude = latitude, Longitude = longitude, LocationLabel = locationLabel,
                CreatedAt = DateTime.UtcNow
            }).ToList();

            _db.Notifications.AddRange(notifications);
            await _db.SaveChangesAsync();

            foreach (var notif in notifications)
            {
                await _hubContext.Clients.Group($"notif_user_{notif.RecipientUserId}")
                    .SendAsync("NewNotification", new
                    {
                        notif.Id, notif.Title, notif.Body, notif.Category,
                        notif.Severity,
                        notif.ReferenceType, notif.ReferenceId, notif.ActionUrl,
                        notif.Latitude, notif.Longitude, notif.LocationLabel,
                        notif.CreatedAt, notif.IsRead
                    });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to notify users");
        }
    }

    public async Task NotifyRolesAsync(
        List<string> roles, string title, string? body = null,
        string? category = "System", string? referenceType = null, string? referenceId = null, string? actionUrl = null,
        decimal? latitude = null, decimal? longitude = null, string? locationLabel = null)
    {
        try
        {
            var userIds = await _db.AppUsers
                .Where(u => roles.Contains(u.Role) && u.IsActive)
                .Select(u => u.UserId).ToListAsync();

            await NotifyUsersAsync(userIds, title, body, category, referenceType, referenceId, actionUrl,
                latitude, longitude, locationLabel);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to notify roles: {Roles}", string.Join(", ", roles));
        }
    }

    public async Task NotifyAllAsync(
        string title, string? body = null,
        string? category = "System", string? referenceType = null, string? referenceId = null, string? actionUrl = null,
        decimal? latitude = null, decimal? longitude = null, string? locationLabel = null)
    {
        try
        {
            var userIds = await _db.AppUsers
                .Where(u => u.IsActive)
                .Select(u => u.UserId).ToListAsync();

            await NotifyUsersAsync(userIds, title, body, category, referenceType, referenceId, actionUrl,
                latitude, longitude, locationLabel);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to notify all users");
        }
    }

    private static string ResolveSeverity(string? eventType, string? category, string? title)
    {
        if (!string.IsNullOrWhiteSpace(eventType))
        {
            if (eventType.StartsWith("Chat.", StringComparison.OrdinalIgnoreCase))
                return "success";
            if (string.Equals(eventType, "Alarm.Duress", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(eventType, "Alarm.EmergencyPass", StringComparison.OrdinalIgnoreCase))
                return "critical";
            if (eventType.StartsWith("Alarm.", StringComparison.OrdinalIgnoreCase))
                return "warning";
            if (eventType.StartsWith("Approval.", StringComparison.OrdinalIgnoreCase))
                return "caution";
        }

        if (string.Equals(category, "Alarm", StringComparison.OrdinalIgnoreCase))
        {
            if (!string.IsNullOrWhiteSpace(title) &&
                (title.Contains("khẩn cấp", StringComparison.OrdinalIgnoreCase) ||
                 title.Contains("uy hiếp", StringComparison.OrdinalIgnoreCase) ||
                 title.Contains("đột nhập", StringComparison.OrdinalIgnoreCase)))
            {
                return "critical";
            }

            return "warning";
        }

        if (string.Equals(category, "Approval", StringComparison.OrdinalIgnoreCase))
            return "caution";

        if (string.Equals(category, "Chat", StringComparison.OrdinalIgnoreCase))
            return "success";

        return "info";
    }
}
