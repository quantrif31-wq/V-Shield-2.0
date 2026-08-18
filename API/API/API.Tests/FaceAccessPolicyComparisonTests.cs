using API.Data;
using API.Models;
using API.Services.AccessPolicyComparison;
using API.Services.AccessCredentials;
using API.Services.FaceCredentialBindings;
using API.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace API.Tests;

public sealed class FaceAccessPolicyComparisonTests
{
    [Theory]
    [InlineData(PolicyEvaluationDecisions.Allow, PolicyEvaluationDecisions.Allow, PolicyComparisonResults.AgreeAllow)]
    [InlineData(PolicyEvaluationDecisions.Deny, PolicyEvaluationDecisions.Deny, PolicyComparisonResults.AgreeDeny)]
    [InlineData(PolicyEvaluationDecisions.Allow, PolicyEvaluationDecisions.Deny, PolicyComparisonResults.LegacyAllowEnterpriseDeny)]
    [InlineData(PolicyEvaluationDecisions.Deny, PolicyEvaluationDecisions.Allow, PolicyComparisonResults.LegacyDenyEnterpriseAllow)]
    [InlineData(PolicyEvaluationDecisions.NotConfigured, PolicyEvaluationDecisions.NotConfigured, PolicyComparisonResults.BothNotConfigured)]
    public void Comparison_DoesNotCreateCombinedAccessDecision(
        string legacy, string enterprise, string expected)
    {
        Assert.Equal(expected, FaceAccessPolicyComparisonProcessor.Compare(
            legacy, enterprise, FacePolicyMappingStatuses.Resolved));
    }

    [Fact]
    public void Fingerprint_IsDeterministicAndChangesWithPolicyInput()
    {
        Assert.Equal(PolicyFingerprint.Create(1, 2, true), PolicyFingerprint.Create(1, 2, true));
        Assert.NotEqual(PolicyFingerprint.Create(1, 2, true), PolicyFingerprint.Create(1, 2, false));
    }

    [Fact]
    public async Task LegacyEvaluator_ReflectsAllowDenyMissingAndDuplicateRows()
    {
        await using var db = Database();
        db.Employees.Add(new Employee { EmployeeId = 1, FullName = "Test" });
        db.Gates.Add(new Gate { GateId = 2, GateName = "Gate" });
        await db.SaveChangesAsync();
        var evaluator = new LegacyGateAccessEvaluator(db);
        Assert.Equal(PolicyEvaluationDecisions.NotConfigured,
            (await evaluator.EvaluateAsync(new(1, 2, DateTime.UtcNow), default)).Decision);

        db.EmployeeAccessPermissions.Add(new EmployeeAccessPermission {
            EmployeeId = 1, GateId = 2, IsAllowed = true
        });
        await db.SaveChangesAsync();
        Assert.Equal("LegacyAllowed",
            (await evaluator.EvaluateAsync(new(1, 2, DateTime.UtcNow), default)).ReasonCode);

        db.EmployeeAccessPermissions.Add(new EmployeeAccessPermission {
            EmployeeId = 1, GateId = 2, IsAllowed = false
        });
        await db.SaveChangesAsync();
        Assert.Equal("LegacyDuplicatePermissions",
            (await evaluator.EvaluateAsync(new(1, 2, DateTime.UtcNow), default)).ReasonCode);
    }

    [Fact]
    public async Task EnterpriseEvaluator_RequiresCredentialAndExplicitDenyWins()
    {
        await using var db = Database();
        var options = Options();
        var evaluator = new EnterpriseAccessPolicyEvaluator(db, options);
        var now = new DateTime(2026, 7, 28, 4, 0, 0, DateTimeKind.Utc);
        Assert.Equal("EnterpriseMissingCredentialContext",
            (await evaluator.EvaluateAsync(new(1, 10, now, null), default)).ReasonCode);

        var version = new AccessPolicyVersion {
            AccessPolicyVersionId = 1, Name = "Active", Status = "Active",
            ActivatedAtUtc = now.AddDays(-1)
        };
        db.AccessPolicyVersions.Add(version);
        db.AccessRules.AddRange(
            new AccessRule {
                AccessRuleId = 1, AccessPolicyVersionId = 1, AccessLevelId = 1,
                SubjectType = "Employee", SubjectId = 1, AccessPointId = 10,
                CredentialType = "Face", AllowAccess = true
            },
            new AccessRule {
                AccessRuleId = 2, AccessPolicyVersionId = 1, AccessLevelId = 1,
                SubjectType = "Employee", SubjectId = 1, AccessPointId = 10,
                CredentialType = "Face", AllowAccess = false
            });
        await db.SaveChangesAsync();
        var result = await evaluator.EvaluateAsync(new(1, 10, now, "Face"), default);
        Assert.Equal(PolicyEvaluationDecisions.Deny, result.Decision);
        Assert.Equal("EnterpriseExplicitDenied", result.ReasonCode);
        Assert.Equal(2, result.RuleId);
    }

