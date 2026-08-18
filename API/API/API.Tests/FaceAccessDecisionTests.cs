using API.Data;
using API.Models;
using API.Services.AccessPolicyComparison;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace API.Tests;

public sealed class FaceAccessDecisionTests
{
    [Theory]
    [InlineData("Allow", "Allow", "Allowed")]
    [InlineData("Allow", "Deny", "Denied")]
    [InlineData("Deny", "Allow", "Denied")]
    [InlineData("Deny", "Deny", "Denied")]
    [InlineData("Allow", "Indeterminate", "ReviewRequired")]
    [InlineData("Indeterminate", "Allow", "ReviewRequired")]
    [InlineData("Deny", "Indeterminate", "Denied")]
    [InlineData("Indeterminate", "Deny", "Denied")]
    [InlineData("NotConfigured", "Allow", "ReviewRequired")]
    [InlineData("Allow", "NotConfigured", "ReviewRequired")]
    [InlineData("NotConfigured", "NotConfigured", "ReviewRequired")]
    [InlineData("Error", "Allow", "Indeterminate")]
    [InlineData("Allow", "Error", "Indeterminate")]
    public void Combine_ImplementsApprovedPrecedence(string legacy, string enterprise,
        string expected)
    {
        Assert.Equal(expected, FaceAccessDecisionProcessor.Combine(legacy, enterprise).Decision);
    }

    [Theory]
    [InlineData("EnterpriseCredentialExpired")]
    [InlineData("EnterpriseCredentialRevoked")]
    [InlineData("EnterpriseFaceCredentialBindingRevoked")]
    [InlineData("EmployeeInactive")]
    public void Combine_ExplicitLifecycleDenialWins(string reason)
    {
        var result = FaceAccessDecisionProcessor.Combine(
            PolicyEvaluationDecisions.Allow, PolicyEvaluationDecisions.Indeterminate,
            "LegacyAllowed", reason, FacePolicyMappingStatuses.Resolved);
        Assert.Equal(FaceAccessDecisionStatuses.Denied, result.Decision);
        Assert.Equal(FaceAccessDecisionReasons.ExplicitDeny, result.ReasonCode);
    }

    [Fact]
    public void Combine_InvalidMappingIsDenied()
    {
        var result = FaceAccessDecisionProcessor.Combine(
            PolicyEvaluationDecisions.Indeterminate, PolicyEvaluationDecisions.Indeterminate,
            "MappingUnavailable", "MappingUnavailable", FacePolicyMappingStatuses.GateMissing);
        Assert.Equal(FaceAccessDecisionStatuses.Denied, result.Decision);
        Assert.Equal(FaceAccessDecisionReasons.MappingInvalid, result.ReasonCode);
    }

    [Fact]
    public async Task Processor_IsIdempotentAndPersistsImmutableSnapshot()
    {
        var root = new InMemoryDatabaseRoot();
        var services = Services(root);
        await using var provider = services.BuildServiceProvider();
        await SeedAsync(provider);

        var processor = provider.GetRequiredService<IFaceAccessDecisionProcessor>();
        await processor.RunCycleAsync(default);
        await processor.RunCycleAsync(default);

        using var scope = provider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var decision = Assert.Single(await db.FaceAccessDecisions.AsNoTracking().ToListAsync());
        Assert.Equal(FaceAccessDecisionStatuses.Allowed, decision.Decision);
        Assert.Equal(FaceAccessDecisionReasons.BothEnginesAllowed, decision.ReasonCode);
        Assert.Equal(64, decision.InputFingerprint.Length);
        Assert.Contains("\"occurredAtUtc\"", decision.PolicySnapshotJson);
        Assert.Contains("\"LegacyPermissionId\":7", decision.PolicySnapshotJson);

        var tracked = await db.FaceAccessDecisions.SingleAsync();
        tracked.ReasonCode = "changed";
        await Assert.ThrowsAsync<InvalidOperationException>(() => db.SaveChangesAsync());
        db.Entry(tracked).State = EntityState.Unchanged;
        db.FaceAccessDecisions.Remove(tracked);
        await Assert.ThrowsAsync<InvalidOperationException>(() => db.SaveChangesAsync());
    }

