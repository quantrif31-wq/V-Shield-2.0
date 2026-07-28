using System.Security.Cryptography;
using System.Text;
using API.Data;
using API.Models;
using Microsoft.EntityFrameworkCore;

namespace API.Services.AccessPolicyComparison;

public sealed record LegacyGateEvaluationInput(int EmployeeId, int GateId, DateTime OccurredAtUtc);
public sealed record EnterprisePolicyEvaluationInput(
    int EmployeeId, int AccessPointId, DateTime OccurredAtUtc, string? CredentialType);
public sealed record PolicyEvaluationResult(
    string Decision, string ReasonCode, string Fingerprint,
    int? PermissionId = null, int? PolicyVersionId = null, int? RuleId = null,
    int? ScheduleId = null, DateTime? EffectiveFromUtc = null, DateTime? EffectiveToUtc = null);

public interface ILegacyGateAccessEvaluator
{
    Task<PolicyEvaluationResult> EvaluateAsync(LegacyGateEvaluationInput input, CancellationToken token);
}

public interface IEnterpriseAccessPolicyEvaluator
{
    Task<PolicyEvaluationResult> EvaluateAsync(EnterprisePolicyEvaluationInput input, CancellationToken token);
}

public sealed class LegacyGateAccessEvaluator(ApplicationDbContext db) : ILegacyGateAccessEvaluator
{
    public async Task<PolicyEvaluationResult> EvaluateAsync(
        LegacyGateEvaluationInput input, CancellationToken token)
    {
        if (!await db.Employees.AsNoTracking().AnyAsync(x => x.EmployeeId == input.EmployeeId, token))
            return Result(PolicyEvaluationDecisions.Indeterminate, "LegacyEmployeeMissing", input, null);
        if (!await db.Gates.AsNoTracking().AnyAsync(x => x.GateId == input.GateId, token))
            return Result(PolicyEvaluationDecisions.Indeterminate, "LegacyGateMissing", input, null);
        var rows = await db.EmployeeAccessPermissions.AsNoTracking()
            .Where(x => x.EmployeeId == input.EmployeeId && x.GateId == input.GateId)
            .OrderBy(x => x.Id).ToListAsync(token);
        if (rows.Count == 0)
            return Result(PolicyEvaluationDecisions.NotConfigured, "LegacyPermissionMissing", input, null);
        if (rows.Count != 1)
            return Result(PolicyEvaluationDecisions.Indeterminate, "LegacyDuplicatePermissions", input, null);
        var row = rows[0];
        return Result(row.IsAllowed ? PolicyEvaluationDecisions.Allow : PolicyEvaluationDecisions.Deny,
            row.IsAllowed ? "LegacyAllowed" : "LegacyExplicitDenied", input, row);
    }

    private static PolicyEvaluationResult Result(string decision, string reason,
        LegacyGateEvaluationInput input, EmployeeAccessPermission? permission) =>
        new(decision, reason, PolicyFingerprint.Create(
            input.EmployeeId, input.GateId, permission?.Id, permission?.IsAllowed),
            PermissionId: permission?.Id);
}

