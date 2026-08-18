using API.Data;
using API.Models;
using API.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace API.Tests;

public sealed class DeviceSimulatorServiceTests
{
    private static ApplicationDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"sim_{Guid.NewGuid():N}")
            .Options;
        return new ApplicationDbContext(options);
    }

    private static async Task<SecurityDevice> SeedDeviceAsync(ApplicationDbContext db, int id = 1)
    {
        var device = new SecurityDevice
        {
            SecurityDeviceId = id,
            DeviceType = "VirtualController",
            Name = "Cam 1",
            Status = "Ok"
        };
        db.SecurityDevices.Add(device);
        await db.SaveChangesAsync();
        return device;
    }

    [Fact]
    public void SimulatorType_ReturnsVendorString()
    {
        var db = CreateDb();
        Assert.Equal("V-Shield EnterpriseEdge-Sim", new DeviceSimulatorService(db).SimulatorType);
    }

    [Fact]
    public async Task CreateVirtualDeviceAsync_CreatesDeviceAndPeripherals()
    {
        var db = CreateDb();
        var svc = new DeviceSimulatorService(db);

        var device = await svc.CreateVirtualDeviceAsync("  Cam A  ", 7, 9);

        Assert.Equal("Cam A", device.Name);
        Assert.Equal("VirtualController", device.DeviceType);
        Assert.Equal("Ok", device.Status);
        Assert.StartsWith("SIM-", device.SerialNumber);
        Assert.Equal(7, device.SiteId);
        Assert.Equal(9, device.AccessPointId);
        var saved = Assert.Single(db.SecurityDevices);
        Assert.Equal(device.SecurityDeviceId, saved.SecurityDeviceId);
        Assert.Single(db.AccessControllerDevices);
        Assert.Single(db.ReaderDevices);
        Assert.Single(db.DeviceRelays);
        Assert.Single(db.DeviceSensors);
        Assert.Single(db.DeviceHealthSnapshots);
        var acd = db.AccessControllerDevices.Single();
        Assert.Equal(device.SecurityDeviceId, acd.SecurityDeviceId);
        Assert.True(acd.SupportsOfflineDecision);
    }

    [Fact]
    public async Task SimulateOfflineDecisionAsync_DeviceMissing_Throws()
    {
        var db = CreateDb();
        var svc = new DeviceSimulatorService(db);
        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => svc.SimulateOfflineDecisionAsync(99, "Employee", 1, "QR"));
    }

    [Fact]
    public async Task SimulateOfflineDecisionAsync_AllowAll_Allows()
    {
        var db = CreateDb();
        var device = await SeedDeviceAsync(db, 1);
        db.OfflinePolicyPackages.Add(new OfflinePolicyPackage
        {
            SecurityDeviceId = 1,
            PackageVersion = "v1",
            Status = "Published",
            PublishedAtUtc = DateTime.UtcNow,
            PayloadJson = "{\"allowAll\":true}"
        });
        await db.SaveChangesAsync();

        var svc = new DeviceSimulatorService(db);
        var decision = await svc.SimulateOfflineDecisionAsync(1, "Employee", 3, "QR");

        Assert.Equal(AccessDecisionResults.Allow, decision.Result);
        Assert.Equal("Employee", decision.SubjectType);
        Assert.Equal(3, decision.SubjectId);
        Assert.Single(db.AccessDecisions);
        Assert.Single(db.SecurityEvents);
        Assert.Equal("OfflineAccessGranted", db.SecurityEvents.Single().EventType);
    }

    [Fact]
    public async Task SimulateOfflineDecisionAsync_SubjectMatch_Allows()
    {
        var db = CreateDb();
        await SeedDeviceAsync(db, 1);
        db.OfflinePolicyPackages.Add(new OfflinePolicyPackage
        {
            SecurityDeviceId = 1,
            PackageVersion = "v2",
            Status = "Published",
            PublishedAtUtc = DateTime.UtcNow,
            PayloadJson = """
                {"allowedSubjects":[
                  {"subjectType":"Vehicle","subjectId":5,"credentialType":"Plate"},
                  {"subjectType":"Employee","subjectId":12}
                ]}
                """
        });
        await db.SaveChangesAsync();

        var svc = new DeviceSimulatorService(db);
        var decision = await svc.SimulateOfflineDecisionAsync(1, "Employee", 12, "QR");

        Assert.Equal(AccessDecisionResults.Allow, decision.Result);
    }

    [Fact]
    public async Task SimulateOfflineDecisionAsync_NoMatch_Denies()
    {
        var db = CreateDb();
        await SeedDeviceAsync(db, 1);
        db.OfflinePolicyPackages.Add(new OfflinePolicyPackage
        {
            SecurityDeviceId = 1,
            Status = "Published",
            PublishedAtUtc = DateTime.UtcNow,
            PayloadJson = """{"allowedSubjects":[{"subjectType":"Employee","subjectId":12}]}"""
        });
        await db.SaveChangesAsync();

        var svc = new DeviceSimulatorService(db);
        var decision = await svc.SimulateOfflineDecisionAsync(1, "Employee", 99, "QR");

        Assert.Equal(AccessDecisionResults.Deny, decision.Result);
        Assert.Equal("OfflineAccessDenied", db.SecurityEvents.Single().EventType);
        Assert.Equal("Medium", db.SecurityEvents.Single().Severity);
        Assert.Equal("Ok", db.SecurityDevices.Single().Status);
    }

    [Fact]
    public async Task SimulateOfflineDecisionAsync_MalformedJsonAllowAll_FallsBack()
    {
        var db = CreateDb();
        await SeedDeviceAsync(db, 1);
        db.OfflinePolicyPackages.Add(new OfflinePolicyPackage
        {
            SecurityDeviceId = 1,
            Status = "Published",
            PublishedAtUtc = DateTime.UtcNow,
            PayloadJson = "{\"allowAll\":\"not-a-number!!!\""
        });
        await db.SaveChangesAsync();

        var svc = new DeviceSimulatorService(db);
        var decision = await svc.SimulateOfflineDecisionAsync(1, "Employee", 1, "QR");

        Assert.Equal(AccessDecisionResults.Deny, decision.Result);
    }

    [Fact]
    public async Task InjectFaultAsync_MissingDevice_ReturnsFalse()
    {
        var db = CreateDb();
        var svc = new DeviceSimulatorService(db);
        Assert.False(await svc.InjectFaultAsync(9, "Overheat", "High", "Quá nhiệt"));
    }

    [Fact]
    public async Task InjectFaultAsync_SetsStatusAndCreatesSnapshotAndAlarm()
    {
        var db = CreateDb();
        await SeedDeviceAsync(db, 1);
        var svc = new DeviceSimulatorService(db);

        var ok = await svc.InjectFaultAsync(1, "Overheat", "High", "  Quá nhiệt  ");

        Assert.True(ok);
        Assert.Equal("Overheat", db.SecurityDevices.Single().Status);
        var snapshot = db.DeviceHealthSnapshots.Single();
        Assert.Equal("Overheat", snapshot.Status);
        Assert.Equal("Quá nhiệt", snapshot.Message);
        var alarm = db.Alarms.Single();
        Assert.Equal("Overheat", alarm.AlarmType);
        Assert.Equal("High", alarm.Severity);
        Assert.Equal("New", alarm.State);
        Assert.Contains("Cam 1", alarm.Summary);
    }

    [Fact]
    public async Task RestoreNormalAsync_MissingDevice_ReturnsFalse()
    {
        var db = CreateDb();
        var svc = new DeviceSimulatorService(db);
        Assert.False(await svc.RestoreNormalAsync(9));
    }

    [Fact]
    public async Task RestoreNormalAsync_SetsOkAndAddsSnapshot()
    {
        var db = CreateDb();
        await SeedDeviceAsync(db, 1);
        db.SecurityDevices.Single().Status = "Faulted";
        await db.SaveChangesAsync();
        var svc = new DeviceSimulatorService(db);

        var ok = await svc.RestoreNormalAsync(1);

        Assert.True(ok);
        Assert.Equal("Ok", db.SecurityDevices.Single().Status);
        Assert.Equal("Ok", db.DeviceHealthSnapshots.Single().Status);
    }
}