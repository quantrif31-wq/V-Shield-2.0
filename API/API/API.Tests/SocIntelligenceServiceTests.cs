using API.Data;
using API.Models;
using API.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace API.Tests;

public sealed class SocIntelligenceServiceTests
{
    private static int _databaseCounter;

    private static ApplicationDbContext CreateDatabase(out ServiceProvider provider)
    {
        var databaseName = $"soc-{System.Threading.Interlocked.Increment(ref _databaseCounter)}";
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
    public async Task ClassifyAlarmAsync_ReturnsNull_WhenAlarmMissing()
    {
        await using var db = CreateDatabase(out var provider);
        await using (provider)
        {
            var service = new SocIntelligenceService(db);
            Assert.Null(await service.ClassifyAlarmAsync(999));
        }
    }

    [Fact]
    public async Task ClassifyAlarmAsync_DetectsCriticalKeywords()
    {
        await using var db = CreateDatabase(out var provider);
        await using (provider)
        {
            db.Alarms.Add(new Alarm { AlarmId = 1, Summary = "Forced intrusion breach detected", AlarmType = "Generic", Severity = "Medium" });
            await db.SaveChangesAsync();

            var service = new SocIntelligenceService(db);
            var result = await service.ClassifyAlarmAsync(1);

            Assert.NotNull(result);
            Assert.Equal("Critical", result!.PredictedSeverity);
            Assert.Contains("breach", result.MatchedKeywords);
        }
    }

    [Fact]
    public async Task ClassifyAlarmAsync_Demotes_WhenLowConfidenceSource()
    {
        await using var db = CreateDatabase(out var provider);
        await using (provider)
        {
            var securityEvent = new SecurityEvent { SecurityEventId = 1, Confidence = 30 };
            db.SecurityEvents.Add(securityEvent);
            db.Alarms.Add(new Alarm
            {
                AlarmId = 1,
                Summary = "fire emergency",
                AlarmType = "Generic",
                Severity = "High",
                SecurityEventId = 1
            });
            await db.SaveChangesAsync();

            var service = new SocIntelligenceService(db);
            var result = await service.ClassifyAlarmAsync(1);

            Assert.Equal("High", result!.PredictedSeverity);
            Assert.Contains("low-confidence-source", result.MatchedKeywords);
        }
    }

    [Fact]
    public async Task RecommendSopAsync_ReturnsScoredTemplates()
    {
        await using var db = CreateDatabase(out var provider);
        await using (provider)
        {
            db.Alarms.Add(new Alarm { AlarmId = 1, Summary = "Tamper detected", AlarmType = "Tamper", Severity = "High" });
            db.SopTemplates.AddRange(
                new SopTemplate { SopTemplateId = 1, Name = "Tamper High response", AlarmType = "Tamper", IsActive = true, ChecklistJson = """["step1","step2"]""" },
                new SopTemplate { SopTemplateId = 2, Name = "Generic drill", AlarmType = "Generic", IsActive = true, ChecklistJson = "[]" });
            await db.SaveChangesAsync();

            var service = new SocIntelligenceService(db);
            var recommendations = await service.RecommendSopAsync(1);

            Assert.NotEmpty(recommendations);
            Assert.All(recommendations, r => Assert.True(r.RelevanceScore > 0));
        }
    }

    [Fact]
    public async Task RecommendSopAsync_ReturnsEmpty_WhenAlarmMissing()
    {
        await using var db = CreateDatabase(out var provider);
        await using (provider)
        {
            var service = new SocIntelligenceService(db);
            Assert.Empty(await service.RecommendSopAsync(999));
        }
    }

    [Fact]
    public async Task PredictEscalationRiskAsync_ClosedAlarm_HasZeroRisk()
    {
        await using var db = CreateDatabase(out var provider);
        await using (provider)
        {
            db.Alarms.Add(new Alarm { AlarmId = 1, Summary = "test", AlarmType = "Generic", Severity = "Low", State = "Closed" });
            await db.SaveChangesAsync();

            var service = new SocIntelligenceService(db);
            var result = await service.PredictEscalationRiskAsync(1);

            Assert.NotNull(result);
            Assert.Equal(0, result!.RiskScore);
            Assert.Equal("da_dong", result.RiskLevel);
        }
    }

    [Fact]
    public async Task PredictEscalationRiskAsync_UnassignedCritical_HasHighRisk()
    {
        await using var db = CreateDatabase(out var provider);
        await using (provider)
        {
            db.Alarms.Add(new Alarm
            {
                AlarmId = 1,
                Summary = "breach",
                AlarmType = "Breach",
                Severity = "Critical",
                State = "New",
                CreatedAtUtc = DateTime.UtcNow.AddHours(-6),
                AssignedToUserId = null
            });
            await db.SaveChangesAsync();

            var service = new SocIntelligenceService(db);
            var result = await service.PredictEscalationRiskAsync(1);

            Assert.NotNull(result);
            Assert.True(result!.RiskScore >= 50);
            Assert.Contains(result.Factors, f => f.Contains("Chua duoc phan cong"));
            Assert.Contains(result.Factors, f => f.Contains("Critical"));
        }
    }

    [Fact]
    public async Task DetectAnomaliesAsync_DetectsUnassignedCritical()
    {
        await using var db = CreateDatabase(out var provider);
        await using (provider)
        {
            db.Alarms.Add(new Alarm { AlarmId = 1, Summary = "x", AlarmType = "Generic", Severity = "Critical", State = "New", AssignedToUserId = null });
            await db.SaveChangesAsync();

            var service = new SocIntelligenceService(db);
            var anomalies = await service.DetectAnomaliesAsync();

            Assert.Contains(anomalies, a => a.Type == "unassigned_critical");
        }
    }

    [Fact]
    public async Task GetIntelligenceAsync_ReturnsSummaryAndStats()
    {
        await using var db = CreateDatabase(out var provider);
        await using (provider)
        {
            db.Alarms.Add(new Alarm { AlarmId = 1, Summary = "breach", AlarmType = "Breach", Severity = "Critical", State = "New", CreatedAtUtc = DateTime.UtcNow });
            await db.SaveChangesAsync();

            var service = new SocIntelligenceService(db);
            var result = await service.GetIntelligenceAsync();

            var summary = result.GetType().GetProperty("summary")!.GetValue(result)!.ToString();
            Assert.Contains("1 alarm", summary);
            var stats = result.GetType().GetProperty("statistics")!.GetValue(result)!;
            Assert.Equal(1, Convert.ToInt32(stats.GetType().GetProperty("totalToday")!.GetValue(stats)));
            Assert.Equal(1, Convert.ToInt32(stats.GetType().GetProperty("criticalOpenAlarms")!.GetValue(stats)));
        }
    }

    [Fact]
    public async Task GetIntelligenceAsync_EmptyData_ProducesLowRisk()
    {
        await using var db = CreateDatabase(out var provider);
        await using (provider)
        {
            var service = new SocIntelligenceService(db);
            var result = await service.GetIntelligenceAsync();

            var risk = result.GetType().GetProperty("overallRisk")!.GetValue(result)!.ToString();
            Assert.Equal("thap", risk);
        }
    }
}