public sealed class EnterpriseAccessPolicyEvaluator(
    ApplicationDbContext db, FaceAccessPolicyComparisonOptions options)
    : IEnterpriseAccessPolicyEvaluator
{
    public async Task<PolicyEvaluationResult> EvaluateAsync(
        EnterprisePolicyEvaluationInput input, CancellationToken token)
    {
        if (string.IsNullOrWhiteSpace(input.CredentialType))
            return Basic(PolicyEvaluationDecisions.Indeterminate,
                "EnterpriseMissingCredentialContext", input);

        var policyVersionId = await db.AccessPolicyVersions.AsNoTracking()
            .Where(x => x.Status == "Active")
            .OrderByDescending(x => x.ActivatedAtUtc ?? x.CreatedAtUtc)
            .Select(x => (int?)x.AccessPolicyVersionId).FirstOrDefaultAsync(token);
        var rules = await db.AccessRules.AsNoTracking().Include(x => x.Schedule)
            .Where(x => x.IsActive && x.SubjectType == "Employee")
            .Where(x => x.SubjectId == null || x.SubjectId == input.EmployeeId)
            .Where(x => x.AccessPointId == null || x.AccessPointId == input.AccessPointId)
            .Where(x => x.CredentialType == "Any" || x.CredentialType == input.CredentialType)
            .Where(x => !policyVersionId.HasValue || x.AccessPolicyVersionId == null ||
                        x.AccessPolicyVersionId == policyVersionId)
            .OrderByDescending(x => x.AccessPolicyVersionId == policyVersionId)
            .ThenBy(x => x.AllowAccess).ThenBy(x => x.AccessRuleId)
            .ToListAsync(token);
        var temporary = await db.TemporaryAccessGrants.AsNoTracking().AnyAsync(x =>
            !x.IsRevoked && x.SubjectType == "Employee" && x.SubjectId == input.EmployeeId &&
            x.ValidFromUtc <= input.OccurredAtUtc && x.ValidToUtc >= input.OccurredAtUtc &&
            (x.AccessPointId == null || x.AccessPointId == input.AccessPointId), token);
        if (rules.Count == 0)
        {
            if (temporary)
                return Basic(PolicyEvaluationDecisions.Allow,
                    "EnterpriseTemporaryGrant", input, policyVersionId);
            return Basic(PolicyEvaluationDecisions.NotConfigured,
                policyVersionId.HasValue ? "EnterpriseNoMatchingRule" : "EnterpriseNoActivePolicy", input,
                policyVersionId);
        }

        var effective = rules.Where(x =>
            (!x.ValidFromUtc.HasValue || x.ValidFromUtc <= input.OccurredAtUtc) &&
            (!x.ValidToUtc.HasValue || x.ValidToUtc >= input.OccurredAtUtc)).ToList();
        if (effective.Count == 0)
        {
            var reason = rules.All(x => x.ValidFromUtc > input.OccurredAtUtc)
                ? "EnterpriseNotYetEffective" : "EnterpriseExpired";
            return RuleResult(PolicyEvaluationDecisions.Deny, reason, input, policyVersionId, rules[0]);
        }

        var local = TimeZoneInfo.ConvertTimeFromUtc(
            DateTime.SpecifyKind(input.OccurredAtUtc, DateTimeKind.Utc), options.TimeZone);
        var scheduled = effective.Where(x => WithinSchedule(x.Schedule, local)).ToList();
        var deny = scheduled.FirstOrDefault(x => !x.AllowAccess);
        if (deny is not null)
            return RuleResult(PolicyEvaluationDecisions.Deny, "EnterpriseExplicitDenied",
                input, policyVersionId, deny);

        if (temporary)
            return Basic(PolicyEvaluationDecisions.Allow,
                "EnterpriseTemporaryGrant", input, policyVersionId);
        if (scheduled.Count == 0)
            return RuleResult(PolicyEvaluationDecisions.Deny, "EnterpriseOutsideSchedule",
                input, policyVersionId, effective[0]);
        var allow = scheduled.First();
        return RuleResult(PolicyEvaluationDecisions.Allow, "EnterpriseAllowed",
            input, policyVersionId, allow);
    }

    private static bool WithinSchedule(AccessSchedule? schedule, DateTime local)
    {
        if (schedule is null) return true;
        if (!schedule.IsActive) return false;
        var day = local.DayOfWeek.ToString()[..3];
        if (!schedule.DaysOfWeek.Split(',', StringSplitOptions.RemoveEmptyEntries |
            StringSplitOptions.TrimEntries).Any(x => x.Equals(day, StringComparison.OrdinalIgnoreCase)))
            return false;
        var time = local.TimeOfDay;
        return schedule.StartTime <= schedule.EndTime
            ? time >= schedule.StartTime && time <= schedule.EndTime
            : time >= schedule.StartTime || time <= schedule.EndTime;
    }

    private static PolicyEvaluationResult Basic(string decision, string reason,
        EnterprisePolicyEvaluationInput input, int? version = null) =>
        new(decision, reason, PolicyFingerprint.Create(
            input.EmployeeId, input.AccessPointId, version, input.CredentialType ?? "missing"),
            PolicyVersionId: version);

    private static PolicyEvaluationResult RuleResult(string decision, string reason,
        EnterprisePolicyEvaluationInput input, int? version, AccessRule rule) =>
        new(decision, reason, PolicyFingerprint.Create(input.EmployeeId, input.AccessPointId,
            version, rule.AccessRuleId, rule.AllowAccess, rule.ValidFromUtc, rule.ValidToUtc,
            rule.AccessScheduleId, input.OccurredAtUtc, input.CredentialType),
            PolicyVersionId: version, RuleId: rule.AccessRuleId,
            ScheduleId: rule.AccessScheduleId, EffectiveFromUtc: rule.ValidFromUtc,
            EffectiveToUtc: rule.ValidToUtc);
}

public static class PolicyFingerprint
{
    public static string Create(params object?[] values)
    {
        var canonical = string.Join("|", values.Select((value, index) =>
            $"{index}:{Convert.ToString(value, System.Globalization.CultureInfo.InvariantCulture) ?? "null"}"));
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(canonical))).ToLowerInvariant();
    }
}
