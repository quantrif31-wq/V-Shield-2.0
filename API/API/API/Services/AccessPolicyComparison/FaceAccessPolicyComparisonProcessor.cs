using API.Data;
using API.Models;
using API.Services.AccessCredentials;
using API.Services.FaceCredentialBindings;
using Microsoft.EntityFrameworkCore;

namespace API.Services.AccessPolicyComparison;

public sealed class FaceAccessPolicyComparisonOptions
{
    public const string SectionName = "FaceAccessPolicyComparison";
    public bool ProcessorEnabled { get; set; } = true;
    public int PollIntervalMilliseconds { get; set; } = 1000;
    public int BatchSize { get; set; } = 100;
    public int MaxParallelism { get; set; } = 2;
    public int EvaluationVersion { get; set; } = 1;
    public string TimeZoneId { get; set; } = "Asia/Ho_Chi_Minh";
    public TimeZoneInfo TimeZone { get; set; } = TimeZoneInfo.Utc;
}

public sealed record FacePolicyMapping(
    string Status, int? ConfigurationId, int? LaneId, int? GateId, int? AccessPointId);

public interface IFaceAccessPolicyComparisonProcessor
{
    Task RunCycleAsync(CancellationToken token);
    Task<object> HealthAsync(CancellationToken token);
}

