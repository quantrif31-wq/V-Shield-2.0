using API.Data;
using API.Models;
using API.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace API.Tests;

public sealed class LockerServiceTests
{
    private static ApplicationDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"locker_{Guid.NewGuid():N}")
            .Options;
        return new ApplicationDbContext(options);
    }

    [Fact]
    public async Task GetAvailableCompartments_ReturnsOnlyEmpty()
    {
        var db = CreateDb();
        db.LockerCompartments.AddRange(
            new LockerCompartment { LockerCabinetId = 1, Code = "A1", Status = "Empty" },
            new LockerCompartment { LockerCabinetId = 1, Code = "B1", Status = "Occupied" },
            new LockerCompartment { LockerCabinetId = 2, Code = "C1", Status = "Empty" });
        await db.SaveChangesAsync();
        var svc = new LockerService(db);

        var result = await svc.GetAvailableCompartmentsAsync(1);

        Assert.Single(result);
        Assert.Equal("A1", result[0].Code);
    }

    [Fact]
    public async Task GetOccupiedCompartments_IncludesEvidence()
    {
        var db = CreateDb();
        db.LockerCompartments.Add(new LockerCompartment
        {
            LockerCabinetId = 1,
            Code = "A1",
            Status = "Occupied",
            EvidenceItemId = 5
        });
        await db.SaveChangesAsync();
        var svc = new LockerService(db);

        var result = await svc.GetOccupiedCompartmentsAsync(1);

        Assert.Single(result);
        Assert.Equal(5, result[0].EvidenceItemId);
    }

    [Fact]
    public async Task AssignCompartment_CompartmentMissing_Fails()
    {
        var db = CreateDb();
        var svc = new LockerService(db);
        var (success, message) = await svc.AssignCompartmentAsync(99, 1, 1);
        Assert.False(success);
        Assert.Contains("not found", message);
    }

    [Fact]
    public async Task AssignCompartment_NotEmpty_Fails()
    {
        var db = CreateDb();
        db.LockerCompartments.Add(new LockerCompartment { LockerCabinetId = 1, Code = "A1", Status = "Occupied" });
        await db.SaveChangesAsync();
        var svc = new LockerService(db);

        var (success, message) = await svc.AssignCompartmentAsync(1, 1, 1);

        Assert.False(success);
        Assert.Contains("Occupied", message);
    }

    [Fact]
    public async Task AssignCompartment_EvidenceMissing_Fails()
    {
        var db = CreateDb();
        db.LockerCompartments.Add(new LockerCompartment { LockerCabinetId = 1, Code = "A1", Status = "Empty" });
        await db.SaveChangesAsync();
        var svc = new LockerService(db);

        var (success, _) = await svc.AssignCompartmentAsync(1, 99, 1);

        Assert.False(success);
        Assert.Equal("Empty", db.LockerCompartments.Single().Status);
    }

    [Fact]
    public async Task AssignCompartment_Success()
    {
        var db = CreateDb();
        db.LockerCompartments.Add(new LockerCompartment { LockerCompartmentId = 1, LockerCabinetId = 1, Code = "A1", Status = "Empty" });
        db.EvidenceItems.Add(new EvidenceItem { EvidenceItemId = 5 });
        await db.SaveChangesAsync();
        var svc = new LockerService(db);

        var (success, message) = await svc.AssignCompartmentAsync(1, 5, 7);

        Assert.True(success);
        Assert.Contains("A1", message);
        var compartment = db.LockerCompartments.Single();
        Assert.Equal("Occupied", compartment.Status);
        Assert.Equal(5, compartment.EvidenceItemId);
        Assert.Equal(7, compartment.OccupiedByUserId);
        Assert.Single(db.ChainOfCustodyEntries);
        Assert.Equal("StoredInLocker", db.ChainOfCustodyEntries.Single().Action);
        Assert.Single(db.LockerAccessLogs);
        Assert.Equal("Assigned", db.LockerAccessLogs.Single().Action);
    }

    [Fact]
    public async Task AssignCompartmentToFoundItem_Success()
    {
        var db = CreateDb();
        db.LockerCompartments.Add(new LockerCompartment { LockerCompartmentId = 1, LockerCabinetId = 1, Code = "B2", Status = "Empty" });
        await db.SaveChangesAsync();
        var svc = new LockerService(db);

        var (success, _) = await svc.AssignCompartmentToFoundItemAsync(1, 42, 9, 3);

        Assert.True(success);
        Assert.Equal("Occupied", db.LockerCompartments.Single().Status);
        Assert.Equal(9, db.LockerCompartments.Single().EvidenceItemId);
        Assert.Single(db.ChainOfCustodyEntries);
        Assert.Single(db.LockerAccessLogs);
        Assert.Equal("AssignedFoundItem", db.LockerAccessLogs.Single().Action);
    }

    [Fact]
    public async Task AssignCompartmentToFoundItem_WithoutEvidence_NoCustodyEntry()
    {
        var db = CreateDb();
        db.LockerCompartments.Add(new LockerCompartment { LockerCompartmentId = 1, LockerCabinetId = 1, Code = "B3", Status = "Empty" });
        db.LockerCompartments.Add(new LockerCompartment { LockerCompartmentId = 2, LockerCabinetId = 1, Code = "B4", Status = "Occupied" });
        await db.SaveChangesAsync();
        var svc = new LockerService(db);

        var (success, _) = await svc.AssignCompartmentToFoundItemAsync(1, 42, null, 3);

        Assert.True(success);
        Assert.Empty(db.ChainOfCustodyEntries);
        Assert.Single(db.LockerAccessLogs);
    }

    [Fact]
    public async Task ReleaseCompartment_NotOccupied_Fails()
    {
        var db = CreateDb();
        db.LockerCompartments.Add(new LockerCompartment { LockerCabinetId = 1, Code = "A1", Status = "Empty" });
        await db.SaveChangesAsync();
        var svc = new LockerService(db);

        var (success, message) = await svc.ReleaseCompartmentAsync(1, 1);

        Assert.False(success);
        Assert.Contains("Empty", message);
    }

    [Fact]
    public async Task ReleaseCompartment_Success()
    {
        var db = CreateDb();
        db.LockerCompartments.Add(new LockerCompartment
        {
            LockerCompartmentId = 1,
            LockerCabinetId = 1,
            Code = "A1",
            Status = "Occupied",
            EvidenceItemId = 5
        });
        await db.SaveChangesAsync();
        var svc = new LockerService(db);

        var (success, _) = await svc.ReleaseCompartmentAsync(1, 7);

        Assert.True(success);
        var compartment = db.LockerCompartments.Single();
        Assert.Equal("Empty", compartment.Status);
        Assert.Null(compartment.EvidenceItemId);
        Assert.NotNull(compartment.ReleasedAtUtc);
        Assert.Single(db.ChainOfCustodyEntries);
        Assert.Equal("ReleasedFromLocker", db.ChainOfCustodyEntries.Single().Action);
        Assert.Single(db.LockerAccessLogs);
        Assert.Equal("Released", db.LockerAccessLogs.Single().Action);
    }

    [Fact]
    public async Task GetAccessLogs_FiltersByCompartmentAndLimits()
    {
        var db = CreateDb();
        db.LockerCompartments.AddRange(
            new LockerCompartment { LockerCompartmentId = 1, LockerCabinetId = 1, Code = "A1", Status = "Empty" },
            new LockerCompartment { LockerCompartmentId = 2, LockerCabinetId = 1, Code = "A2", Status = "Empty" });
        db.LockerAccessLogs.AddRange(
            new LockerAccessLog { LockerCompartmentId = 1, Action = "Assigned", Timestamp = DateTime.UtcNow.AddMinutes(1) },
            new LockerAccessLog { LockerCompartmentId = 1, Action = "Released", Timestamp = DateTime.UtcNow },
            new LockerAccessLog { LockerCompartmentId = 2, Action = "Assigned", Timestamp = DateTime.UtcNow });
        await db.SaveChangesAsync();
        var svc = new LockerService(db);

        var result = await svc.GetAccessLogsAsync(compartmentId: 1, limit: 10);

        Assert.Equal(2, result.Count);
        Assert.Equal("Assigned", result[0].Action);

        var limited = await svc.GetAccessLogsAsync(compartmentId: 1, limit: 1);
        Assert.Single(limited);
    }
}