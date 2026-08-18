using API.Data;
using API.Models;
using API.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace API.Tests;

public sealed class CompanyHierarchyBackfillServiceTests
{
    private static int _databaseCounter;

    private static ApplicationDbContext CreateDatabase(out ServiceProvider provider)
    {
        var databaseName = $"backfill-{System.Threading.Interlocked.Increment(ref _databaseCounter)}";
        var services = new ServiceCollection();
        services.AddDbContext<ApplicationDbContext>(options =>
            options
                .UseInMemoryDatabase(databaseName, new InMemoryDatabaseRoot())
                .ConfigureWarnings(w => w.Ignore(
                    Microsoft.EntityFrameworkCore.Diagnostics.CoreEventId.ManyServiceProvidersCreatedWarning)));
        provider = services.BuildServiceProvider();
        return provider.GetRequiredService<ApplicationDbContext>();
    }

    [Fact]
    public async Task BackfillDefaultSiteAsync_CreatesCompanyAndSite()
    {
        await using var db = CreateDatabase(out var provider);
        await using (provider)
        {
            var service = new CompanyHierarchyBackfillService(db);
            var report = await service.BackfillDefaultSiteAsync(
                new CompanyHierarchyBackfillRequest(
                    "ACME", "ACM", "HQ", "MAIN", "Asia/Ho_Chi_Minh"),
                1);

            Assert.True(report.CompanyId > 0);
            Assert.True(report.SiteId > 0);
            Assert.True(report.SecurityZoneId > 0);

            var company = await db.Companies.SingleAsync(c => c.Code == "ACM");
            var site = await db.Sites.SingleAsync(s => s.Code == "MAIN");
            Assert.Equal("ACME", company.Name);
            Assert.Equal("Asia/Ho_Chi_Minh", site.TimeZoneId);
        }
    }

    [Fact]
    public async Task BackfillDefaultSiteAsync_ReusesExistingCompany()
    {
        await using var db = CreateDatabase(out var provider);
        await using (provider)
        {
            db.Companies.Add(new Company { Name = "Existing", Code = "EXIST" });
            await db.SaveChangesAsync();

            var service = new CompanyHierarchyBackfillService(db);
            var report = await service.BackfillDefaultSiteAsync(
                new CompanyHierarchyBackfillRequest("X", "EXIST", "HQ", "SITE1", null),
                null);

            Assert.Equal(1, report.CompanyId);
            Assert.Equal(1, await db.Companies.CountAsync());
        }
    }

    [Fact]
    public async Task BackfillDefaultSiteAsync_MapsGatesAndCreatesLanes()
    {
        await using var db = CreateDatabase(out var provider);
        await using (provider)
        {
            db.Gates.AddRange(
                new Gate { GateId = 1, GateName = "Gate A" },
                new Gate { GateId = 2, GateName = "Gate B" });
            db.Cameras.AddRange(
                new Camera { CameraId = 1, CameraName = "Cam 1", CameraType = "IP" },
                new Camera { CameraId = 2, CameraName = "Cam 2", CameraType = "IP", GateId = 2 });
            db.Employees.Add(new Employee { EmployeeId = 1, FullName = "Emp", Status = true, LifecycleStatus = EmployeeLifecycleStates.Active });
            db.Vehicles.Add(new Vehicle { VehicleId = 1, LicensePlate = "29A-12345" });
            await db.SaveChangesAsync();

            var service = new CompanyHierarchyBackfillService(db);
            var report = await service.BackfillDefaultSiteAsync(
                new CompanyHierarchyBackfillRequest("ACME", "ACM", "HQ", "MAIN", null), 1);

            Assert.Equal(2, report.GatesMapped);
            Assert.Equal(2, report.CameraDevicesCreated);
            Assert.Equal(1, report.EmployeesMapped);
            Assert.Equal(1, report.VehiclesMapped);
            Assert.Equal(2, await db.Lanes.CountAsync());
            Assert.Equal(2, await db.SecurityDevices.CountAsync());

            var emp = await db.Employees.SingleAsync(e => e.EmployeeId == 1);
            Assert.NotNull(emp.PrimarySiteId);
        }
    }

    [Fact]
    public async Task BackfillDefaultSiteAsync_DoesNotDuplicateOnSecondRun()
    {
        await using var db = CreateDatabase(out var provider);
        await using (provider)
        {
            db.Gates.Add(new Gate { GateId = 1, GateName = "Gate A" });
            db.Cameras.Add(new Camera { CameraId = 1, CameraName = "Cam 1", CameraType = "IP" });
            db.Employees.Add(new Employee { EmployeeId = 1, FullName = "Emp", Status = true, LifecycleStatus = EmployeeLifecycleStates.Active });
            await db.SaveChangesAsync();

            var service = new CompanyHierarchyBackfillService(db);
            await service.BackfillDefaultSiteAsync(
                new CompanyHierarchyBackfillRequest("ACME", "ACM", "HQ", "MAIN", null), 1);
            var second = await service.BackfillDefaultSiteAsync(
                new CompanyHierarchyBackfillRequest("ACME", "ACM", "HQ", "MAIN", null), 1);

            Assert.Equal(0, second.GatesMapped);
            Assert.Equal(0, second.CameraDevicesCreated);
            Assert.Equal(1, await db.Lanes.CountAsync());
            Assert.Equal(1, await db.SecurityDevices.CountAsync());
            Assert.Single(await db.Companies.ToListAsync());
        }
    }

    [Fact]
    public async Task GetAssetMapAsync_ReturnsGateCameraVehicleItems()
    {
        await using var db = CreateDatabase(out var provider);
        await using (provider)
        {
            var company = new Company { Name = "ACME", Code = "ACM" };
            var site = new Site { CompanyId = 1, Name = "HQ", Code = "MAIN" };
            db.Companies.Add(company);
            db.Sites.Add(site);
            await db.SaveChangesAsync();

            db.Gates.Add(new Gate { GateId = 1, GateName = "Gate A", Location = "Lobby" });
            db.Cameras.Add(new Camera { CameraId = 1, CameraName = "Cam 1", CameraType = "IP", GateId = 1 });
            db.Vehicles.Add(new Vehicle { VehicleId = 1, LicensePlate = "29A-99999", SiteId = site.SiteId, ParkingStatus = "IN" });
            db.Lanes.Add(new Lane { SiteId = site.SiteId, GateId = 1, Name = "Lane A", Direction = "Bidirectional" });
            db.SecurityDevices.Add(new SecurityDevice
            {
                SiteId = site.SiteId,
                DeviceType = "Camera",
                Name = "Cam 1",
                SerialNumber = "legacy-camera-1",
                Status = "Active"
            });
            await db.SaveChangesAsync();

            var service = new CompanyHierarchyBackfillService(db);
            var map = await service.GetAssetMapAsync();

            var gate = Assert.Single(map.Gates);
            Assert.Equal("Gate A", gate.GateName);
            var camera = Assert.Single(map.Cameras);
            Assert.Equal("Cam 1", camera.CameraName);
            Assert.NotNull(camera.SecurityDeviceId);
            var vehicle = Assert.Single(map.Vehicles);
            Assert.Equal("29A-99999", vehicle.LicensePlate);
            Assert.Equal(site.SiteId, vehicle.SiteId);
        }
    }
}
