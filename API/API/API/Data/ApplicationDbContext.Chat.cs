using API.Models;
using Microsoft.EntityFrameworkCore;

namespace API.Data;

public partial class ApplicationDbContext
{
    public DbSet<ChatMessage> ChatMessages => Set<ChatMessage>();
    public DbSet<ChatConversation> ChatConversations => Set<ChatConversation>();
    public DbSet<ChatParticipant> ChatParticipants => Set<ChatParticipant>();

    internal static void ConfigureChatModels(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ChatMessage>(entity =>
        {
            entity.ToTable("ChatMessages");
            entity.HasKey(e => e.MessageId);
            entity.Property(e => e.Content).HasMaxLength(2000);
            entity.Property(e => e.MessageType).HasMaxLength(20);
            entity.Property(e => e.ClientMessageId).HasMaxLength(64);
            entity.HasOne(e => e.Conversation)
                  .WithMany(c => c.Messages)
                  .HasForeignKey(e => e.ConversationId)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.Sender)
                  .WithMany()
                  .HasForeignKey(e => e.SenderId)
                  .OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(e => e.ConversationId);
            entity.HasIndex(e => e.SentAt);
            entity.HasIndex(e => new { e.ConversationId, e.SenderId, e.ClientMessageId })
                  .IsUnique()
                  .HasFilter("[ClientMessageId] IS NOT NULL");
        });

        modelBuilder.Entity<ChatConversation>(entity =>
        {
            entity.ToTable("ChatConversations");
            entity.HasKey(e => e.ConversationId);
        });

        modelBuilder.Entity<ChatParticipant>(entity =>
        {
            entity.ToTable("ChatParticipants");
            entity.HasKey(e => e.ParticipantId);
            entity.HasOne(e => e.Conversation)
                  .WithMany(c => c.Participants)
                  .HasForeignKey(e => e.ConversationId)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.Employee)
                  .WithMany()
                  .HasForeignKey(e => e.EmployeeId)
                  .OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(e => new { e.ConversationId, e.EmployeeId }).IsUnique();
        });
    }
}
