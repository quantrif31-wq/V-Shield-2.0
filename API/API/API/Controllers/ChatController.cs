using API.Data;
using API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace API.Controllers;

[Route("api/chat")]
[ApiController]
[Authorize]
public class ChatController : ControllerBase
{
    private readonly ApplicationDbContext _db;

    public ChatController(ApplicationDbContext db)
    {
        _db = db;
    }

    private int GetEmployeeId()
    {
        var claim = User.FindFirst("employeeId")?.Value;
        return int.TryParse(claim, out var id) ? id : 0;
    }

    /// <summary>
    /// Danh sách nhân viên để chat (danh bạ)
    /// </summary>
    [HttpGet("contacts")]
    public async Task<IActionResult> GetContacts()
    {
        var empId = GetEmployeeId();
        var contacts = await _db.Employees
            .Where(e => e.EmployeeId != empId && e.Status == true)
            .OrderBy(e => e.FullName)
            .Select(e => new
            {
                e.EmployeeId,
                e.FullName,
                e.Email,
                phone = e.Phone,
                positionName = e.Position != null ? e.Position.Name : null,
                departmentName = e.Department != null ? e.Department.Name : null
            })
            .ToListAsync();

        return Ok(new { success = true, data = contacts });
    }

    /// <summary>
    /// Danh sách hội thoại của tôi
    /// </summary>
    [HttpGet("conversations")]
    public async Task<IActionResult> GetConversations()
    {
        var empId = GetEmployeeId();

        var convs = await _db.ChatConversations
            .Where(c => c.Participants.Any(p => p.EmployeeId == empId))
            .OrderByDescending(c => c.Messages.Max(m => m.SentAt))
            .Select(c => new
            {
                c.ConversationId,
                c.CreatedAt,
                c.Title,
                lastMessage = c.Messages.OrderByDescending(m => m.SentAt).Select(m => new
                {
                    m.MessageId,
                    m.Content,
                    m.SentAt,
                    m.MessageType,
                    senderName = m.Sender.FullName,
                    senderId = m.SenderId
                }).FirstOrDefault(),
                participants = c.Participants.Select(p => new
                {
                    p.EmployeeId,
                    fullName = p.Employee.FullName
                }).ToList(),
                unreadCount = c.Messages.Count(m => m.SenderId != empId && !m.IsRead),
                lastReadAt = c.Participants
                    .Where(p => p.EmployeeId == empId)
                    .Select(p => p.LastReadAt)
                    .FirstOrDefault()
            })
            .ToListAsync();

        return Ok(new { success = true, data = convs });
    }

    /// <summary>
    /// Tạo hội thoại mới (1-1 hoặc nhóm)
    /// </summary>
    [HttpPost("conversations")]
    public async Task<IActionResult> CreateConversation([FromBody] CreateConversationRequest request)
    {
        var empId = GetEmployeeId();
        if (request.EmployeeIds == null || request.EmployeeIds.Count == 0)
            return BadRequest(new { success = false, message = "Cần ít nhất một người tham gia." });

        var allIds = request.EmployeeIds.Append(empId).Distinct().ToList();
        var validEmployees = await _db.Employees
            .Where(e => allIds.Contains(e.EmployeeId) && e.Status == true)
            .Select(e => e.EmployeeId)
            .ToListAsync();

        if (validEmployees.Count != allIds.Count)
            return BadRequest(new { success = false, message = "Một số nhân viên không hợp lệ." });

        // Nếu là 1-1, kiểm tra hội thoại đã tồn tại
        if (allIds.Count == 2)
        {
            var existingConv = await _db.ChatConversations
                .FirstOrDefaultAsync(c => c.Participants.Count == 2 &&
                    c.Participants.All(p => allIds.Contains(p.EmployeeId)));

            if (existingConv != null)
                return Ok(new { success = true, data = new { existingConv.ConversationId }, isExisting = true });
        }

        var conv = new ChatConversation
        {
            Title = request.Title,
            CreatedAt = DateTime.UtcNow
        };

        foreach (var id in allIds)
        {
            conv.Participants.Add(new ChatParticipant
            {
                EmployeeId = id,
                LastReadAt = DateTime.UtcNow
            });
        }

        _db.ChatConversations.Add(conv);
        await _db.SaveChangesAsync();

        return Ok(new { success = true, data = new { conv.ConversationId }, isExisting = false });
    }

    /// <summary>
    /// Lịch sử tin nhắn của một hội thoại
    /// </summary>
    [HttpGet("conversations/{conversationId}/messages")]
    public async Task<IActionResult> GetMessages(int conversationId, [FromQuery] int skip = 0, [FromQuery] int take = 50)
    {
        var empId = GetEmployeeId();
        var isParticipant = await _db.ChatParticipants
            .AnyAsync(p => p.ConversationId == conversationId && p.EmployeeId == empId);

        if (!isParticipant)
            return Forbid();

        var messages = await _db.ChatMessages
            .Where(m => m.ConversationId == conversationId)
            .OrderByDescending(m => m.SentAt)
            .Skip(skip)
            .Take(take)
            .OrderBy(m => m.SentAt)
            .Select(m => new
            {
                m.MessageId,
                m.SenderId,
                senderName = m.Sender.FullName,
                m.Content,
                m.MessageType,
                m.SignalingData,
                m.SentAt,
                m.IsRead,
                m.ReadAt
            })
            .ToListAsync();

        return Ok(new { success = true, data = messages });
    }

    /// <summary>
    /// Đánh dấu đã đọc tất cả tin nhắn trong hội thoại
    /// </summary>
    [HttpPost("conversations/{conversationId}/read")]
    public async Task<IActionResult> MarkAsRead(int conversationId)
    {
        var empId = GetEmployeeId();
        var now = DateTime.UtcNow;

        var unread = await _db.ChatMessages
            .Where(m => m.ConversationId == conversationId && m.SenderId != empId && !m.IsRead)
            .ToListAsync();

        foreach (var m in unread)
        {
            m.IsRead = true;
            m.ReadAt = now;
        }

        var participant = await _db.ChatParticipants
            .FirstOrDefaultAsync(p => p.ConversationId == conversationId && p.EmployeeId == empId);
        if (participant != null)
            participant.LastReadAt = now;

        await _db.SaveChangesAsync();

        return Ok(new { success = true, readCount = unread.Count });
    }
}

public class CreateConversationRequest
{
    public List<int> EmployeeIds { get; set; } = new();
    public string? Title { get; set; }
}
