using API.Models;
using Microsoft.EntityFrameworkCore;

namespace API.Data;

public partial class ApplicationDbContext
{
    public DbSet<AccessSchedule> AccessSchedules { get; set; }
    public DbSet<HolidayCalendar> HolidayCalendars { get; set; }
    public DbSet<AccessLevel> AccessLevels { get; set; }
    public DbSet<AccessGroup> AccessGroups { get; set; }
    public DbSet<AccessRule> AccessRules { get; set; }
    public DbSet<TemporaryAccessGrant> TemporaryAccessGrants { get; set; }
    public DbSet<AccessPolicyVersion> AccessPolicyVersions { get; set; }
    public DbSet<AccessDecision> AccessDecisions { get; set; }
    public DbSet<AntiPassbackState> AntiPassbackStates { get; set; }
    public DbSet<OccupancySnapshot> OccupancySnapshots { get; set; }
    public DbSet<EmergencyState> EmergencyStates { get; set; }
    public DbSet<DuressEvent> DuressEvents { get; set; }

    private static void ConfigureAccessPolicyEngine(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AccessSchedule>(entity =>
        {
            entity.HasKey(e => e.AccessScheduleId);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(160);
            entity.Property(e => e.DaysOfWeek).IsRequired().HasMaxLength(40);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
        });

        modelBuilder.Entity<HolidayCalendar>(entity =>
        {
            entity.HasKey(e => e.HolidayCalendarId);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(160);
            entity.HasIndex(e => new { e.SiteId, e.HolidayDate });
            entity.HasOne(e => e.Site)
                .WithMany()
                .HasForeignKey(e => e.SiteId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<AccessLevel>(entity =>
        {
            entity.HasKey(e => e.AccessLevelId);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(160);
            entity.Property(e => e.Code).IsRequired().HasMaxLength(50);
            entity.HasIndex(e => e.Code).IsUnique();
        });

        modelBuilder.Entity<AccessGroup>(entity =>
        {
            entity.HasKey(e => e.AccessGroupId);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(160);
            entity.Property(e => e.Code).IsRequired().HasMaxLength(50);
            entity.HasIndex(e => e.Code).IsUnique();
        });

        modelBuilder.Entity<AccessRule>(entity =>
        {
            entity.HasKey(e => e.AccessRuleId);
            entity.Property(e => e.SubjectType).IsRequired().HasMaxLength(40);
            entity.Property(e => e.CredentialType).IsRequired().HasMaxLength(40);
            entity.HasIndex(e => new { e.SubjectType, e.SubjectId, e.SiteId, e.SecurityZoneId, e.AccessPointId });
            entity.HasOne(e => e.AccessPolicyVersion)
                .WithMany(v => v.Rules)
                .HasForeignKey(e => e.AccessPolicyVersionId)
                .OnDelete(DeleteBehavior.SetNull);
            entity.HasOne(e => e.AccessLevel)
                .WithMany()
                .HasForeignKey(e => e.AccessLevelId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.AccessGroup)
                .WithMany()
                .HasForeignKey(e => e.AccessGroupId)
                .OnDelete(DeleteBehavior.SetNull);
            entity.HasOne(e => e.Site)
                .WithMany()
                .HasForeignKey(e => e.SiteId)
                .OnDelete(DeleteBehavior.SetNull);
            entity.HasOne(e => e.SecurityZone)
                .WithMany()
                .HasForeignKey(e => e.SecurityZoneId)
                .OnDelete(DeleteBehavior.SetNull);
            entity.HasOne(e => e.AccessPoint)
                .WithMany()
                .HasForeignKey(e => e.AccessPointId)
                .OnDelete(DeleteBehavior.SetNull);
            entity.HasOne(e => e.Schedule)
                .WithMany()
                .HasForeignKey(e => e.AccessScheduleId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<TemporaryAccessGrant>(entity =>
        {
            entity.HasKey(e => e.TemporaryAccessGrantId);
            entity.Property(e => e.SubjectType).IsRequired().HasMaxLength(40);
            entity.Property(e => e.Reason).IsRequired().HasMaxLength(1000);
            entity.HasIndex(e => new { e.SubjectType, e.SubjectId, e.ValidToUtc });
            entity.HasOne(e => e.ApprovedByUser)
                .WithMany()
                .HasForeignKey(e => e.ApprovedByUserId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<AccessPolicyVersion>(entity =>
        {
            entity.HasKey(e => e.AccessPolicyVersionId);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(160);
            entity.Property(e => e.Status).IsRequired().HasMaxLength(40);
            entity.Property(e => e.ChangeSummary).HasMaxLength(1000);
            entity.HasIndex(e => new { e.Status, e.CreatedAtUtc });
            entity.HasOne(e => e.CreatedByUser)
                .WithMany()
                .HasForeignKey(e => e.CreatedByUserId)
                .OnDelete(DeleteBehavior.SetNull);
            entity.HasOne(e => e.ApprovedByUser)
                .WithMany()
                .HasForeignKey(e => e.ApprovedByUserId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<AccessDecision>(entity =>
        {
            entity.HasKey(e => e.AccessDecisionId);
            entity.Property(e => e.SubjectType).IsRequired().HasMaxLength(40);
            entity.Property(e => e.CredentialType).IsRequired().HasMaxLength(40);
            entity.Property(e => e.Result).IsRequired().HasMaxLength(20);
            entity.Property(e => e.Reason).IsRequired().HasMaxLength(1000);
            entity.Property(e => e.DecisionMode).IsRequired().HasMaxLength(40).HasDefaultValue("Enforced");
            entity.Property(e => e.LegacyResult).HasMaxLength(20);
            entity.HasIndex(e => new { e.SubjectType, e.SubjectId, e.EvaluatedAtUtc });
            entity.HasOne(e => e.AccessPolicyVersion)
                .WithMany()
                .HasForeignKey(e => e.AccessPolicyVersionId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<AntiPassbackState>(entity =>
        {
            entity.HasKey(e => e.AntiPassbackStateId);
            entity.Property(e => e.SubjectType).IsRequired().HasMaxLength(40);
            entity.Property(e => e.State).IsRequired().HasMaxLength(40);
            entity.HasIndex(e => new { e.SubjectType, e.SubjectId, e.SecurityZoneId }).IsUnique();
        });

        modelBuilder.Entity<OccupancySnapshot>(entity =>
        {
            entity.HasKey(e => e.OccupancySnapshotId);
            entity.HasIndex(e => new { e.SiteId, e.SecurityZoneId, e.CapturedAtUtc });
        });

        modelBuilder.Entity<EmergencyState>(entity =>
        {
            entity.HasKey(e => e.EmergencyStateId);
            entity.Property(e => e.State).IsRequired().HasMaxLength(40);
            entity.Property(e => e.Reason).IsRequired().HasMaxLength(1000);
            entity.HasIndex(e => new { e.SiteId, e.SecurityZoneId, e.AccessPointId, e.IsActive });
        });
    }
}
