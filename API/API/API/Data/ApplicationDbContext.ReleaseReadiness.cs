using API.Models;
using Microsoft.EntityFrameworkCore;

namespace API.Data;

public partial class ApplicationDbContext
{
    public DbSet<QaTestRun> QaTestRuns { get; set; }
    public DbSet<ReleaseCandidate> ReleaseCandidates { get; set; }
    public DbSet<ReleaseGateCheck> ReleaseGateChecks { get; set; }
    public DbSet<RunbookAcknowledgement> RunbookAcknowledgements { get; set; }

    private static void ConfigureReleaseReadiness(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<QaTestRun>(entity =>
        {
            entity.HasKey(e => e.QaTestRunId);
            entity.Property(e => e.TestType).IsRequired().HasMaxLength(80);
            entity.Property(e => e.Profile).IsRequired().HasMaxLength(120);
            entity.Property(e => e.Status).IsRequired().HasMaxLength(40);
            entity.Property(e => e.EvidenceReference).HasMaxLength(500);
            entity.Property(e => e.Notes).HasMaxLength(1000);
            entity.HasIndex(e => new { e.TestType, e.Status, e.StartedAtUtc });
        });

        modelBuilder.Entity<ReleaseCandidate>(entity =>
        {
            entity.HasKey(e => e.ReleaseCandidateId);
            entity.Property(e => e.Version).IsRequired().HasMaxLength(80);
            entity.Property(e => e.Status).IsRequired().HasMaxLength(40);
            entity.Property(e => e.MigrationId).HasMaxLength(160);
            entity.Property(e => e.BuildReference).HasMaxLength(240);
            entity.HasIndex(e => new { e.Version, e.Status });
        });

        modelBuilder.Entity<ReleaseGateCheck>(entity =>
        {
            entity.HasKey(e => e.ReleaseGateCheckId);
            entity.Property(e => e.GateName).IsRequired().HasMaxLength(120);
            entity.Property(e => e.Status).IsRequired().HasMaxLength(40);
            entity.Property(e => e.EvidenceReference).HasMaxLength(500);
            entity.Property(e => e.Notes).HasMaxLength(1000);
            entity.HasOne(e => e.ReleaseCandidate).WithMany().HasForeignKey(e => e.ReleaseCandidateId).OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(e => new { e.ReleaseCandidateId, e.GateName });
        });

        modelBuilder.Entity<RunbookAcknowledgement>(entity =>
        {
            entity.HasKey(e => e.RunbookAcknowledgementId);
            entity.Property(e => e.RunbookName).IsRequired().HasMaxLength(120);
            entity.Property(e => e.RoleName).IsRequired().HasMaxLength(80);
            entity.Property(e => e.EvidenceReference).HasMaxLength(240);
            entity.HasIndex(e => new { e.RunbookName, e.RoleName, e.AcknowledgedAtUtc });
        });
    }
}
