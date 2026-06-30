using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace API.Models;

[Table("ChatMessages")]
public class ChatMessage
{
    [Key]
    public int MessageId { get; set; }

    public int ConversationId { get; set; }

    public int SenderId { get; set; }

    [MaxLength(2000)]
    public string Content { get; set; } = string.Empty;

    public DateTime SentAt { get; set; } = DateTime.UtcNow;

    public bool IsRead { get; set; }

    public DateTime? ReadAt { get; set; }

    /// <summary>
    /// MessageType: Text, Image, File, CallOffer, CallAnswer, IceCandidate, CallEnd
    /// </summary>
    [MaxLength(20)]
    public string MessageType { get; set; } = "Text";

    [MaxLength(64)]
    public string? ClientMessageId { get; set; }

    /// <summary>
    /// Dùng cho WebRTC signaling payload (CallOffer, CallAnswer, IceCandidate)
    /// </summary>
    public string? SignalingData { get; set; }

    [ForeignKey(nameof(ConversationId))]
    public ChatConversation Conversation { get; set; } = null!;

    [ForeignKey(nameof(SenderId))]
    public Employee Sender { get; set; } = null!;
}