    [Fact]
    public void Model_EnforcesOneDecisionPerEventAndComparisonWithNoActionForeignKeys()
    {
        using var db = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"decision-model-{Guid.NewGuid():N}")
            .ConfigureWarnings(w => w.Ignore(
                Microsoft.EntityFrameworkCore.Diagnostics.CoreEventId.ManyServiceProvidersCreatedWarning)).Options);
        var entity = db.Model.FindEntityType(typeof(FaceAccessDecision))!;
        Assert.True(entity.GetIndexes().Single(x =>
            x.Properties.Single().Name == nameof(FaceAccessDecision.FaceRecognitionEventId)).IsUnique);
        Assert.True(entity.GetIndexes().Single(x =>
            x.Properties.Single().Name == nameof(FaceAccessDecision.FaceAccessPolicyComparisonId)).IsUnique);
        Assert.All(entity.GetForeignKeys(), x => Assert.Equal(DeleteBehavior.NoAction, x.DeleteBehavior));
    }

    private static ServiceCollection Services(InMemoryDatabaseRoot root)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDbContext<ApplicationDbContext>(x =>
            x.UseInMemoryDatabase("face-access-decisions", root)
                .ConfigureWarnings(w => w.Ignore(
                    Microsoft.EntityFrameworkCore.Diagnostics.CoreEventId.ManyServiceProvidersCreatedWarning)));
        services.AddSingleton(new FaceAccessPolicyComparisonOptions {
            BatchSize = 100, MaxParallelism = 2, EvaluationVersion = 1,
            TimeZoneId = "Asia/Ho_Chi_Minh"
        });
        services.AddSingleton<FaceAccessDecisionProcessor>();
        services.AddSingleton<IFaceAccessDecisionProcessor>(sp =>
            sp.GetRequiredService<FaceAccessDecisionProcessor>());
        return services;
    }

    private static async Task SeedAsync(IServiceProvider provider)
    {
        using var scope = provider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var occurred = new DateTime(2026, 7, 29, 2, 38, 43, DateTimeKind.Utc);
        var recognition = new FaceRecognitionEvent {
            Id = 1, RuntimeEventId = Guid.NewGuid(), CameraId = "face-test",
            EventType = "Recognized", MatchStatus = FaceRecognitionMatchStatuses.Matched,
            OccurredAtUtc = occurred, ReceivedAtUtc = occurred
        };
        db.FaceRecognitionEvents.Add(recognition);
        db.FaceAccessPolicyComparisons.Add(new FaceAccessPolicyComparison {
            Id = 1, FaceRecognitionEventId = 1, FaceRecognitionEvent = recognition,
            EmployeeId = 9, CameraId = "face-test", LaneId = 2, GateId = 3, AccessPointId = 4,
            OccurredAtUtc = occurred, EvaluatedAtUtc = occurred,
            LegacyDecision = PolicyEvaluationDecisions.Allow, LegacyReasonCode = "LegacyAllowed",
            LegacyPermissionId = 7,
            EnterpriseDecision = PolicyEvaluationDecisions.Allow,
            EnterpriseReasonCode = "EnterpriseAllowed",
            EnterprisePolicyVersionId = 8, EnterpriseRuleId = 9, EnterpriseScheduleId = 10,
            ComparisonResult = PolicyComparisonResults.AgreeAllow,
            MappingStatus = FacePolicyMappingStatuses.Resolved, EvaluationVersion = 1,
            LegacyInputFingerprint = new string('a', 64),
            EnterpriseInputFingerprint = new string('b', 64),
            ScheduleTimeZoneId = "Asia/Ho_Chi_Minh", CreatedAtUtc = occurred
        });
        await db.SaveChangesAsync();
    }
}
