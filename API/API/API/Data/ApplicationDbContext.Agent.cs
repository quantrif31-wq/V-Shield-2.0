using API.Models;
using Microsoft.EntityFrameworkCore;

namespace API.Data;

public partial class ApplicationDbContext
{
    public DbSet<AgentThread> AgentThreads => Set<AgentThread>();
    public DbSet<AgentMessage> AgentMessages => Set<AgentMessage>();
    public DbSet<AgentDraft> AgentDrafts => Set<AgentDraft>();
    public DbSet<AgentAuditLog> AgentAuditLogs => Set<AgentAuditLog>();

    internal static void ConfigureAgentModels(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AgentThread>(entity =>
        {
            entity.ToTable("AgentThreads");
            entity.HasKey(e => e.AgentThreadId);
            entity.Property(e => e.Summary).HasColumnType("nvarchar(max)");
            entity.Property(e => e.FactBlob).HasColumnType("nvarchar(max)");
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.UpdatedAt);
        });

        modelBuilder.Entity<AgentMessage>(entity =>
        {
            entity.ToTable("AgentMessages");
            entity.HasKey(e => e.AgentMessageId);
            entity.Property(e => e.Role).HasMaxLength(20);
            entity.Property(e => e.Content).HasColumnType("nvarchar(max)");
            entity.HasIndex(e => e.AgentThreadId);
            entity.HasIndex(e => e.CreatedAt);
        });

        modelBuilder.Entity<AgentDraft>(entity =>
        {
            entity.ToTable("AgentDrafts");
            entity.HasKey(e => e.AgentDraftId);
            entity.Property(e => e.Status).HasMaxLength(20);
            entity.Property(e => e.To).HasColumnType("nvarchar(max)");
            entity.Property(e => e.Subject).HasColumnType("nvarchar(max)");
            entity.Property(e => e.Body).HasColumnType("nvarchar(max)");
            entity.HasIndex(e => e.AgentThreadId);
            entity.HasIndex(e => e.UserId);
        });

        modelBuilder.Entity<AgentAuditLog>(entity =>
        {
            entity.ToTable("AgentAuditLogs");
            entity.HasKey(e => e.AgentAuditId);
            entity.Property(e => e.ToolName).HasMaxLength(60);
            entity.Property(e => e.ArgsJson).HasColumnType("nvarchar(max)");
            entity.Property(e => e.ResultSummary).HasColumnType("nvarchar(max)");
            entity.Property(e => e.Status).HasMaxLength(20);
            entity.HasIndex(e => e.AgentThreadId);
            entity.HasIndex(e => e.CreatedAt);
        });
    }
}