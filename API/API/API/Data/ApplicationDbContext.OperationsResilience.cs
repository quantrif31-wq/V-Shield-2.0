using API.Models;
using Microsoft.EntityFrameworkCore;

namespace API.Data;

public partial class ApplicationDbContext
{
    public DbSet<OutboxEvent> OutboxEvents { get; set; }
    public DbSet<WebhookSubscription> WebhookSubscriptions { get; set; }
    public DbSet<WebhookDelivery> WebhookDeliveries { get; set; }
    public DbSet<RuntimeDependencyHealth> RuntimeDependencyHealths { get; set; }
    public DbSet<BackupRun> BackupRuns { get; set; }
    public DbSet<RestoreDrill> RestoreDrills { get; set; }
    public DbSet<SecurityOperationsCheck> SecurityOperationsChecks { get; set; }

    private static void ConfigureOperationsResilience(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<OutboxEvent>(entity =>
        {
            entity.HasKey(e => e.OutboxEventId);
            entity.Property(e => e.Channel).IsRequired().HasMaxLength(40);
            entity.Property(e => e.EventType).IsRequired().HasMaxLength(120);
            entity.Property(e => e.AggregateType).IsRequired().HasMaxLength(120);
            entity.Property(e => e.AggregateId).HasMaxLength(120);
            entity.Property(e => e.PayloadJson).IsRequired().HasMaxLength(4000);
            entity.Property(e => e.Status).IsRequired().HasMaxLength(40);
            entity.Property(e => e.AreaNodeId).HasMaxLength(120);
            entity.Property(e => e.ScopeType).HasMaxLength(40);
            entity.Property(e => e.SourceSystem).IsRequired().HasMaxLength(40);
            entity.Property(e => e.CorrelationId).IsRequired().HasMaxLength(80);
            entity.HasIndex(e => new { e.Channel, e.Status, e.NextAttemptAtUtc, e.CreatedAtUtc });
            entity.HasIndex(e => new { e.Channel, e.SiteId, e.ScopeType, e.ScopeId, e.OutboxEventId });
            entity.HasIndex(e => new { e.Channel, e.AreaNodeId, e.CorrelationId });
        });

        modelBuilder.Entity<WebhookSubscription>(entity =>
        {
            entity.HasKey(e => e.WebhookSubscriptionId);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(160);
            entity.Property(e => e.TargetUrl).IsRequired().HasMaxLength(500);
            entity.Property(e => e.SecretReference).IsRequired().HasMaxLength(240);
            entity.Property(e => e.EventTypes).IsRequired().HasMaxLength(1000);
        });

        modelBuilder.Entity<WebhookDelivery>(entity =>
        {
            entity.HasKey(e => e.WebhookDeliveryId);
            entity.Property(e => e.Status).IsRequired().HasMaxLength(40);
            entity.Property(e => e.ResponseBody).HasMaxLength(1000);
            entity.Property(e => e.Signature).IsRequired().HasMaxLength(240);
            entity.HasOne(e => e.Subscription).WithMany().HasForeignKey(e => e.WebhookSubscriptionId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.OutboxEvent).WithMany().HasForeignKey(e => e.OutboxEventId).OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<RuntimeDependencyHealth>(entity =>
        {
            entity.HasKey(e => e.RuntimeDependencyHealthId);
            entity.Property(e => e.DependencyName).IsRequired().HasMaxLength(120);
            entity.Property(e => e.DependencyType).IsRequired().HasMaxLength(80);
            entity.Property(e => e.Status).IsRequired().HasMaxLength(40);
            entity.Property(e => e.Message).HasMaxLength(1000);
            entity.HasIndex(e => new { e.DependencyName, e.ObservedAtUtc });
        });

        modelBuilder.Entity<BackupRun>(entity =>
        {
            entity.HasKey(e => e.BackupRunId);
            entity.Property(e => e.Profile).IsRequired().HasMaxLength(120);
            entity.Property(e => e.Status).IsRequired().HasMaxLength(40);
            entity.Property(e => e.BackupReference).HasMaxLength(500);
            entity.Property(e => e.Notes).HasMaxLength(1000);
            entity.HasIndex(e => new { e.Profile, e.StartedAtUtc });
        });

        modelBuilder.Entity<RestoreDrill>(entity =>
        {
            entity.HasKey(e => e.RestoreDrillId);
            entity.Property(e => e.Profile).IsRequired().HasMaxLength(120);
            entity.Property(e => e.Status).IsRequired().HasMaxLength(40);
            entity.Property(e => e.Findings).HasMaxLength(2000);
            entity.HasOne(e => e.BackupRun).WithMany().HasForeignKey(e => e.BackupRunId).OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<SecurityOperationsCheck>(entity =>
        {
            entity.HasKey(e => e.SecurityOperationsCheckId);
            entity.Property(e => e.CheckType).IsRequired().HasMaxLength(120);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(160);
            entity.Property(e => e.Status).IsRequired().HasMaxLength(40);
            entity.Property(e => e.Evidence).HasMaxLength(1000);
        });
    }
}
