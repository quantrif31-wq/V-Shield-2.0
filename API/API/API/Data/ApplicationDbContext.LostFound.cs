using API.Models;
using Microsoft.EntityFrameworkCore;

namespace API.Data;

public partial class ApplicationDbContext
{
    public DbSet<LostItemReport> LostItemReports { get; set; }
    public DbSet<FoundItemReport> FoundItemReports { get; set; }
    public DbSet<ItemMatch> ItemMatches { get; set; }
    public DbSet<ClaimRequest> ClaimRequests { get; set; }
    public DbSet<LockerCabinet> LockerCabinets { get; set; }
    public DbSet<LockerCompartment> LockerCompartments { get; set; }
    public DbSet<LockerAccessLog> LockerAccessLogs { get; set; }

    private static void ConfigureLostFound(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<LostItemReport>(entity =>
        {
            entity.HasKey(e => e.LostItemReportId);
            entity.Property(e => e.ReporterName).IsRequired().HasMaxLength(120);
            entity.Property(e => e.ReporterPhone).IsRequired().HasMaxLength(20);
            entity.Property(e => e.ReporterEmail).HasMaxLength(240);
            entity.Property(e => e.ItemDescription).IsRequired().HasMaxLength(1000);
            entity.Property(e => e.LastSeenLocation).HasMaxLength(240);
            entity.Property(e => e.PhotoUrl).HasMaxLength(500);
            entity.Property(e => e.Status).IsRequired().HasMaxLength(40);
            entity.HasIndex(e => new { e.Status, e.CreatedAtUtc });
        });

        modelBuilder.Entity<FoundItemReport>(entity =>
        {
            entity.HasKey(e => e.FoundItemReportId);
            entity.Property(e => e.FoundByName).IsRequired().HasMaxLength(120);
            entity.Property(e => e.FoundLocation).IsRequired().HasMaxLength(240);
            entity.Property(e => e.ItemDescription).IsRequired().HasMaxLength(1000);
            entity.Property(e => e.PhotoUrl).HasMaxLength(500);
            entity.Property(e => e.StorageLocation).HasMaxLength(500);
            entity.Property(e => e.Status).IsRequired().HasMaxLength(40);
            entity.HasIndex(e => new { e.Status, e.CreatedAtUtc });
            entity.HasOne(e => e.LockerCompartment).WithMany().HasForeignKey(e => e.LockerCompartmentId).OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<ItemMatch>(entity =>
        {
            entity.HasKey(e => e.ItemMatchId);
            entity.Property(e => e.Status).IsRequired().HasMaxLength(40);
            entity.Property(e => e.Note).HasMaxLength(500);
            entity.HasOne(e => e.LostItem).WithMany().HasForeignKey(e => e.LostItemReportId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.FoundItem).WithMany().HasForeignKey(e => e.FoundItemReportId).OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(e => new { e.Status, e.MatchedAtUtc });
        });

        modelBuilder.Entity<ClaimRequest>(entity =>
        {
            entity.HasKey(e => e.ClaimRequestId);
            entity.Property(e => e.ClaimantName).IsRequired().HasMaxLength(120);
            entity.Property(e => e.ClaimantIdNumber).IsRequired().HasMaxLength(40);
            entity.Property(e => e.ClaimantPhone).IsRequired().HasMaxLength(20);
            entity.Property(e => e.ProofDocumentUrl).HasMaxLength(500);
            entity.Property(e => e.Status).IsRequired().HasMaxLength(40);
            entity.HasOne(e => e.FoundItem).WithMany().HasForeignKey(e => e.FoundItemReportId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.LostItem).WithMany().HasForeignKey(e => e.LostItemReportId).OnDelete(DeleteBehavior.SetNull);
            entity.HasIndex(e => new { e.Status, e.RequestedAtUtc });
        });

        modelBuilder.Entity<LockerCabinet>(entity =>
        {
            entity.HasKey(e => e.LockerCabinetId);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(120);
            entity.Property(e => e.Location).HasMaxLength(240);
            entity.Property(e => e.Description).HasMaxLength(500);
        });

        modelBuilder.Entity<LockerCompartment>(entity =>
        {
            entity.HasKey(e => e.LockerCompartmentId);
            entity.Property(e => e.Code).IsRequired().HasMaxLength(20);
            entity.Property(e => e.Status).IsRequired().HasMaxLength(40);
            entity.HasOne(e => e.Cabinet).WithMany().HasForeignKey(e => e.LockerCabinetId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.EvidenceItem).WithMany().HasForeignKey(e => e.EvidenceItemId).OnDelete(DeleteBehavior.SetNull);
            entity.HasIndex(e => new { e.LockerCabinetId, e.Code }).IsUnique();
            entity.HasIndex(e => new { e.Status, e.LockerCabinetId });
        });

        modelBuilder.Entity<LockerAccessLog>(entity =>
        {
            entity.HasKey(e => e.LockerAccessLogId);
            entity.Property(e => e.Action).IsRequired().HasMaxLength(80);
            entity.Property(e => e.Purpose).HasMaxLength(500);
            entity.HasOne(e => e.Compartment).WithMany().HasForeignKey(e => e.LockerCompartmentId).OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(e => new { e.LockerCompartmentId, e.Timestamp });
        });
    }
}
