using API.Models;
using Microsoft.EntityFrameworkCore;

namespace API.Data;

public partial class ApplicationDbContext
{
    public DbSet<SecurityDevice> SecurityDevices { get; set; }
    public DbSet<AccessControllerDevice> AccessControllerDevices { get; set; }
    public DbSet<ReaderDevice> ReaderDevices { get; set; }
    public DbSet<DeviceRelay> DeviceRelays { get; set; }
    public DbSet<DeviceSensor> DeviceSensors { get; set; }
    public DbSet<DeviceCredential> DeviceCredentials { get; set; }
    public DbSet<DeviceHealthSnapshot> DeviceHealthSnapshots { get; set; }
    public DbSet<DeviceConfigurationVersion> DeviceConfigurationVersions { get; set; }
    public DbSet<DeviceProvisioningRequest> DeviceProvisioningRequests { get; set; }
    public DbSet<OfflinePolicyPackage> OfflinePolicyPackages { get; set; }

    private static void ConfigureDeviceProtocolOperations(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<SecurityDevice>(entity =>
        {
            entity.HasKey(e => e.SecurityDeviceId);
            entity.Property(e => e.DeviceType).IsRequired().HasMaxLength(80);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(160);
            entity.Property(e => e.Status).IsRequired().HasMaxLength(60);
            entity.HasIndex(e => new { e.SiteId, e.DeviceType, e.Name });
            entity.HasOne(e => e.Site).WithMany().HasForeignKey(e => e.SiteId).OnDelete(DeleteBehavior.SetNull);
            entity.HasOne(e => e.AccessPoint).WithMany().HasForeignKey(e => e.AccessPointId).OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<AccessControllerDevice>(entity =>
        {
            entity.HasKey(e => e.AccessControllerDeviceId);
            entity.Property(e => e.Protocol).IsRequired().HasMaxLength(60);
            entity.HasIndex(e => e.SecurityDeviceId).IsUnique();
            entity.HasOne(e => e.SecurityDevice).WithMany().HasForeignKey(e => e.SecurityDeviceId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ReaderDevice>(entity =>
        {
            entity.HasKey(e => e.ReaderDeviceId);
            entity.Property(e => e.ReaderProtocol).IsRequired().HasMaxLength(60);
            entity.Property(e => e.CredentialFormats).IsRequired().HasMaxLength(80);
            entity.HasOne(e => e.SecurityDevice).WithMany().HasForeignKey(e => e.SecurityDeviceId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.Controller).WithMany().HasForeignKey(e => e.AccessControllerDeviceId).OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<DeviceRelay>(entity =>
        {
            entity.HasKey(e => e.DeviceRelayId);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(80);
            entity.Property(e => e.State).IsRequired().HasMaxLength(40);
            entity.HasOne(e => e.SecurityDevice).WithMany().HasForeignKey(e => e.SecurityDeviceId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<DeviceSensor>(entity =>
        {
            entity.HasKey(e => e.DeviceSensorId);
            entity.Property(e => e.SensorType).IsRequired().HasMaxLength(80);
            entity.Property(e => e.State).IsRequired().HasMaxLength(40);
            entity.HasOne(e => e.SecurityDevice).WithMany().HasForeignKey(e => e.SecurityDeviceId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<DeviceCredential>(entity =>
        {
            entity.HasKey(e => e.DeviceCredentialId);
            entity.Property(e => e.CredentialType).IsRequired().HasMaxLength(80);
            entity.Property(e => e.CredentialReference).IsRequired().HasMaxLength(120);
            entity.HasIndex(e => new { e.SecurityDeviceId, e.CredentialReference }).IsUnique();
            entity.HasOne(e => e.SecurityDevice).WithMany().HasForeignKey(e => e.SecurityDeviceId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<DeviceHealthSnapshot>(entity =>
        {
            entity.HasKey(e => e.DeviceHealthSnapshotId);
            entity.Property(e => e.Status).IsRequired().HasMaxLength(40);
            entity.HasIndex(e => new { e.SecurityDeviceId, e.CapturedAtUtc });
            entity.HasOne(e => e.SecurityDevice).WithMany().HasForeignKey(e => e.SecurityDeviceId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<DeviceConfigurationVersion>(entity =>
        {
            entity.HasKey(e => e.DeviceConfigurationVersionId);
            entity.Property(e => e.Version).IsRequired().HasMaxLength(80);
            entity.Property(e => e.ConfigurationJson).IsRequired().HasMaxLength(4000);
            entity.HasOne(e => e.SecurityDevice).WithMany().HasForeignKey(e => e.SecurityDeviceId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<DeviceProvisioningRequest>(entity =>
        {
            entity.HasKey(e => e.DeviceProvisioningRequestId);
            entity.Property(e => e.DeviceType).IsRequired().HasMaxLength(80);
            entity.Property(e => e.RequestedName).IsRequired().HasMaxLength(160);
            entity.Property(e => e.Status).IsRequired().HasMaxLength(40);
            entity.HasOne(e => e.SecurityDevice).WithMany().HasForeignKey(e => e.SecurityDeviceId).OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<OfflinePolicyPackage>(entity =>
        {
            entity.HasKey(e => e.OfflinePolicyPackageId);
            entity.Property(e => e.PackageVersion).IsRequired().HasMaxLength(80);
            entity.Property(e => e.PayloadJson).IsRequired().HasMaxLength(4000);
            entity.Property(e => e.Status).IsRequired().HasMaxLength(40);
            entity.HasOne(e => e.SecurityDevice).WithMany().HasForeignKey(e => e.SecurityDeviceId).OnDelete(DeleteBehavior.SetNull);
        });
    }
}