    [Fact]
    public async Task EnterpriseSchedule_UsesConfiguredVietnamTimezoneAndOccurredTime()
    {
        await using var db = Database();
        var options = Options();
        var now = new DateTime(2026, 7, 28, 1, 0, 0, DateTimeKind.Utc); // 08:00 VN
        var schedule = new AccessSchedule {
            AccessScheduleId = 1, Name = "Morning", DaysOfWeek = "Tue",
            StartTime = new TimeSpan(8, 0, 0), EndTime = new TimeSpan(9, 0, 0)
        };
        db.AccessSchedules.Add(schedule);
        db.AccessRules.Add(new AccessRule {
            AccessRuleId = 1, AccessLevelId = 1, SubjectType = "Employee",
            SubjectId = 1, AccessPointId = 10, CredentialType = "Face",
            AllowAccess = true, AccessScheduleId = 1
        });
        await db.SaveChangesAsync();
        var result = await new EnterpriseAccessPolicyEvaluator(db, options)
            .EvaluateAsync(new(1, 10, now, "Face"), default);
        Assert.Equal(PolicyEvaluationDecisions.Allow, result.Decision);
    }

    [Fact]
    public async Task Processor_IsIdempotentAndDoesNotCreateAccessDecision()
    {
        var services = new ServiceCollection();
        var root = new InMemoryDatabaseRoot();
        services.AddLogging();
        services.AddDbContext<ApplicationDbContext>(x =>
            x.UseInMemoryDatabase("comparison-processor", root)
                .ConfigureWarnings(w => w.Ignore(
                    Microsoft.EntityFrameworkCore.Diagnostics.CoreEventId.ManyServiceProvidersCreatedWarning)));
        services.AddSingleton(Options());
        services.AddScoped<ILegacyGateAccessEvaluator, LegacyGateAccessEvaluator>();
        services.AddScoped<IEnterpriseAccessPolicyEvaluator, EnterpriseAccessPolicyEvaluator>();
        services.AddScoped<IAccessCredentialStateEvaluator, AccessCredentialStateEvaluator>();
        services.AddScoped<IAccessCredentialIdentifierProtector>(_ =>
            new AccessCredentialIdentifierProtector(new AccessCredentialOptions
            {
                IdentifierHmacKey = "test-key-with-at-least-thirty-two-characters!"
            }));
        services.AddScoped<AccessCredentialService>();
        services.AddScoped<IAccessCredentialContextResolver>(sp =>
            sp.GetRequiredService<AccessCredentialService>());
        services.AddScoped<IFaceCredentialBindingService, FaceCredentialBindingService>();
        services.AddScoped<ICurrentUserContext, TestUser>();
        services.AddSingleton<FaceAccessPolicyComparisonProcessor>();
        services.AddSingleton<IFaceAccessPolicyComparisonProcessor>(sp =>
            sp.GetRequiredService<FaceAccessPolicyComparisonProcessor>());
        await using var provider = services.BuildServiceProvider();
        using (var scope = provider.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            db.Employees.Add(new Employee { EmployeeId = 1, FullName = "Test", Status = true });
            db.Gates.Add(new Gate { GateId = 1, GateName = "Gate" });
            db.Lanes.Add(new Lane { LaneId = 1, SiteId = 1, GateId = 1, AccessPointId = 1 });
            db.Cameras.Add(new Camera { CameraId = 1, CameraName = "Face" });
            db.FaceCameraConfigurations.Add(new FaceCameraConfiguration {
                Id = 1, CameraId = 1, RuntimeCameraId = "face-1", LaneId = 1
            });
            db.EmployeeAccessPermissions.Add(new EmployeeAccessPermission {
                EmployeeId = 1, GateId = 1, IsAllowed = true
            });
            db.FaceRecognitionEvents.Add(new FaceRecognitionEvent {
                Id = 1, RuntimeEventId = Guid.NewGuid(), CameraId = "face-1",
                FaceCameraConfigurationId = 1, LaneId = 1, EmployeeId = 1,
                EventType = "Recognized", MatchStatus = FaceRecognitionMatchStatuses.Matched,
                OccurredAtUtc = DateTime.UtcNow, ReceivedAtUtc = DateTime.UtcNow
            });
            await db.SaveChangesAsync();
        }

        var processor = provider.GetRequiredService<IFaceAccessPolicyComparisonProcessor>();
        await processor.RunCycleAsync(default);
        await processor.RunCycleAsync(default);
        using (var scope = provider.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var comparison = Assert.Single(await db.FaceAccessPolicyComparisons.ToListAsync());
            Assert.Equal(PolicyComparisonResults.EnterpriseIndeterminate, comparison.ComparisonResult);
            Assert.Empty(await db.AccessDecisions.ToListAsync());
            Assert.Equal(FaceRecognitionMatchStatuses.Matched,
                (await db.FaceRecognitionEvents.SingleAsync()).MatchStatus);
        }
    }

    private static FaceAccessPolicyComparisonOptions Options()
    {
        var id = "Asia/Ho_Chi_Minh";
        return new() { TimeZoneId = id, TimeZone = TimeZoneInfo.FindSystemTimeZoneById(id) };
    }

    private static ApplicationDbContext Database()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"policy-comparison-{Guid.NewGuid():N}")
            .ConfigureWarnings(w => w.Ignore(
                Microsoft.EntityFrameworkCore.Diagnostics.CoreEventId.ManyServiceProvidersCreatedWarning)).Options;
        return new ApplicationDbContext(options);
    }

    private sealed class TestUser : ICurrentUserContext
    {
        public int? UserId => null;
        public string? Username => "test";
    }
}
