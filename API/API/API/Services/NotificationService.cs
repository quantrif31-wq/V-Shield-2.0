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

    /// <summary>
    /// Tra cứu rules matching eventType, tìm người nhận, tạo notification + push real-time
    /// </summary>
    public async Task NotifyEventAsync(
        string eventType,
        string title,
        string? body = null,
        string? referenceType = null,
        string? referenceId = null,
        string? actionUrl = null)
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
                {
                    userIds.Add(rule.RecipientUserId.Value);
                }
                else if (!string.IsNullOrEmpty(rule.RecipientRole))
                {
                    var roleUserIds = await _db.AppUsers
                        .Where(u => u.Role == rule.RecipientRole && u.IsActive)
                        .Select(u => u.UserId)
                        .ToListAsync();
                    foreach (var id in roleUserIds) userIds.Add(id);
                }
            }

            if (userIds.Count == 0) return;

            var notifications = userIds.Select(uid => new Notification
            {
                RecipientUserId = uid,
                Title = title,
                Body = body,
                Category = eventType.StartsWith("Alarm.") ? "Alarm"
                    : eventType.StartsWith("Approval.") ? "Approval"
                    : "System",
                ReferenceType = referenceType,
                ReferenceId = referenceId,
                ActionUrl = actionUrl,
                CreatedAt = DateTime.UtcNow
            }).ToList();

            _db.Notifications.AddRange(notifications);
            await _db.SaveChangesAsync();

            // Push real-time qua SignalR
            foreach (var notif in notifications)
            {
                await _hubContext.Clients
                    .Group($"notif_user_{notif.RecipientUserId}")
                    .SendAsync("NewNotification", new
                    {
                        notif.Id,
                        notif.Title,
                        notif.Body,
                        notif.Category,
                        notif.ReferenceType,
                        notif.ReferenceId,
                        notif.ActionUrl,
                        notif.CreatedAt,
                        notif.IsRead
                    });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send notification for event {EventType}", eventType);
        }
    }

    public async Task NotifyUsersAsync(
        List<int> userIds,
        string title,
        string? body = null,
        string? category = "System",
        string? referenceType = null,
        string? referenceId = null,
        string? actionUrl = null)
    {
        if (userIds.Count == 0) return;
        try
        {
            var notifications = userIds.Select(uid => new Notification
            {
                RecipientUserId = uid,
                Title = title,
                Body = body,
                Category = category,
                ReferenceType = referenceType,
                ReferenceId = referenceId,
                ActionUrl = actionUrl,
                CreatedAt = DateTime.UtcNow
            }).ToList();

            _db.Notifications.AddRange(notifications);
            await _db.SaveChangesAsync();

            foreach (var notif in notifications)
            {
                await _hubContext.Clients
                    .Group($"notif_user_{notif.RecipientUserId}")
                    .SendAsync("NewNotification", new
                    {
                        notif.Id,
                        notif.Title,
                        notif.Body,
                        notif.Category,
                        notif.ReferenceType,
                        notif.ReferenceId,
                        notif.ActionUrl,
                        notif.CreatedAt,
                        notif.IsRead
                    });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to notify users");
        }
    }

    public async Task NotifyRolesAsync(
        List<string> roles,
        string title,
        string? body = null,
        string? category = "System",
        string? referenceType = null,
        string? referenceId = null,
        string? actionUrl = null)
    {
        try
        {
            var userIds = await _db.AppUsers
                .Where(u => roles.Contains(u.Role) && u.IsActive)
                .Select(u => u.UserId)
                .ToListAsync();

            await NotifyUsersAsync(userIds, title, body, category, referenceType, referenceId, actionUrl);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to notify roles: {Roles}", string.Join(", ", roles));
        }
    }

    public async Task NotifyAllAsync(
        string title,
        string? body = null,
        string? category = "System",
        string? referenceType = null,
        string? referenceId = null,
        string? actionUrl = null)
    {
        try
        {
            var userIds = await _db.AppUsers
                .Where(u => u.IsActive)
                .Select(u => u.UserId)
                .ToListAsync();

            await NotifyUsersAsync(userIds, title, body, category, referenceType, referenceId, actionUrl);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to notify all users");
        }
    }
}
