using API.Data;
using API.Models;
using API.Services.ChatRelay;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace API.Hubs;

[Authorize]
public class ChatHub : Hub
{
    private readonly ApplicationDbContext _db;
    private readonly ILogger<ChatHub> _logger;
    private readonly ChatPresenceRegistry _presenceRegistry;
    private readonly ChatRelayGateway _relayGateway;

    public ChatHub(
        ApplicationDbContext db,
        ILogger<ChatHub> logger,
        ChatPresenceRegistry presenceRegistry,
        ChatRelayGateway relayGateway)
    {
        _db = db;
        _logger = logger;
        _presenceRegistry = presenceRegistry;
        _relayGateway = relayGateway;
    }

    private int GetEmployeeId()
    {
        var claim = Context.User?.FindFirst("employeeId")?.Value;
        return int.TryParse(claim, out var id) ? id : 0;
    }

    public override async Task OnConnectedAsync()
    {
        var empId = GetEmployeeId();
        if (empId > 0)
        {
            _presenceRegistry.Add(empId);

            var convIds = await _db.ChatParticipants
                .Where(p => p.EmployeeId == empId)
                .Select(p => $"conv_{p.ConversationId}")
                .ToListAsync();

            foreach (var group in convIds)
                await Groups.AddToGroupAsync(Context.ConnectionId, group);

            await Groups.AddToGroupAsync(Context.ConnectionId, $"user_{empId}");
        }
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var empId = GetEmployeeId();
        if (empId > 0)
        {
            _presenceRegistry.Remove(empId);
        }
        await base.OnDisconnectedAsync(exception);
    }

    public async Task<object?> SendMessage(int conversationId, string content, string messageType = "Text", string? signalingData = null, string? clientMessageId = null)
    {
        var empId = GetEmployeeId();
        if (empId <= 0 || string.IsNullOrWhiteSpace(content)) return null;

        var isParticipant = await _db.ChatParticipants
            .AnyAsync(p => p.ConversationId == conversationId && p.EmployeeId == empId);

        if (!isParticipant) return null;

        var normalizedClientMessageId = string.IsNullOrWhiteSpace(clientMessageId)
            ? null
            : clientMessageId.Trim();

        if (normalizedClientMessageId != null)
        {
            var existingMessage = await _db.ChatMessages
                .AsNoTracking()
                .Where(m => m.ConversationId == conversationId &&
                            m.SenderId == empId &&
                            m.ClientMessageId == normalizedClientMessageId)
                .Select(m => new
                {
                    m.MessageId,
                    m.ConversationId,
                    m.SenderId,
                    senderName = m.Sender.FullName,
                    m.Content,
                    m.MessageType,
                    m.ClientMessageId,
                    m.SignalingData,
                    m.SentAt,
                    m.IsRead,
                    m.ReadAt
                })
                .FirstOrDefaultAsync();

            if (existingMessage != null)
                return existingMessage;
        }

        var msg = new ChatMessage
        {
            ConversationId = conversationId,
            SenderId = empId,
            Content = content,
            MessageType = messageType,
            ClientMessageId = normalizedClientMessageId,
            SignalingData = signalingData,
            SentAt = DateTime.UtcNow
        };

        _db.ChatMessages.Add(msg);
        await _db.SaveChangesAsync();

        var sender = await _db.Employees
            .Where(e => e.EmployeeId == empId)
            .Select(e => new { e.EmployeeId, e.FullName })
            .FirstAsync();

        var payload = new
        {
            msg.MessageId,
            msg.ConversationId,
            msg.SenderId,
            senderName = sender.FullName,
            msg.Content,
            msg.MessageType,
            msg.ClientMessageId,
            msg.SignalingData,
            msg.SentAt,
            msg.IsRead,
            msg.ReadAt
        };

        await Clients.Group($"conv_{conversationId}").SendAsync("ReceiveMessage", payload);
        return payload;
    }

    public async Task MarkRead(int conversationId)
    {
        var empId = GetEmployeeId();
        if (empId <= 0) return;

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

        await Clients.Group($"conv_{conversationId}").SendAsync("MessagesRead", new
        {
            conversationId,
            readByEmployeeId = empId,
            readAt = now
        });
    }

