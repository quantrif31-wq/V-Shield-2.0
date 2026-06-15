using API.Models;
using Microsoft.EntityFrameworkCore;

namespace API.Data;

public partial class ApplicationDbContext
{
    public DbSet<Visit> Visits { get; set; }
    public DbSet<VisitorCredential> VisitorCredentials { get; set; }
    public DbSet<VisitorCheckIn> VisitorCheckIns { get; set; }
    public DbSet<VisitorFormTemplate> VisitorFormTemplates { get; set; }
    public DbSet<VisitorFormAcceptance> VisitorFormAcceptances { get; set; }
    public DbSet<WatchlistEntry> WatchlistEntries { get; set; }
    public DbSet<WatchlistMatch> WatchlistMatches { get; set; }
    public DbSet<ParkingArea> ParkingAreas { get; set; }
    public DbSet<ParkingPermit> ParkingPermits { get; set; }
    public DbSet<SecurityBarrier> Barriers { get; set; }
    public DbSet<LaneEvent> LaneEvents { get; set; }
    public DbSet<BarrierCommandAudit> BarrierCommandAudits { get; set; }
    public DbSet<Contractor> Contractors { get; set; }

    private static void ConfigureVisitorVehicleOperations(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Visit>(entity =>
        {
            entity.HasKey(e => e.VisitId);
            entity.Property(e => e.VisitorName).IsRequired().HasMaxLength(180);
            entity.Property(e => e.VisitorType).IsRequired().HasMaxLength(40);
            entity.Property(e => e.Status).IsRequired().HasMaxLength(40);
            entity.HasIndex(e => new { e.SiteId, e.Status, e.ExpectedInUtc });
            entity.HasOne(e => e.Site).WithMany().HasForeignKey(e => e.SiteId).OnDelete(DeleteBehavior.SetNull);
            entity.HasOne(e => e.HostEmployee).WithMany().HasForeignKey(e => e.HostEmployeeId).OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<VisitorCredential>(entity =>
        {
            entity.HasKey(e => e.VisitorCredentialId);
            entity.Property(e => e.CredentialType).IsRequired().HasMaxLength(40);
            entity.Property(e => e.CredentialReference).IsRequired().HasMaxLength(200);
            entity.HasIndex(e => e.CredentialReference).IsUnique();
            entity.HasOne(e => e.Visit).WithMany().HasForeignKey(e => e.VisitId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<VisitorCheckIn>(entity =>
        {
            entity.HasKey(e => e.VisitorCheckInId);
            entity.Property(e => e.VerificationStatus).IsRequired().HasMaxLength(40);
            entity.HasIndex(e => e.VisitId);
            entity.HasOne(e => e.Visit).WithMany().HasForeignKey(e => e.VisitId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<VisitorFormTemplate>(entity =>
        {
            entity.HasKey(e => e.VisitorFormTemplateId);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(160);
            entity.Property(e => e.FormType).IsRequired().HasMaxLength(40);
            entity.Property(e => e.Body).IsRequired().HasMaxLength(4000);
        });

        modelBuilder.Entity<VisitorFormAcceptance>(entity =>
        {
            entity.HasKey(e => e.VisitorFormAcceptanceId);
            entity.Property(e => e.AcceptedByName).IsRequired().HasMaxLength(180);
            entity.HasOne(e => e.Visit).WithMany().HasForeignKey(e => e.VisitId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.Template).WithMany().HasForeignKey(e => e.VisitorFormTemplateId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<WatchlistEntry>(entity =>
        {
            entity.HasKey(e => e.WatchlistEntryId);
            entity.Property(e => e.EntityType).IsRequired().HasMaxLength(40);
            entity.Property(e => e.DisplayName).IsRequired().HasMaxLength(180);
            entity.Property(e => e.Severity).IsRequired().HasMaxLength(40);
            entity.Property(e => e.Reason).IsRequired().HasMaxLength(1000);
            entity.HasIndex(e => new { e.EntityType, e.Identifier, e.IsActive });
        });

        modelBuilder.Entity<WatchlistMatch>(entity =>
        {
            entity.HasKey(e => e.WatchlistMatchId);
            entity.Property(e => e.Status).IsRequired().HasMaxLength(40);
            entity.HasOne(e => e.WatchlistEntry).WithMany().HasForeignKey(e => e.WatchlistEntryId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.Visit).WithMany().HasForeignKey(e => e.VisitId).OnDelete(DeleteBehavior.SetNull);
            entity.HasOne(e => e.Vehicle).WithMany().HasForeignKey(e => e.VehicleId).OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<ParkingArea>(entity =>
        {
            entity.HasKey(e => e.ParkingAreaId);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(160);
            entity.HasOne(e => e.Site).WithMany().HasForeignKey(e => e.SiteId).OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<ParkingPermit>(entity =>
        {
            entity.HasKey(e => e.ParkingPermitId);
            entity.Property(e => e.PermitType).IsRequired().HasMaxLength(60);
            entity.HasIndex(e => new { e.ParkingAreaId, e.ValidToUtc });
            entity.HasOne(e => e.ParkingArea).WithMany().HasForeignKey(e => e.ParkingAreaId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.Vehicle).WithMany().HasForeignKey(e => e.VehicleId).OnDelete(DeleteBehavior.SetNull);
            entity.HasOne(e => e.Visit).WithMany().HasForeignKey(e => e.VisitId).OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<SecurityBarrier>(entity =>
        {
            entity.HasKey(e => e.BarrierId);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(160);
            entity.Property(e => e.State).IsRequired().HasMaxLength(40);
            entity.HasOne(e => e.Lane).WithMany().HasForeignKey(e => e.LaneId).OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<LaneEvent>(entity =>
        {
            entity.HasKey(e => e.LaneEventId);
            entity.Property(e => e.EventType).IsRequired().HasMaxLength(40);
            entity.Property(e => e.Direction).IsRequired().HasMaxLength(40);
            entity.HasIndex(e => new { e.LaneId, e.OccurredAtUtc });
            entity.HasOne(e => e.Lane).WithMany().HasForeignKey(e => e.LaneId).OnDelete(DeleteBehavior.SetNull);
            entity.HasOne(e => e.Vehicle).WithMany().HasForeignKey(e => e.VehicleId).OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<BarrierCommandAudit>(entity =>
        {
            entity.HasKey(e => e.BarrierCommandAuditId);
            entity.Property(e => e.Command).IsRequired().HasMaxLength(40);
            entity.Property(e => e.Reason).IsRequired().HasMaxLength(1000);
            entity.Property(e => e.Result).IsRequired().HasMaxLength(40);
            entity.HasOne(e => e.Barrier).WithMany().HasForeignKey(e => e.BarrierId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Contractor>(entity =>
        {
            entity.HasKey(e => e.ContractorId);
            entity.Property(e => e.FullName).IsRequired().HasMaxLength(180);
            entity.Property(e => e.Company).IsRequired().HasMaxLength(180);
            entity.Property(e => e.Status).IsRequired().HasMaxLength(40);
            entity.HasIndex(e => new { e.Status, e.ContractToUtc });
            entity.HasOne(e => e.Employee).WithMany().HasForeignKey(e => e.EmployeeId).OnDelete(DeleteBehavior.SetNull);
            entity.HasOne(e => e.Site).WithMany().HasForeignKey(e => e.SiteId).OnDelete(DeleteBehavior.SetNull);
        });
    }
}
