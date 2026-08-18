using API.Data;
using API.Models;
using API.Services;
using API.Services.AI;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace API.Tests;

public sealed class UebaRiskGraphServiceTests
{
    private static int _databaseCounter;

    private static (ServiceProvider provider, ApplicationDbContext db) CreateDatabase()
    {
        var databaseName = $"ueba-graph-{System.Threading.Interlocked.Increment(ref _databaseCounter)}";
        var services = new ServiceCollection();
        services.AddDbContext<ApplicationDbContext>(options =>
            options
                .UseInMemoryDatabase(databaseName, new InMemoryDatabaseRoot())
                .ConfigureWarnings(w => w.Ignore(
                    Microsoft.EntityFrameworkCore.Diagnostics.CoreEventId.ManyServiceProvidersCreatedWarning)));
        var provider = services.BuildServiceProvider();
        return (provider, provider.GetRequiredService<ApplicationDbContext>());
    }

    [Fact]
    public async Task ExplainEmployeeRiskAsync_Throws_WhenEmployeeMissing()
    {
        await using var db = CreateDatabase().db;
        var ai = new Mock<IAiRecommendationService>();
        var service = new UebaRiskGraphService(db, ai.Object);

        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => service.ExplainEmployeeRiskAsync(999, null));
    }

    [Fact]
    public async Task ExplainEmployeeRiskAsync_IncludesProfileAndAnomaliesInInput()
    {
        var (provider, db) = CreateDatabase();
        await using (provider)
        {
            var ai = new Mock<IAiRecommendationService>();
            ai.Setup(x => x.AnalyzeAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<Dictionary<string, string>>(),
                It.IsAny<int?>(), It.IsAny<string?>()))
                .ReturnsAsync(new AiRecommendationResult
                {
                    AnalysisJobId = 1,
                    Provider = "test",
                    Summary = "ok"
                });

            var dept = new Department { Name = "Phòng IT" };
            db.Departments.Add(dept);
            db.Employees.Add(new Employee { EmployeeId = 1, FullName = "Nguyen Van A", Department = dept, Status = true, LifecycleStatus = EmployeeLifecycleStates.Active });
            db.UEBAProfiles.Add(new UEBAProfile { EmployeeId = 1, RiskScore = 75, TotalAccessCount = 100, AvgAccessPerDay = 2.5, TypicalStartHour = 8, TypicalEndHour = 17, BypassRate = 1.0, WeekendAccessRatio = 2.0 });
            db.UEBAAnomalies.Add(new UEBAAnomaly { EmployeeId = 1, AnomalyType = UEBAAnomalyTypes.UnusualTime, Severity = UEBASeverities.Medium, Status = UEBAStatuses.Open });
            db.AccessLogs.Add(new AccessLog { LogId = 1, EmployeeId = 1, Timestamp = DateTime.UtcNow, Direction = "IN", ResultStatus = "Approved" });
            await db.SaveChangesAsync();

            var service = new UebaRiskGraphService(db, ai.Object);
            var result = await service.ExplainEmployeeRiskAsync(1, 42);

            Assert.Equal(1, result.AnalysisJobId);
            ai.Verify(x => x.AnalyzeAsync(
                "ueba", "employee", "1", "ueba-risk-analysis",
                It.Is<Dictionary<string, string>>(d =>
                    d["risk_score"] == "75.0" &&
                    d["risk_factors"].Contains("UnusualTime") &&
                    d["profile_summary"].Contains("Tong truy cap: 100")),
                It.IsAny<int?>(), It.IsAny<string?>()), Times.Once);
        }
    }

    [Fact]
    public async Task ExplainEmployeeRiskAsync_WithoutProfile_UsesPlaceholders()
    {
        var (provider, db) = CreateDatabase();
        await using (provider)
        {
            var ai = new Mock<IAiRecommendationService>();
            ai.Setup(x => x.AnalyzeAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<Dictionary<string, string>>(),
                It.IsAny<int?>(), It.IsAny<string?>()))
                .ReturnsAsync(new AiRecommendationResult { AnalysisJobId = 2 });

            db.Employees.Add(new Employee { EmployeeId = 2, FullName = "Nguyen Van B", Status = true, LifecycleStatus = EmployeeLifecycleStates.Active });
            await db.SaveChangesAsync();

            var service = new UebaRiskGraphService(db, ai.Object);
            var result = await service.ExplainEmployeeRiskAsync(2, null);

            Assert.Equal(2, result.AnalysisJobId);
            ai.Verify(x => x.AnalyzeAsync(
                "ueba", "employee", "2", "ueba-risk-analysis",
                It.Is<Dictionary<string, string>>(d =>
                    d["risk_score"] == "Chua co profile" &&
                    d["risk_factors"] == "Khong co anomaly open"),
                It.IsAny<int?>(), It.IsAny<string?>()), Times.Once);
        }
    }
}
