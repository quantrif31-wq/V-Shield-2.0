using API.Data;
using API.Models;
using API.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace API.Tests;

public sealed class UebaServiceTests
{
    private static int _databaseCounter;

    private static ApplicationDbContext CreateDatabase(out ServiceProvider provider)
    {
        var databaseName = $"ueba-{System.Threading.Interlocked.Increment(ref _databaseCounter)}";
        var services = new ServiceCollection();
        services.AddDbContext<ApplicationDbContext>(options =>
            options
                .UseInMemoryDatabase(databaseName, new InMemoryDatabaseRoot())
                .ConfigureWarnings(w => w.Ignore(
                    Microsoft.EntityFrameworkCore.Diagnostics.CoreEventId.ManyServiceProvidersCreatedWarning)));
        provider = services.BuildServiceProvider();
        return provider.GetRequiredService<ApplicationDbContext>();
    }

    private static Employee Employee(int id) => new()
    {
        EmployeeId = id,
        FullName = $"Employee {id}",
        Status = true,
        LifecycleStatus = EmployeeLifecycleStates.Active
    };

    private static AccessLog AccessLog(int id, int? employeeId, DateTime timestamp,
        string direction = "IN", int? gateId = null, bool? isBypass = null)
    {
        return new AccessLog
        {
            LogId = id,
            EmployeeId = employeeId,
            Timestamp = timestamp,
            Direction = direction,
            GateId = gateId,
            IsBypass = isBypass,
            ResultStatus = "Approved"
        };
    }

    [Fact]
    public async Task GetProfileAsync_ReturnsNull_WhenNoProfileExists()
    {
        await using var db = CreateDatabase(out var provider);
        await using (provider)
        {
            var service = new UebaService(db);
            Assert.Null(await service.GetProfileAsync(1));
        }
    }

    [Fact]
    public async Task BuildProfileAsync_ComputesStats_FromAccessLogs()
    {
        await using var db = CreateDatabase(out var provider);
        await using (provider)
        {
            db.Employees.Add(Employee(1));
            db.AccessLogs.AddRange(
                AccessLog(1, 1, DateTime.UtcNow.AddHours(-2), "IN", gateId: 3),
                AccessLog(2, 1, DateTime.UtcNow.AddHours(-1), "OUT", gateId: 3),
                AccessLog(3, 1, DateTime.UtcNow.AddHours(-3), "IN", gateId: 3));
            await db.SaveChangesAsync();

            var service = new UebaService(db);
            var profile = await service.BuildProfileAsync(1);

            Assert.Equal(3, profile.TotalAccessCount);
            Assert.True(profile.RiskScore >= 0);
            Assert.False(string.IsNullOrWhiteSpace(profile.CommonGatesJson));
            Assert.False(string.IsNullOrWhiteSpace(profile.UnusualHoursJson));
            var stored = await db.UEBAProfiles.SingleAsync();
            Assert.Equal(profile.ProfileId, stored.ProfileId);
        }
    }

    [Fact]
    public async Task AnalyzeAccessLogAsync_CreatesFirstTimeAccessAnomaly_WhenNoLogsYet()
    {
        await using var db = CreateDatabase(out var provider);
        await using (provider)
        {
            db.Employees.Add(Employee(1));
            await db.SaveChangesAsync();

            var service = new UebaService(db);
            var log = AccessLog(10, 1, DateTime.UtcNow, "IN");

            await service.AnalyzeAccessLogAsync(log);

            var anomaly = Assert.Single(await db.UEBAAnomalies.ToListAsync());
            Assert.Equal(UEBAAnomalyTypes.FirstTimeAccess, anomaly.AnomalyType);
            Assert.Equal(UEBAStatuses.Open, anomaly.Status);
        }
    }

    [Fact]
    public async Task AnalyzeAccessLogAsync_DetectsUnusualTimeAndGateAndBypass()
    {
        await using var db = CreateDatabase(out var provider);
        await using (provider)
        {
            db.Employees.Add(Employee(1));
            // Existing profile with common gate 5 and typical hours 8-17.
            db.UEBAProfiles.Add(new UEBAProfile
            {
                EmployeeId = 1,
                TotalAccessCount = 100,
                AvgAccessPerDay = 2,
                TypicalStartHour = 8,
                TypicalEndHour = 17,
                WeekendAccessRatio = 1,
                BypassRate = 1,
                LastBuiltAt = DateTime.UtcNow,
                CommonGatesJson = "[{\"gateId\":5,\"count\":90,\"percentage\":90.0}]"
            });
            await db.SaveChangesAsync();

            var service = new UebaService(db);
            var log = AccessLog(11, 1, new DateTime(2026, 8, 16, 3, 0, 0, DateTimeKind.Utc),
                "IN", gateId: 9, isBypass: true);
            db.AccessLogs.Add(log);
            await db.SaveChangesAsync();

            await service.AnalyzeAccessLogAsync(log);

            var types = (await db.UEBAAnomalies.ToListAsync()).Select(a => a.AnomalyType).ToHashSet();
            Assert.Contains(UEBAAnomalyTypes.UnusualTime, types);
            Assert.Contains(UEBAAnomalyTypes.UnusualGate, types);
            Assert.Contains(UEBAAnomalyTypes.OutOfHours, types);
            Assert.Contains(UEBAAnomalyTypes.BypassPattern, types);
        }
    }

