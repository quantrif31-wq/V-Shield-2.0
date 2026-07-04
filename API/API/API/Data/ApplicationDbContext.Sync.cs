using API.Models;
using Microsoft.EntityFrameworkCore;

namespace API.Data;

public partial class ApplicationDbContext
{
    public DbSet<SyncAreaNode> SyncAreaNodes { get; set; }
    public DbSet<SyncAreaAssignment> SyncAreaAssignments { get; set; }
    public DbSet<SyncInboundEvent> SyncInboundEvents { get; set; }
    public DbSet<SyncOutboundCheckpoint> SyncOutboundCheckpoints { get; set; }

    private static void ConfigureSyncModels(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<SyncAreaNode>(entity =>
        {
            entity.HasKey(e => e.AreaNodeId);
            entity.Property(e => e.AreaNodeId).HasMaxLength(120);
            entity.Property(e => e.DisplayName).IsRequired().HasMaxLength(160);
            entity.Property(e => e.NodeSecretHash).HasMaxLength(200);
            entity.Property(e => e.Mode).IsRequired().HasMaxLength(32);
            entity.Property(e => e.Status).IsRequired().HasMaxLength(32);
            entity.Property(e => e.Version).HasMaxLength(40);
        });

        modelBuilder.Entity<SyncAreaAssignment>(entity =>
        {
            entity.HasKey(e => e.SyncAreaAssignmentId);
            entity.Property(e => e.AreaNodeId).IsRequired().HasMaxLength(120);
            entity.Property(e => e.ScopeType).IsRequired().HasMaxLength(40);
            entity.HasIndex(e => new { e.AreaNodeId, e.ScopeType, e.ScopeId }).IsUnique();
            entity.HasOne(e => e.AreaNode)
                .WithMany(e => e.Assignments)
                .HasForeignKey(e => e.AreaNodeId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<SyncInboundEvent>(entity =>
        {
            entity.HasKey(e => e.SyncInboundEventId);
            entity.Property(e => e.AreaNodeId).IsRequired().HasMaxLength(120);
            entity.Property(e => e.ScopeType).HasMaxLength(40);
            entity.Property(e => e.EventType).IsRequired().HasMaxLength(120);
            entity.Property(e => e.AggregateType).IsRequired().HasMaxLength(120);
            entity.Property(e => e.AggregateId).HasMaxLength(120);
            entity.Property(e => e.CorrelationId).IsRequired().HasMaxLength(80);
            entity.Property(e => e.SourceSystem).IsRequired().HasMaxLength(40);
            entity.Property(e => e.PayloadJson).IsRequired().HasMaxLength(4000);
            entity.Property(e => e.Status).IsRequired().HasMaxLength(40);
            entity.Property(e => e.FailureReason).HasMaxLength(240);
            entity.Property(e => e.AppliedAggregateId).HasMaxLength(120);
            entity.HasIndex(e => new { e.AreaNodeId, e.CorrelationId }).IsUnique();
            entity.HasIndex(e => new { e.AreaNodeId, e.AggregateType, e.AggregateId });
        });

        modelBuilder.Entity<SyncOutboundCheckpoint>(entity =>
        {
            entity.HasKey(e => e.AreaNodeId);
            entity.Property(e => e.AreaNodeId).HasMaxLength(120);
        });
    }
}
