using API.Models;
using Microsoft.EntityFrameworkCore;

namespace API.Data;

public partial class ApplicationDbContext
{
    public DbSet<EvidenceItem> EvidenceItems { get; set; }
    public DbSet<EvidenceCollection> EvidenceCollections { get; set; }
    public DbSet<EvidenceCollectionItem> EvidenceCollectionItems { get; set; }
    public DbSet<EvidenceAccessLog> EvidenceAccessLogs { get; set; }
    public DbSet<EvidenceExportRequest> EvidenceExportRequests { get; set; }
    public DbSet<RetentionPolicy> RetentionPolicies { get; set; }
    public DbSet<LegalHold> LegalHolds { get; set; }
    public DbSet<ChainOfCustodyEntry> ChainOfCustodyEntries { get; set; }
    public DbSet<RedactionRequest> RedactionRequests { get; set; }
    public DbSet<ComplianceReportRun> ComplianceReportRuns { get; set; }

    private static void ConfigureEvidenceComplianceGovernance(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<EvidenceItem>(entity =>
        {
            entity.HasKey(e => e.EvidenceItemId);
            entity.Property(e => e.EvidenceType).IsRequired().HasMaxLength(80);
            entity.Property(e => e.SourceType).IsRequired().HasMaxLength(80);
            entity.Property(e => e.SourceReference).HasMaxLength(240);
            entity.Property(e => e.StorageReference).IsRequired().HasMaxLength(500);
            entity.Property(e => e.HashSha256).IsRequired().HasMaxLength(128);
            entity.Property(e => e.PrivacyLabel).IsRequired().HasMaxLength(80);
            entity.Property(e => e.RetentionCategory).IsRequired().HasMaxLength(80);
            entity.Property(e => e.LastHashVerificationStatus).IsRequired().HasMaxLength(40).HasDefaultValue("NotVerified");
            entity.Property(e => e.PurgeReason).HasMaxLength(500);
            entity.HasIndex(e => new { e.EvidenceType, e.PrivacyLabel, e.CreatedAtUtc });
            entity.HasIndex(e => e.HashSha256);
            entity.HasIndex(e => new { e.RetentionCategory, e.PurgedAtUtc, e.IsLegalHold });
        });

        modelBuilder.Entity<EvidenceCollection>(entity =>
        {
            entity.HasKey(e => e.EvidenceCollectionId);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(160);
            entity.Property(e => e.Purpose).IsRequired().HasMaxLength(160);
            entity.Property(e => e.BundleHash).HasMaxLength(128);
            entity.Property(e => e.Status).IsRequired().HasMaxLength(40);
        });

        modelBuilder.Entity<EvidenceCollectionItem>(entity =>
        {
            entity.HasKey(e => e.EvidenceCollectionItemId);
            entity.HasIndex(e => new { e.EvidenceCollectionId, e.EvidenceItemId }).IsUnique();
            entity.HasOne(e => e.Collection).WithMany().HasForeignKey(e => e.EvidenceCollectionId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.EvidenceItem).WithMany().HasForeignKey(e => e.EvidenceItemId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<EvidenceAccessLog>(entity =>
        {
            entity.HasKey(e => e.EvidenceAccessLogId);
            entity.Property(e => e.AccessType).IsRequired().HasMaxLength(80);
            entity.Property(e => e.Purpose).IsRequired().HasMaxLength(500);
            entity.HasIndex(e => new { e.EvidenceItemId, e.AccessedAtUtc });
            entity.HasOne(e => e.EvidenceItem).WithMany().HasForeignKey(e => e.EvidenceItemId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<EvidenceExportRequest>(entity =>
        {
            entity.HasKey(e => e.EvidenceExportRequestId);
            entity.Property(e => e.Purpose).IsRequired().HasMaxLength(500);
            entity.Property(e => e.Recipient).IsRequired().HasMaxLength(240);
            entity.Property(e => e.Status).IsRequired().HasMaxLength(40);
            entity.Property(e => e.ExportHash).HasMaxLength(128);
            entity.Property(e => e.Watermark).HasMaxLength(240);
            entity.Property(e => e.SignatureReference).HasMaxLength(240);
            entity.HasIndex(e => new { e.Status, e.RequestedAtUtc });
        });

        modelBuilder.Entity<RetentionPolicy>(entity =>
        {
            entity.HasKey(e => e.RetentionPolicyId);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(160);
            entity.Property(e => e.EvidenceType).IsRequired().HasMaxLength(80);
            entity.Property(e => e.RetentionCategory).IsRequired().HasMaxLength(80);
            entity.Property(e => e.PurgeMode).IsRequired().HasMaxLength(40);
            entity.HasIndex(e => new { e.EvidenceType, e.RetentionCategory, e.IsActive });
        });

        modelBuilder.Entity<LegalHold>(entity =>
        {
            entity.HasKey(e => e.LegalHoldId);
            entity.Property(e => e.Reason).IsRequired().HasMaxLength(500);
            entity.Property(e => e.Status).IsRequired().HasMaxLength(40);
            entity.HasIndex(e => new { e.Status, e.AppliedAtUtc });
        });

        modelBuilder.Entity<ChainOfCustodyEntry>(entity =>
        {
            entity.HasKey(e => e.ChainOfCustodyEntryId);
            entity.Property(e => e.Action).IsRequired().HasMaxLength(80);
            entity.Property(e => e.FromCustodian).HasMaxLength(160);
            entity.Property(e => e.ToCustodian).HasMaxLength(160);
            entity.Property(e => e.HashBefore).HasMaxLength(128);
            entity.Property(e => e.HashAfter).HasMaxLength(128);
            entity.Property(e => e.Note).HasMaxLength(1000);
            entity.HasOne(e => e.EvidenceItem).WithMany().HasForeignKey(e => e.EvidenceItemId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<RedactionRequest>(entity =>
        {
            entity.HasKey(e => e.RedactionRequestId);
            entity.Property(e => e.Reason).IsRequired().HasMaxLength(500);
            entity.Property(e => e.PrivacyLabel).IsRequired().HasMaxLength(80);
            entity.Property(e => e.Status).IsRequired().HasMaxLength(40);
            entity.Property(e => e.RedactedStorageReference).HasMaxLength(500);
            entity.HasOne(e => e.EvidenceItem).WithMany().HasForeignKey(e => e.EvidenceItemId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<ComplianceReportRun>(entity =>
        {
            entity.HasKey(e => e.ComplianceReportRunId);
            entity.Property(e => e.ReportType).IsRequired().HasMaxLength(120);
            entity.Property(e => e.Status).IsRequired().HasMaxLength(40);
            entity.Property(e => e.OutputReference).HasMaxLength(500);
            entity.HasIndex(e => new { e.ReportType, e.CreatedAtUtc });
        });
    }
}