    [Fact]
    public async Task AnalyzeAccessLogAsync_DetectsUnusualFrequency()
    {
        await using var db = CreateDatabase(out var provider);
        await using (provider)
        {
            db.Employees.Add(Employee(1));
            db.UEBAProfiles.Add(new UEBAProfile
            {
                EmployeeId = 1,
                TotalAccessCount = 100,
                AvgAccessPerDay = 2,
                TypicalStartHour = 8,
                TypicalEndHour = 17,
                LastBuiltAt = DateTime.UtcNow
            });
            var now = DateTime.UtcNow;
            for (var i = 0; i < 7; i++)
            {
                db.AccessLogs.Add(AccessLog(100 + i, 1, now.AddMinutes(-i)));
            }
            await db.SaveChangesAsync();

            var service = new UebaService(db);
            var log = AccessLog(200, 1, now, "IN");
            db.AccessLogs.Add(log);
            await db.SaveChangesAsync();

            await service.AnalyzeAccessLogAsync(log);

            var anomaly = await db.UEBAAnomalies
                .FirstOrDefaultAsync(a => a.AnomalyType == UEBAAnomalyTypes.UnusualFrequency);
            Assert.NotNull(anomaly);
            Assert.Equal(UEBASeverities.High, anomaly!.Severity);
        }
    }

    [Fact]
    public async Task GetAnomaliesAsync_FiltersByCriteria()
    {
        await using var db = CreateDatabase(out var provider);
        await using (provider)
        {
            db.Employees.AddRange(Employee(1), Employee(2));
            db.UEBAAnomalies.AddRange(
                new UEBAAnomaly { EmployeeId = 1, AnomalyType = UEBAAnomalyTypes.UnusualTime, Severity = UEBASeverities.Medium, Status = UEBAStatuses.Open, EventTimestamp = DateTime.UtcNow },
                new UEBAAnomaly { EmployeeId = 2, AnomalyType = UEBAAnomalyTypes.UnusualGate, Severity = UEBASeverities.High, Status = UEBAStatuses.Resolved, EventTimestamp = DateTime.UtcNow });
            await db.SaveChangesAsync();

            var service = new UebaService(db);
            var byEmployee = await service.GetAnomaliesAsync(employeeId: 1);
            Assert.Single(byEmployee);
            Assert.Equal(1, byEmployee[0].EmployeeId);

            var byType = await service.GetAnomaliesAsync(type: UEBAAnomalyTypes.UnusualGate);
            Assert.Single(byType);
            Assert.Equal(UEBAAnomalyTypes.UnusualGate, byType[0].AnomalyType);

            var byStatus = await service.GetAnomaliesAsync(status: UEBAStatuses.Resolved);
            Assert.Single(byStatus);
        }
    }

    [Fact]
    public async Task ResolveAnomalyAsync_MarksResolved()
    {
        await using var db = CreateDatabase(out var provider);
        await using (provider)
        {
            db.UEBAAnomalies.Add(new UEBAAnomaly
            {
                EmployeeId = 1,
                AnomalyType = UEBAAnomalyTypes.UnusualTime,
                Severity = UEBASeverities.Medium,
                Status = UEBAStatuses.Open
            });
            await db.SaveChangesAsync();

            var service = new UebaService(db);
            await service.ResolveAnomalyAsync(1, "Verified", 42);

            var stored = await db.UEBAAnomalies.SingleAsync();
            Assert.Equal(UEBAStatuses.Resolved, stored.Status);
            Assert.Equal("Verified", stored.Resolution);
            Assert.Equal(42, stored.ResolvedBy);
            Assert.NotNull(stored.ResolvedAt);
        }
    }

    [Fact]
    public async Task ResolveAnomalyAsync_Throws_WhenMissing()
    {
        await using var db = CreateDatabase(out var provider);
        await using (provider)
        {
            var service = new UebaService(db);
            await Assert.ThrowsAsync<KeyNotFoundException>(
                () => service.ResolveAnomalyAsync(999, "x", 1));
        }
    }

    [Fact]
    public async Task MarkFalsePositiveAsync_SetsStatus()
    {
        await using var db = CreateDatabase(out var provider);
        await using (provider)
        {
            db.UEBAAnomalies.Add(new UEBAAnomaly
            {
                EmployeeId = 1,
                AnomalyType = UEBAAnomalyTypes.UnusualTime,
                Severity = UEBASeverities.Medium,
                Status = UEBAStatuses.Open
            });
            await db.SaveChangesAsync();

            var service = new UebaService(db);
            await service.MarkFalsePositiveAsync(1, 7);

            var stored = await db.UEBAAnomalies.SingleAsync();
            Assert.Equal(UEBAStatuses.FalsePositive, stored.Status);
            Assert.Equal(7, stored.ResolvedBy);
        }
    }

    [Fact]
    public async Task GetSummaryAsync_AggregatesCounts()
    {
        await using var db = CreateDatabase(out var provider);
        await using (provider)
        {
            db.UEBAProfiles.Add(new UEBAProfile { EmployeeId = 1, RiskScore = 80 });
            db.UEBAProfiles.Add(new UEBAProfile { EmployeeId = 2, RiskScore = 10 });
            db.UEBAAnomalies.Add(new UEBAAnomaly
            {
                EmployeeId = 1, AnomalyType = UEBAAnomalyTypes.UnusualTime,
                Severity = UEBASeverities.Medium, Status = UEBAStatuses.Open
            });
            await db.SaveChangesAsync();

            var service = new UebaService(db);
            var summary = await service.GetSummaryAsync();

            var props = summary.GetType().GetProperties().ToDictionary(p => p.Name, p => p.GetValue(summary));
            Assert.Equal(1, Convert.ToInt32(props["openAnomalies"]));
            Assert.Equal(1, Convert.ToInt32(props["highRiskProfiles"]));
            Assert.Equal(2, Convert.ToInt32(props["totalProfiles"]));
        }
    }
}
