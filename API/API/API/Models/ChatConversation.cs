using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace API.Models;

[Table("ChatConversations")]
public class ChatConversation
{
    [Key]
    public int ConversationId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [MaxLength(200)]
    public string? Title { get; set; }

    public ICollection<ChatParticipant> Participants { get; set; } = new List<ChatParticipant>();
    public ICollection<ChatMessage> Messages { get; set; } = new List<ChatMessage>();
}

[Table("ChatParticipants")]
public class ChatParticipant
{
    [Key]
    public int ParticipantId { get; set; }

    public int ConversationId { get; set; }

    public int EmployeeId { get; set; }

    public DateTime? LastReadAt { get; set; }

    [ForeignKey(nameof(ConversationId))]
    public ChatConversation Conversation { get; set; } = null!;

    [ForeignKey(nameof(EmployeeId))]
    public Employee Employee { get; set; } = null!;
}
