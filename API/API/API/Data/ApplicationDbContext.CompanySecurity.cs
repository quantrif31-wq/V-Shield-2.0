using API.Models;
using Microsoft.EntityFrameworkCore;

namespace API.Data;

public partial class ApplicationDbContext
{
    public DbSet<Company> Companies { get; set; }
    public DbSet<Site> Sites { get; set; }
    public DbSet<Building> Buildings { get; set; }
    public DbSet<FacilityFloor> FacilityFloors { get; set; }
    public DbSet<SecurityZone> SecurityZones { get; set; }
    public DbSet<AccessPoint> AccessPoints { get; set; }
    public DbSet<Door> Doors { get; set; }
    public DbSet<Lane> Lanes { get; set; }
    public DbSet<MusterPoint> MusterPoints { get; set; }
    public DbSet<ExternalIdentityProvider> ExternalIdentityProviders { get; set; }
    public DbSet<ExternalIdentityMapping> ExternalIdentityMappings { get; set; }
    public DbSet<EmployeeLifecycleEvent> EmployeeLifecycleEvents { get; set; }
    public DbSet<AccessRecertificationCampaign> AccessRecertificationCampaigns { get; set; }
    public DbSet<AccessRecertificationDecision> AccessRecertificationDecisions { get; set; }

    private static void ConfigureCompanySecurityFoundation(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Company>(entity =>
        {
            entity.HasKey(e => e.CompanyId);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(160);
            entity.Property(e => e.Code).IsRequired().HasMaxLength(50);
            entity.HasIndex(e => e.Code).IsUnique();
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.CreatedAtUtc).HasDefaultValueSql("(getutcdate())");
        });

