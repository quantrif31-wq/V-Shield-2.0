using API.Models;
using Microsoft.EntityFrameworkCore;

namespace API.Data;

public partial class ApplicationDbContext
{
    public DbSet<Alarm> Alarms { get; set; }
    public DbSet<AlarmRule> AlarmRules { get; set; }
    public DbSet<AlarmComment> AlarmComments { get; set; }
    public DbSet<SopTemplate> SopTemplates { get; set; }
    public DbSet<SopExecution> SopExecutions { get; set; }
    public DbSet<Incident> Incidents { get; set; }
    public DbSet<IncidentTimelineItem> IncidentTimelineItems { get; set; }
    public DbSet<DispatchTask> DispatchTasks { get; set; }
    public DbSet<ShiftHandover> ShiftHandovers { get; set; }
    public DbSet<EmergencyMusterSnapshot> EmergencyMusterSnapshots { get; set; }

    private static void ConfigureSocIncidentOperations(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Alarm>(entity =>
        {
            entity.HasKey(e => e.AlarmId);
            entity.Property(e => e.AlarmType).IsRequired().HasMaxLength(80);
            entity.Property(e => e.Severity).IsRequired().HasMaxLength(40);
            entity.Property(e => e.State).IsRequired().HasMaxLength(40);
            entity.Property(e => e.Summary).IsRequired().HasMaxLength(2000);
            entity.HasIndex(e => new { e.State, e.Severity, e.CreatedAtUtc });
        });

        modelBuilder.Entity<AlarmRule>(entity =>
        {
            entity.HasKey(e => e.AlarmRuleId);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(160);
            entity.Property(e => e.EventType).IsRequired().HasMaxLength(80);
            entity.Property(e => e.Severity).IsRequired().HasMaxLength(40);
        });

        modelBuilder.Entity<AlarmComment>(entity =>
        {
            entity.HasKey(e => e.AlarmCommentId);
            entity.Property(e => e.Comment).IsRequired().HasMaxLength(2000);
            entity.HasOne(e => e.Alarm).WithMany().HasForeignKey(e => e.AlarmId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<SopTemplate>(entity =>
        {
            entity.HasKey(e => e.SopTemplateId);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(160);
            entity.Property(e => e.AlarmType).IsRequired().HasMaxLength(80);
            entity.Property(e => e.ChecklistJson).IsRequired().HasMaxLength(4000);
        });

        modelBuilder.Entity<SopExecution>(entity =>
        {
            entity.HasKey(e => e.SopExecutionId);
            entity.Property(e => e.Status).IsRequired().HasMaxLength(40);
            entity.Property(e => e.CompletedStepsJson).IsRequired().HasMaxLength(4000);
        });

        modelBuilder.Entity<Incident>(entity =>
        {
            entity.HasKey(e => e.IncidentId);
            entity.Property(e => e.Title).IsRequired().HasMaxLength(160);
            entity.Property(e => e.Severity).IsRequired().HasMaxLength(40);
            entity.Property(e => e.Status).IsRequired().HasMaxLength(40);
            entity.HasIndex(e => new { e.Status, e.Severity, e.OpenedAtUtc });
        });

        modelBuilder.Entity<IncidentTimelineItem>(entity =>
        {
            entity.HasKey(e => e.IncidentTimelineItemId);
            entity.Property(e => e.ItemType).IsRequired().HasMaxLength(80);
            entity.Property(e => e.Text).IsRequired().HasMaxLength(2000);
            entity.HasOne(e => e.Incident).WithMany().HasForeignKey(e => e.IncidentId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<DispatchTask>(entity =>
        {
            entity.HasKey(e => e.DispatchTaskId);
            entity.Property(e => e.LocationText).IsRequired().HasMaxLength(160);
            entity.Property(e => e.Priority).IsRequired().HasMaxLength(40);
            entity.Property(e => e.Status).IsRequired().HasMaxLength(40);
            entity.Property(e => e.Instructions).IsRequired().HasMaxLength(2000);
            entity.HasIndex(e => new { e.Status, e.Priority, e.CreatedAtUtc });
        });

        modelBuilder.Entity<ShiftHandover>(entity =>
        {
            entity.HasKey(e => e.ShiftHandoverId);
            entity.Property(e => e.Summary).IsRequired().HasMaxLength(4000);
        });

        modelBuilder.Entity<EmergencyMusterSnapshot>(entity =>
        {
            entity.HasKey(e => e.EmergencyMusterSnapshotId);
            entity.HasIndex(e => new { e.SiteId, e.CapturedAtUtc });
        });
    }
}