    public async Task Typing(int conversationId)
    {
        var empId = GetEmployeeId();
        var empName = await _db.Employees
            .Where(e => e.EmployeeId == empId)
            .Select(e => e.FullName)
            .FirstOrDefaultAsync();

        if (empName == null) return;

        await Clients.OthersInGroup($"conv_{conversationId}").SendAsync("UserTyping", new
        {
            conversationId,
            employeeId = empId,
            fullName = empName
        });
    }

    /// <summary>
    /// WebRTC: gửi tín hiệu cuộc gọi đến một user cụ thể
    /// </summary>
    public async Task CallUser(int targetEmployeeId, string signalingType, string signalingData, int? conversationId = null)
    {
        var empId = GetEmployeeId();
        if (empId <= 0) return;

        var callerName = await _db.Employees
            .Where(e => e.EmployeeId == empId)
            .Select(e => e.FullName)
            .FirstOrDefaultAsync();

        if (callerName == null) return;

        var signal = new RelaySignal
        {
            Kind = RelaySignalKind.IncomingCall,
            TargetEmployeeId = targetEmployeeId,
            FromEmployeeId = empId,
            FromFullName = callerName,
            SignalingType = signalingType,
            SignalingData = signalingData,
            ConversationId = conversationId
        };

        if (_presenceRegistry.IsOnline(targetEmployeeId))
        {
            await _relayGateway.BroadcastCallSignalAsync(signal);
            return;
        }

        if (_relayGateway.IsEnabled)
        {
            await _relayGateway.RelaySignalAsync(signal);
        }
    }

    /// <summary>
    /// WebRTC: trả lời cuộc gọi
    /// </summary>
    public async Task CallResponse(int targetEmployeeId, string signalingType, string signalingData)
    {
        var empId = GetEmployeeId();
        if (empId <= 0) return;

        var responderName = await _db.Employees
            .Where(e => e.EmployeeId == empId)
            .Select(e => e.FullName)
            .FirstOrDefaultAsync();

        if (_presenceRegistry.IsOnline(targetEmployeeId))
        {
            await Clients.Group($"user_{targetEmployeeId}").SendAsync("CallResponse", new
            {
                fromEmployeeId = empId,
                fromFullName = responderName,
                signalingType,
                signalingData
            });
        }
        else if (_relayGateway.IsEnabled)
        {
            await _relayGateway.RelaySignalAsync(new RelaySignal
            {
                Kind = RelaySignalKind.CallResponse,
                TargetEmployeeId = targetEmployeeId,
                FromEmployeeId = empId,
                FromFullName = responderName,
                SignalingType = signalingType,
                SignalingData = signalingData
            });
        }

        // Call answered/rejected on THIS device: tell the same employee's other
        // devices (and the other backend via relay) to stop ringing.
        if (string.Equals(signalingType, "accepted", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(signalingType, "reject", StringComparison.OrdinalIgnoreCase))
        {
            await Clients.OthersInGroup($"user_{empId}").SendAsync("CallEnded", new
            {
                fromEmployeeId = empId,
                conversationId = (int?)null
            });

            if (_relayGateway.IsEnabled)
            {
                await _relayGateway.NotifyCallHandledElsewhereAsync(empId, null);
            }
        }
    }

    public async Task EndCall(int targetEmployeeId, int? conversationId = null)
    {
        var empId = GetEmployeeId();
        if (empId <= 0) return;

        if (_presenceRegistry.IsOnline(targetEmployeeId))
        {
            await Clients.Group($"user_{targetEmployeeId}").SendAsync("CallEnded", new
            {
                fromEmployeeId = empId,
                conversationId
            });
            return;
        }

        if (_relayGateway.IsEnabled)
        {
            await _relayGateway.RelaySignalAsync(new RelaySignal
            {
                Kind = RelaySignalKind.CallEnded,
                TargetEmployeeId = targetEmployeeId,
                FromEmployeeId = empId,
                ConversationId = conversationId
            });
        }
    }
}