        modelBuilder.Entity<Site>(entity =>
        {
            entity.HasKey(e => e.SiteId);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(160);
            entity.Property(e => e.Code).IsRequired().HasMaxLength(50);
            entity.HasIndex(e => new { e.CompanyId, e.Code }).IsUnique();
            entity.Property(e => e.TimeZoneId).IsRequired().HasMaxLength(80);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.CreatedAtUtc).HasDefaultValueSql("(getutcdate())");
            entity.HasOne(e => e.Company)
                .WithMany(c => c.Sites)
                .HasForeignKey(e => e.CompanyId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Building>(entity =>
        {
            entity.HasKey(e => e.BuildingId);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(160);
            entity.Property(e => e.Code).IsRequired().HasMaxLength(50);
            entity.HasIndex(e => new { e.SiteId, e.Code }).IsUnique();
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.HasOne(e => e.Site)
                .WithMany(s => s.Buildings)
                .HasForeignKey(e => e.SiteId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<FacilityFloor>(entity =>
        {
            entity.HasKey(e => e.FacilityFloorId);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(120);
            entity.Property(e => e.Code).IsRequired().HasMaxLength(50);
            entity.HasIndex(e => new { e.BuildingId, e.Code }).IsUnique();
            entity.HasOne(e => e.Building)
                .WithMany(b => b.Floors)
                .HasForeignKey(e => e.BuildingId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<SecurityZone>(entity =>
        {
            entity.HasKey(e => e.SecurityZoneId);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(160);
            entity.Property(e => e.Code).IsRequired().HasMaxLength(50);
            entity.Property(e => e.SecurityLevel).IsRequired().HasMaxLength(40);
            entity.HasIndex(e => new { e.SiteId, e.Code }).IsUnique();
            entity.HasOne(e => e.Site)
                .WithMany(s => s.Zones)
                .HasForeignKey(e => e.SiteId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.FacilityFloor)
                .WithMany(f => f.Zones)
                .HasForeignKey(e => e.FacilityFloorId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<AccessPoint>(entity =>
        {
            entity.HasKey(e => e.AccessPointId);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(160);
            entity.Property(e => e.Type).IsRequired().HasMaxLength(60);
            entity.Property(e => e.DirectionMode).IsRequired().HasMaxLength(80);
            entity.HasIndex(e => new { e.SiteId, e.Name });
            entity.HasOne(e => e.Site)
                .WithMany()
                .HasForeignKey(e => e.SiteId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.SecurityZone)
                .WithMany(z => z.AccessPoints)
                .HasForeignKey(e => e.SecurityZoneId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<Door>(entity =>
        {
            entity.HasKey(e => e.DoorId);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(160);
            entity.Property(e => e.DoorMode).IsRequired().HasMaxLength(60);
            entity.HasIndex(e => e.AccessPointId).IsUnique();
            entity.HasOne(e => e.AccessPoint)
                .WithOne(a => a.Door)
                .HasForeignKey<Door>(e => e.AccessPointId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Lane>(entity =>
        {
            entity.HasKey(e => e.LaneId);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(160);
            entity.Property(e => e.Direction).IsRequired().HasMaxLength(40);
            entity.HasOne(e => e.Site)
                .WithMany()
                .HasForeignKey(e => e.SiteId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.Gate)
                .WithMany()
                .HasForeignKey(e => e.GateId)
                .OnDelete(DeleteBehavior.SetNull);
            entity.HasOne(e => e.AccessPoint)
                .WithMany()
                .HasForeignKey(e => e.AccessPointId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<MusterPoint>(entity =>
        {
            entity.HasKey(e => e.MusterPointId);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(160);
            entity.HasOne(e => e.Site)
                .WithMany()
                .HasForeignKey(e => e.SiteId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<ExternalIdentityProvider>(entity =>
        {
            entity.HasKey(e => e.ExternalIdentityProviderId);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(160);
            entity.Property(e => e.Protocol).IsRequired().HasMaxLength(40);
            entity.HasIndex(e => e.Name).IsUnique();
        });

        modelBuilder.Entity<ExternalIdentityMapping>(entity =>
        {
            entity.HasKey(e => e.ExternalIdentityMappingId);
            entity.Property(e => e.ExternalSubject).IsRequired().HasMaxLength(240);
            entity.HasIndex(e => new { e.ExternalIdentityProviderId, e.ExternalSubject }).IsUnique();
            entity.HasOne(e => e.Provider)
                .WithMany()
                .HasForeignKey(e => e.ExternalIdentityProviderId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.SetNull);
            entity.HasOne(e => e.Employee)
                .WithMany()
                .HasForeignKey(e => e.EmployeeId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<Employee>(entity =>
        {
            entity.Property(e => e.LifecycleStatus).IsRequired().HasMaxLength(40).HasDefaultValue(EmployeeLifecycleStates.Active);
            entity.HasOne(e => e.PrimarySite)
                .WithMany()
                .HasForeignKey(e => e.PrimarySiteId)
                .OnDelete(DeleteBehavior.SetNull);
            entity.HasOne(e => e.ManagerEmployee)
                .WithMany(e => e.DirectReports)
                .HasForeignKey(e => e.ManagerEmployeeId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<EmployeeLifecycleEvent>(entity =>
        {
            entity.HasKey(e => e.EmployeeLifecycleEventId);
            entity.Property(e => e.PreviousState).IsRequired().HasMaxLength(40);
            entity.Property(e => e.NewState).IsRequired().HasMaxLength(40);
            entity.HasIndex(e => new { e.EmployeeId, e.EffectiveAtUtc });
            entity.HasOne(e => e.Employee)
                .WithMany()
                .HasForeignKey(e => e.EmployeeId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.ChangedByUser)
                .WithMany()
                .HasForeignKey(e => e.ChangedByUserId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<AccessRecertificationCampaign>(entity =>
        {
            entity.HasKey(e => e.AccessRecertificationCampaignId);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(160);
            entity.Property(e => e.Status).IsRequired().HasMaxLength(40);
            entity.HasOne(e => e.Site)
                .WithMany()
                .HasForeignKey(e => e.SiteId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<AccessRecertificationDecision>(entity =>
        {
            entity.HasKey(e => e.AccessRecertificationDecisionId);
            entity.Property(e => e.Decision).IsRequired().HasMaxLength(40);
            entity.HasIndex(e => new { e.AccessRecertificationCampaignId, e.EmployeeId }).IsUnique();
            entity.HasOne(e => e.Campaign)
                .WithMany(c => c.Decisions)
                .HasForeignKey(e => e.AccessRecertificationCampaignId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.Employee)
                .WithMany()
                .HasForeignKey(e => e.EmployeeId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.ReviewerUser)
                .WithMany()
                .HasForeignKey(e => e.ReviewerUserId)
                .OnDelete(DeleteBehavior.SetNull);
        });
    }
}

