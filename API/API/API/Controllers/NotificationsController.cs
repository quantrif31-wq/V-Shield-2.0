using API.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace API.Controllers;

[Route("api/notifications")]
[ApiController]
[Authorize]
public class NotificationsController : ControllerBase
{
    private readonly ApplicationDbContext _db;

    public NotificationsController(ApplicationDbContext db)
    {
        _db = db;
    }

    private int GetUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                    ?? User.FindFirst("userId")?.Value;
        return int.TryParse(claim, out var id) ? id : 0;
    }

    /// <summary>
    /// Danh sách notification của user (mới nhất trước)
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetNotifications([FromQuery] int skip = 0, [FromQuery] int take = 50)
    {
        var userId = GetUserId();
        var notifications = await _db.Notifications
            .Where(n => n.RecipientUserId == userId)
            .OrderByDescending(n => n.CreatedAt)
            .Skip(skip)
            .Take(take)
            .Select(n => new
            {
                n.Id,
                n.Title,
                n.Body,
                n.Category,
                n.Severity,
                n.ReferenceType,
                n.ReferenceId,
                n.ActionUrl,
                n.Latitude,
                n.Longitude,
                n.LocationLabel,
                n.IsRead,
                n.CreatedAt,
                n.ReadAt
            })
            .ToListAsync();

        return Ok(new { success = true, data = notifications });
    }

    /// <summary>
    /// Số lượng chưa đọc
    /// </summary>
    [HttpGet("unread-count")]
    public async Task<IActionResult> GetUnreadCount()
    {
        var userId = GetUserId();
        var count = await _db.Notifications
            .CountAsync(n => n.RecipientUserId == userId && !n.IsRead);

        return Ok(new { success = true, count });
    }

    /// <summary>
    /// Đánh dấu đã đọc một notification
    /// </summary>
    [HttpPost("{id:long}/read")]
    public async Task<IActionResult> MarkAsRead(long id)
    {
        var userId = GetUserId();
        var notif = await _db.Notifications
            .FirstOrDefaultAsync(n => n.Id == id && n.RecipientUserId == userId);

        if (notif == null)
            return NotFound(new { success = false, message = "Không tìm thấy thông báo." });

        notif.IsRead = true;
        notif.ReadAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        return Ok(new { success = true });
    }

    /// <summary>
    /// Đánh dấu tất cả đã đọc
    /// </summary>
    [HttpPost("read-all")]
    public async Task<IActionResult> MarkAllAsRead()
    {
        var userId = GetUserId();
        var unread = await _db.Notifications
            .Where(n => n.RecipientUserId == userId && !n.IsRead)
            .ToListAsync();

        var now = DateTime.UtcNow;
        foreach (var n in unread)
        {
            n.IsRead = true;
            n.ReadAt = now;
        }

        await _db.SaveChangesAsync();
        return Ok(new { success = true, count = unread.Count });
    }
}
