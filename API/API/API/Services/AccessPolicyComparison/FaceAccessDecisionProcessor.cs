using System.Text.Json;
using API.Data;
using API.Models;
using Microsoft.EntityFrameworkCore;

namespace API.Services.AccessPolicyComparison;

public interface IFaceAccessDecisionProcessor
{
    Task RunCycleAsync(CancellationToken token);
    Task<object> HealthAsync(CancellationToken token);
}

public sealed class FaceAccessDecisionProcessor(
    IServiceScopeFactory scopeFactory,
    FaceAccessPolicyComparisonOptions options,
    ILogger<FaceAccessDecisionProcessor> logger)
    : BackgroundService, IFaceAccessDecisionProcessor
{
    private readonly SemaphoreSlim _gate = new(1, 1);

    public async Task RunCycleAsync(CancellationToken token)
    {
        if (!await _gate.WaitAsync(0, token)) return;
        try
        {
            using var scope = scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var ids = await db.FaceAccessPolicyComparisons.AsNoTracking()
                .Where(x => !db.FaceAccessDecisions.Any(d =>
                    d.FaceAccessPolicyComparisonId == x.Id ||
                    d.FaceRecognitionEventId == x.FaceRecognitionEventId))
                .OrderBy(x => x.OccurredAtUtc).ThenBy(x => x.Id)
                .Select(x => x.Id).Take(options.BatchSize).ToListAsync(token);
            await Parallel.ForEachAsync(ids, new ParallelOptions {
                MaxDegreeOfParallelism = options.MaxParallelism, CancellationToken = token
            }, async (id, ct) =>
            {
                try
                {
                    using var child = scopeFactory.CreateScope();
                    await ProcessOneAsync(child.ServiceProvider, id, ct);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    logger.LogWarning(ex, "Face access decision failed for comparison {ComparisonId}", id);
                }
            });
        }
        finally { _gate.Release(); }
    }

    public async Task<object> HealthAsync(CancellationToken token)
    {
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        return new {
            enabled = options.ProcessorEnabled,
            decisionCount = await db.FaceAccessDecisions.CountAsync(token),
            pendingCount = await db.FaceAccessPolicyComparisons.CountAsync(x =>
                !db.FaceAccessDecisions.Any(d => d.FaceAccessPolicyComparisonId == x.Id), token),
            evaluationVersion = options.EvaluationVersion,
            timeZoneId = options.TimeZoneId
        };
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(options.PollIntervalMilliseconds));
        try
        {
            do { await RunCycleAsync(stoppingToken); }
            while (await timer.WaitForNextTickAsync(stoppingToken));
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { }
    }

    public static (string Decision, string ReasonCode) Combine(string legacy, string enterprise)
        => Combine(legacy, enterprise, null, null, FacePolicyMappingStatuses.Resolved);

    public static (string Decision, string ReasonCode) Combine(
        string legacy, string enterprise, string? legacyReason, string? enterpriseReason,
        string mappingStatus)
    {
        if (legacy == PolicyEvaluationDecisions.Error || enterprise == PolicyEvaluationDecisions.Error)
            return (FaceAccessDecisionStatuses.Indeterminate, FaceAccessDecisionReasons.EvaluationError);
        if (mappingStatus != FacePolicyMappingStatuses.Resolved)
            return (FaceAccessDecisionStatuses.Denied, FaceAccessDecisionReasons.MappingInvalid);
        if (ExplicitDenyReason(legacyReason) || ExplicitDenyReason(enterpriseReason))
            return (FaceAccessDecisionStatuses.Denied, FaceAccessDecisionReasons.ExplicitDeny);
        if (legacy == PolicyEvaluationDecisions.Deny && enterprise == PolicyEvaluationDecisions.Deny)
            return (FaceAccessDecisionStatuses.Denied, FaceAccessDecisionReasons.BothEnginesDenied);
        if (legacy == PolicyEvaluationDecisions.Deny)
            return (FaceAccessDecisionStatuses.Denied, FaceAccessDecisionReasons.LegacyDenied);
        if (enterprise == PolicyEvaluationDecisions.Deny)
            return (FaceAccessDecisionStatuses.Denied, FaceAccessDecisionReasons.EnterpriseDenied);
        if (legacy == PolicyEvaluationDecisions.NotConfigured ||
            enterprise == PolicyEvaluationDecisions.NotConfigured)
            return (FaceAccessDecisionStatuses.ReviewRequired, FaceAccessDecisionReasons.PolicyNotConfigured);
        if (legacy == PolicyEvaluationDecisions.Indeterminate ||
            enterprise == PolicyEvaluationDecisions.Indeterminate)
            return (FaceAccessDecisionStatuses.ReviewRequired, FaceAccessDecisionReasons.EvaluationIncomplete);
        if (legacy == PolicyEvaluationDecisions.Allow && enterprise == PolicyEvaluationDecisions.Allow)
            return (FaceAccessDecisionStatuses.Allowed, FaceAccessDecisionReasons.BothEnginesAllowed);
        return (FaceAccessDecisionStatuses.Indeterminate, FaceAccessDecisionReasons.EvaluationError);
    }

    private static bool ExplicitDenyReason(string? reason) =>
        reason is "EmployeeInactive" or
            "EnterpriseCredentialExpired" or
            "EnterpriseCredentialRevoked" or
            "EnterpriseCredentialInactive" or
            "EnterpriseFaceCredentialBindingRevoked";

    private static async Task ProcessOneAsync(IServiceProvider services, long comparisonId,
        CancellationToken token)
    {
        var db = services.GetRequiredService<ApplicationDbContext>();
        var item = await db.FaceAccessPolicyComparisons.AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == comparisonId, token);
        if (item is null || await db.FaceAccessDecisions.AnyAsync(x =>
                x.FaceAccessPolicyComparisonId == item.Id ||
                x.FaceRecognitionEventId == item.FaceRecognitionEventId, token))
            return;

        var combined = Combine(item.LegacyDecision, item.EnterpriseDecision,
            item.LegacyReasonCode, item.EnterpriseReasonCode, item.MappingStatus);
        var fingerprint = PolicyFingerprint.Create(
            "face-access-decision", item.FaceRecognitionEventId, item.Id,
            item.OccurredAtUtc, item.LegacyDecision, item.LegacyReasonCode,
            item.LegacyInputFingerprint, item.EnterpriseDecision,
            item.EnterpriseReasonCode, item.EnterpriseInputFingerprint,
            item.MappingStatus, item.EvaluationVersion, combined.Decision, combined.ReasonCode);
        var snapshot = JsonSerializer.Serialize(new {
            schemaVersion = 1,
            recognitionEventId = item.FaceRecognitionEventId,
            comparisonId = item.Id,
            occurredAtUtc = item.OccurredAtUtc,
            mapping = new {
                item.FaceCameraConfigurationId, item.LaneId, item.GateId, item.AccessPointId,
                item.MappingStatus
            },
            legacy = new {
                item.LegacyDecision, item.LegacyReasonCode, item.LegacyPermissionId,
                inputFingerprint = item.LegacyInputFingerprint
            },
            enterprise = new {
                item.EnterpriseDecision, item.EnterpriseReasonCode,
                item.EnterprisePolicyVersionId, item.EnterpriseRuleId,
                item.EnterpriseScheduleId, inputFingerprint = item.EnterpriseInputFingerprint
            },
            decision = new { combined.Decision, combined.ReasonCode },
            item.ScheduleTimeZoneId,
            item.EvaluationVersion
        });
        var now = DateTime.UtcNow;
        db.FaceAccessDecisions.Add(new FaceAccessDecision {
            FaceRecognitionEventId = item.FaceRecognitionEventId,
            FaceAccessPolicyComparisonId = item.Id,
            EmployeeId = item.EmployeeId, CameraId = item.CameraId,
            LaneId = item.LaneId, GateId = item.GateId, AccessPointId = item.AccessPointId,
            OccurredAtUtc = item.OccurredAtUtc, DecidedAtUtc = now,
            Decision = combined.Decision, ReasonCode = combined.ReasonCode,
            LegacyDecision = item.LegacyDecision, LegacyReasonCode = item.LegacyReasonCode,
            EnterpriseDecision = item.EnterpriseDecision,
            EnterpriseReasonCode = item.EnterpriseReasonCode,
            LegacyPermissionId = item.LegacyPermissionId,
            EnterprisePolicyVersionId = item.EnterprisePolicyVersionId,
            EnterpriseRuleId = item.EnterpriseRuleId,
            EnterpriseScheduleId = item.EnterpriseScheduleId,
            EvaluationVersion = item.EvaluationVersion,
            ScheduleTimeZoneId = item.ScheduleTimeZoneId,
            InputFingerprint = fingerprint, PolicySnapshotJson = snapshot,
            CreatedAtUtc = now
        });
        try { await db.SaveChangesAsync(token); }
        catch (DbUpdateException)
        {
            db.ChangeTracker.Clear();
            if (!await db.FaceAccessDecisions.AsNoTracking().AnyAsync(x =>
                    x.FaceAccessPolicyComparisonId == item.Id ||
                    x.FaceRecognitionEventId == item.FaceRecognitionEventId, token))
                throw;
        }
    }
}
