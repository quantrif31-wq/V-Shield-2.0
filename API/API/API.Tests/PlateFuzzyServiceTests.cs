using API.Data;
using API.Models;
using API.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace API.Tests;

public sealed class PlateFuzzyServiceTests
{
    private static int _databaseCounter;

    private static ApplicationDbContext CreateDatabase(out ServiceProvider provider)
    {
        var databaseName = $"plate-{System.Threading.Interlocked.Increment(ref _databaseCounter)}";
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
    public async Task FindSimilarPlatesAsync_ExactMatch_Found()
    {
        await using var db = CreateDatabase(out var provider);
        await using (provider)
        {
            db.Vehicles.Add(new Vehicle { VehicleId = 1, LicensePlate = "29A-123.45", ParkingStatus = "OUT" });
            await db.SaveChangesAsync();

            var service = new PlateFuzzyService(db);
            var results = await service.FindSimilarPlatesAsync("29A12345", 0.8, 5);

            Assert.Contains(results, r => r.LicensePlate == "29A-123.45" && r.IsExactMatch);
            Assert.Equal(1.0, results.First(r => r.IsExactMatch).Score, 3);
        }
    }

    [Fact]
    public async Task FindSimilarPlatesAsync_ConfusableVariant_Matched()
    {
        await using var db = CreateDatabase(out var provider);
        await using (provider)
        {
            db.Vehicles.Add(new Vehicle { VehicleId = 2, LicensePlate = "30B-888.88", ParkingStatus = "OUT" });
            await db.SaveChangesAsync();

            var service = new PlateFuzzyService(db);
            var results = await service.FindSimilarPlatesAsync("30B88833", 0.6, 5);

            Assert.NotEmpty(results);
        }
    }

    [Fact]
    public async Task FindSimilarPlatesAsync_ReturnsEmpty_WhenUnderMinScore()
    {
        await using var db = CreateDatabase(out var provider);
        await using (provider)
        {
            db.Vehicles.Add(new Vehicle { VehicleId = 3, LicensePlate = "29A-123.45", ParkingStatus = "OUT" });
            await db.SaveChangesAsync();

            var service = new PlateFuzzyService(db);
            var results = await service.FindSimilarPlatesAsync("ZZZ99999", 0.9, 5);

            Assert.Empty(results);
        }
    }

    [Fact]
    public async Task GetPlateTimelineAsync_ReturnsMatchedEntries()
    {
        await using var db = CreateDatabase(out var provider);
        await using (provider)
        {
            var gate = new Gate { GateId = 1, GateName = "Gate A" };
            db.Gates.Add(gate);
            db.AccessLogs.Add(new AccessLog
            {
                LogId = 1,
                Timestamp = DateTime.UtcNow.AddMinutes(-5),
                Direction = "IN",
                GateId = 1,
                ResultStatus = "APPROVED",
                CapturedLicensePlate = "29A-123.45"
            });
            await db.SaveChangesAsync();

            var service = new PlateFuzzyService(db);
            var entries = await service.GetPlateTimelineAsync("29A12345", 24);

            Assert.Single(entries);
            Assert.Equal("Gate A", entries[0].GateName);
        }
    }

    [Fact]
    public async Task CheckAnomaliesAsync_DetectsRapidReEntry()
    {
        await using var db = CreateDatabase(out var provider);
        await using (provider)
        {
            for (var i = 0; i < 2; i++)
            {
                db.AccessLogs.Add(new AccessLog
                {
                    LogId = 10 + i,
                    Timestamp = DateTime.UtcNow.AddMinutes(-i),
                    Direction = "IN",
                    ResultStatus = "APPROVED",
                    CapturedLicensePlate = "29A-123.45"
                });
            }
            await db.SaveChangesAsync();

            var service = new PlateFuzzyService(db);
            var anomalies = await service.CheckAnomaliesAsync("29A12345", 24);

            Assert.Contains(anomalies, a => a.Type == "RapidReEntry");
        }
    }

    [Fact]
    public async Task SuggestCorrectionAsync_RecognizesKnownPlate()
    {
        await using var db = CreateDatabase(out var provider);
        await using (provider)
        {
            db.Vehicles.Add(new Vehicle { VehicleId = 4, LicensePlate = "29A-123.45", ParkingStatus = "OUT" });
            await db.SaveChangesAsync();

            var service = new PlateFuzzyService(db);
            var result = await service.SuggestCorrectionAsync("29A12345");

            Assert.True(result.IsKnownPlate);
            Assert.Equal("29A-123.45", result.SuggestedPlate);
        }
    }

    [Fact]
    public async Task SuggestCorrectionAsync_Garbage_ReturnsUnrecognized()
    {
        await using var db = CreateDatabase(out var provider);
        await using (provider)
        {
            var service = new PlateFuzzyService(db);
            var result = await service.SuggestCorrectionAsync("###");

            Assert.False(result.IsKnownPlate);
            Assert.Equal("Unrecognized", result.Source);
        }
    }
}