public sealed class FaceAccessPolicyComparisonProcessor(
    IServiceScopeFactory scopeFactory,
    FaceAccessPolicyComparisonOptions options,
    ILogger<FaceAccessPolicyComparisonProcessor> logger)
    : BackgroundService, IFaceAccessPolicyComparisonProcessor
{
    private readonly SemaphoreSlim _gate = new(1, 1);

    public async Task RunCycleAsync(CancellationToken token)
    {
        if (!await _gate.WaitAsync(0, token)) return;
        try
        {
            using var scope = scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var ids = await db.FaceRecognitionEvents.AsNoTracking()
                .Where(x => !db.FaceAccessPolicyComparisons.Any(c => c.FaceRecognitionEventId == x.Id))
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
                    logger.LogWarning(ex, "Policy comparison failed for recognition event {EventId}", id);
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
            comparisonCount = await db.FaceAccessPolicyComparisons.CountAsync(token),
            pendingCount = await db.FaceRecognitionEvents.CountAsync(x =>
                !db.FaceAccessPolicyComparisons.Any(c => c.FaceRecognitionEventId == x.Id), token),
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

    private async Task ProcessOneAsync(IServiceProvider services, long id, CancellationToken token)
    {
        var db = services.GetRequiredService<ApplicationDbContext>();
        if (await db.FaceAccessPolicyComparisons.AnyAsync(x => x.FaceRecognitionEventId == id, token))
            return;
        var item = await db.FaceRecognitionEvents.AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == id, token);
        if (item is null) return;
        var mapping = await ResolveAsync(db, item, token);
        PolicyEvaluationResult legacy;
        PolicyEvaluationResult enterprise;
        if (item.EventType != "Recognized" || item.MatchStatus != FaceRecognitionMatchStatuses.Matched ||
            !item.EmployeeId.HasValue)
        {
            legacy = new(PolicyEvaluationDecisions.Indeterminate,
                "RecognitionNotTrusted", PolicyFingerprint.Create(item.Id, item.MatchStatus));
            enterprise = legacy with { Fingerprint = PolicyFingerprint.Create(item.Id, item.MatchStatus, "enterprise") };
        }
        else if (mapping.Status != FacePolicyMappingStatuses.Resolved)
        {
            legacy = new(PolicyEvaluationDecisions.Indeterminate,
                "MappingUnavailable", PolicyFingerprint.Create(item.Id, mapping.Status, "legacy"));
            enterprise = legacy with { Fingerprint = PolicyFingerprint.Create(item.Id, mapping.Status, "enterprise") };
        }
        else if (!await EmployeeWasActiveAsync(db, item.EmployeeId.Value, item.OccurredAtUtc, token))
        {
            legacy = new(PolicyEvaluationDecisions.Deny,
                "EmployeeInactive", PolicyFingerprint.Create(item.Id, item.EmployeeId, item.OccurredAtUtc, "legacy"));
            enterprise = new(PolicyEvaluationDecisions.Deny,
                "EmployeeInactive", PolicyFingerprint.Create(item.Id, item.EmployeeId, item.OccurredAtUtc, "enterprise"));
        }
        else
        {
            legacy = await services.GetRequiredService<ILegacyGateAccessEvaluator>()
                .EvaluateAsync(new(item.EmployeeId.Value, mapping.GateId!.Value, item.OccurredAtUtc), token);
            enterprise = await EvaluateEnterpriseAsync(services, item, mapping, token);
        }
        var result = Compare(legacy.Decision, enterprise.Decision, mapping.Status);
        db.FaceAccessPolicyComparisons.Add(new FaceAccessPolicyComparison {
            FaceRecognitionEventId = item.Id, EmployeeId = item.EmployeeId,
            CameraId = item.CameraId, FaceCameraConfigurationId = mapping.ConfigurationId,
            LaneId = mapping.LaneId, GateId = mapping.GateId, AccessPointId = mapping.AccessPointId,
            OccurredAtUtc = item.OccurredAtUtc, EvaluatedAtUtc = DateTime.UtcNow,
            LegacyDecision = legacy.Decision, LegacyReasonCode = legacy.ReasonCode,
            LegacyPermissionId = legacy.PermissionId, EnterpriseDecision = enterprise.Decision,
            EnterpriseReasonCode = enterprise.ReasonCode,
            EnterprisePolicyVersionId = enterprise.PolicyVersionId,
            EnterpriseRuleId = enterprise.RuleId, EnterpriseScheduleId = enterprise.ScheduleId,
            ComparisonResult = result, MappingStatus = mapping.Status,
            EvaluationVersion = options.EvaluationVersion,
            LegacyInputFingerprint = legacy.Fingerprint,
            EnterpriseInputFingerprint = enterprise.Fingerprint,
            ScheduleTimeZoneId = options.TimeZoneId
        });
        if (result is PolicyComparisonResults.LegacyAllowEnterpriseDeny or
            PolicyComparisonResults.LegacyDenyEnterpriseAllow)
        {
            db.SystemAuditLogs.Add(new SystemAuditLog {
                EventCategory = "ACCESS_POLICY_COMPARISON", Severity = "WARNING",
                ActionType = "POLICY_DISAGREEMENT", EntityName = "FaceRecognitionEvent",
                EntityId = item.Id.ToString(), IsSuccess = false
            });
        }
        try { await db.SaveChangesAsync(token); }
        catch (DbUpdateException)
        {
            db.ChangeTracker.Clear();
            if (!await db.FaceAccessPolicyComparisons.AsNoTracking()
                .AnyAsync(x => x.FaceRecognitionEventId == id, token))
                throw;
        }
    }

    private static async Task<bool> EmployeeWasActiveAsync(
        ApplicationDbContext db, int employeeId, DateTime occurredAtUtc, CancellationToken token)
    {
        var employee = await db.Employees.AsNoTracking()
            .SingleOrDefaultAsync(x => x.EmployeeId == employeeId, token);
        if (employee is null || employee.Status == false) return false;
        var lifecycle = await db.EmployeeLifecycleEvents.AsNoTracking()
            .Where(x => x.EmployeeId == employeeId && x.EffectiveAtUtc <= occurredAtUtc)
            .OrderByDescending(x => x.EffectiveAtUtc)
            .ThenByDescending(x => x.EmployeeLifecycleEventId)
            .Select(x => x.NewState)
            .FirstOrDefaultAsync(token);
        var state = lifecycle ?? employee.LifecycleStatus;
        return state is EmployeeLifecycleStates.Active or EmployeeLifecycleStates.ContractorActive;
    }

    private static async Task<PolicyEvaluationResult> EvaluateEnterpriseAsync(
        IServiceProvider services,
        FaceRecognitionEvent item,
        FacePolicyMapping mapping,
        CancellationToken token)
    {
        var bindingService = services.GetRequiredService<IFaceCredentialBindingService>();
        var evaluator = services.GetRequiredService<IEnterpriseAccessPolicyEvaluator>();
        var credentialResolver = services.GetRequiredService<IAccessCredentialContextResolver>();

        var input = new EnterprisePolicyEvaluationInput(
            item.EmployeeId!.Value,
            mapping.AccessPointId!.Value,
            item.OccurredAtUtc,
            null);

        var binding = await bindingService.ResolveAsync(item.EmployeeId.Value, item.OccurredAtUtc, token);
        if (binding.Context is null)
        {
            return new PolicyEvaluationResult(
                PolicyEvaluationDecisions.Indeterminate,
                binding.ReasonCode,
                PolicyFingerprint.Create(
                    "enterprise-face-binding",
                    item.EmployeeId.Value,
                    mapping.AccessPointId!.Value,
                    item.OccurredAtUtc,
                    binding.ReasonCode));
        }

        var credential = await credentialResolver.ResolveByCredentialIdAsync(
            binding.Context.AccessCredentialId,
            item.EmployeeId.Value,
            item.OccurredAtUtc,
            token);
        if (credential.Context is null)
        {
            return new PolicyEvaluationResult(
                PolicyEvaluationDecisions.Indeterminate,
                credential.ReasonCode,
                CreateEnterpriseBindingFingerprint(
                    item,
                    binding.Context,
                    null,
                    null,
                    null,
                    null,
                    credential.ReasonCode));
        }

        var result = await evaluator.EvaluateAsync(input, credential.Context, token);
        return result with
        {
            Fingerprint = CreateEnterpriseBindingFingerprint(
                item,
                binding.Context,
                credential.Context,
                result.PolicyVersionId,
                result.RuleId,
                result.ScheduleId,
                result.ReasonCode)
        };
    }

    private static async Task<FacePolicyMapping> ResolveAsync(
        ApplicationDbContext db, FaceRecognitionEvent item, CancellationToken token)
    {
        var config = await db.FaceCameraConfigurations.AsNoTracking()
            .Include(x => x.Lane).SingleOrDefaultAsync(x => x.Id == item.FaceCameraConfigurationId, token);
        if (config is null) return new(FacePolicyMappingStatuses.CameraUnmanaged, null, null, null, null);
        if (config.Lane is null) return new(FacePolicyMappingStatuses.LaneMissing, config.Id, null, null, null);
        if (!config.Lane.GateId.HasValue)
            return new(FacePolicyMappingStatuses.GateMissing, config.Id, config.LaneId, null, config.Lane.AccessPointId);
        if (!config.Lane.AccessPointId.HasValue)
            return new(FacePolicyMappingStatuses.AccessPointMissing, config.Id, config.LaneId, config.Lane.GateId, null);
        return new(FacePolicyMappingStatuses.Resolved, config.Id, config.LaneId,
            config.Lane.GateId, config.Lane.AccessPointId);
    }

    public static string Compare(string legacy, string enterprise, string mapping)
    {
        if (mapping != FacePolicyMappingStatuses.Resolved) return PolicyComparisonResults.MappingUnavailable;
        if (legacy == PolicyEvaluationDecisions.Error || enterprise == PolicyEvaluationDecisions.Error)
            return PolicyComparisonResults.EvaluationError;
        if (legacy == PolicyEvaluationDecisions.Indeterminate) return PolicyComparisonResults.LegacyIndeterminate;
        if (enterprise == PolicyEvaluationDecisions.Indeterminate) return PolicyComparisonResults.EnterpriseIndeterminate;
        if (legacy == PolicyEvaluationDecisions.Allow && enterprise == PolicyEvaluationDecisions.Allow)
            return PolicyComparisonResults.AgreeAllow;
        if (legacy == PolicyEvaluationDecisions.Deny && enterprise == PolicyEvaluationDecisions.Deny)
            return PolicyComparisonResults.AgreeDeny;
        if (legacy == PolicyEvaluationDecisions.Allow && enterprise == PolicyEvaluationDecisions.Deny)
            return PolicyComparisonResults.LegacyAllowEnterpriseDeny;
        if (legacy == PolicyEvaluationDecisions.Deny && enterprise == PolicyEvaluationDecisions.Allow)
            return PolicyComparisonResults.LegacyDenyEnterpriseAllow;
        if (legacy == PolicyEvaluationDecisions.NotConfigured && enterprise == PolicyEvaluationDecisions.NotConfigured)
            return PolicyComparisonResults.BothNotConfigured;
        if (enterprise == PolicyEvaluationDecisions.NotConfigured)
            return PolicyComparisonResults.LegacyConfiguredEnterpriseMissing;
        return PolicyComparisonResults.EnterpriseConfiguredLegacyMissing;
    }

    private static string CreateEnterpriseBindingFingerprint(
        FaceRecognitionEvent item,
        EmployeeFaceCredentialBindingContext binding,
        AccessCredentialContext? credential,
        int? policyVersionId,
        int? ruleId,
        int? scheduleId,
        string reasonCode)
    {
        return PolicyFingerprint.Create(
            "enterprise-face-binding",
            binding.BindingId,
            binding.ActivatedAtUtc,
            binding.AccessCredentialId,
            credential?.CredentialType ?? AccessCredentialTypes.FaceBiometric,
            credential?.StoredStatus,
            credential?.EffectiveStatus,
            credential?.EffectiveFromUtc,
            credential?.ExpiresAtUtc,
            policyVersionId,
            ruleId,
            scheduleId,
            item.OccurredAtUtc,
            reasonCode);
    }
}
