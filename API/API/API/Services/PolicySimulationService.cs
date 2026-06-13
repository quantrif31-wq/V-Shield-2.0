using API.Data;
using API.Models;
using API.Services.AI;
using Microsoft.EntityFrameworkCore;

namespace API.Services;

public interface IPolicySimulationService
{
    /// <summary>
    /// Phân tích tác động của policy version trước khi kích hoạt.
    /// </summary>
    Task<AiRecommendationResult> SimulatePolicyAsync(int policyVersionId, int? requestedByUserId);

    /// <summary>
    /// Giải thích policy version bằng ngôn ngữ tự nhiên.
    /// </summary>
    Task<AiRecommendationResult> ExplainPolicyAsync(int policyVersionId, int? requestedByUserId);
}

public class PolicySimulationService : IPolicySimulationService
{
    private readonly ApplicationDbContext _db;
    private readonly IAiRecommendationService _aiRec;

    public PolicySimulationService(ApplicationDbContext db, IAiRecommendationService aiRec)
    {
        _db = db;
        _aiRec = aiRec;
    }

    public async Task<AiRecommendationResult> SimulatePolicyAsync(int policyVersionId, int? requestedByUserId)
    {
        var version = await _db.AccessPolicyVersions.AsNoTracking()
            .FirstOrDefaultAsync(v => v.AccessPolicyVersionId == policyVersionId);
        if (version == null)
            throw new KeyNotFoundException($"Policy version {policyVersionId} not found.");

        // Lấy rules của version này
        var rules = await _db.AccessRules.AsNoTracking()
            .Where(r => r.AccessPolicyVersionId == policyVersionId)
            .ToListAsync();

        // Lấy active version hiện tại để so sánh
        var activeVersion = await _db.AccessPolicyVersions.AsNoTracking()
            .FirstOrDefaultAsync(v => v.Status == "Active");

        var activeRules = activeVersion != null
            ? await _db.AccessRules.AsNoTracking()
                .Where(r => r.AccessPolicyVersionId == activeVersion.AccessPolicyVersionId)
                .ToListAsync()
            : new List<AccessRule>();

        // Đếm số user bị ảnh hưởng
        var totalEmployees = await _db.Employees.CountAsync();
        var affectedEmployeeIds = rules
            .Where(r => r.SubjectType == "Employee" && r.SubjectId.HasValue)
            .Select(r => r.SubjectId!.Value)
            .Distinct()
            .Count();

        // Lấy zones bị ảnh hưởng
        var zoneIds = rules.Where(r => r.SecurityZoneId.HasValue)
            .Select(r => r.SecurityZoneId!.Value)
            .Distinct()
            .ToList();
        var zones = zoneIds.Count > 0
            ? await _db.SecurityZones.AsNoTracking()
                .Where(z => zoneIds.Contains(z.SecurityZoneId))
                .Select(z => z.Name)
                .ToListAsync()
            : new List<string>();

        // Xác định conflict
        var conflicts = new List<string>();
        foreach (var rule in rules)
        {
            var conflicting = activeRules.Any(ar =>
                ar.AccessRuleId != rule.AccessRuleId &&
                ar.SubjectType == rule.SubjectType &&
                ar.SubjectId == rule.SubjectId &&
                ar.SecurityZoneId == rule.SecurityZoneId &&
                ar.AllowAccess != rule.AllowAccess);
            if (conflicting)
                conflicts.Add($"Rule {rule.AccessRuleId}: subject {rule.SubjectType}:{rule.SubjectId} zone {rule.SecurityZoneId} - allow/deny conflict");
        }

        var inputData = new Dictionary<string, string>
        {
            ["policy_name"] = version.Name,
            ["policy_status"] = version.Status,
            ["change_summary"] = version.ChangeSummary ?? "N/A",
            ["rule_count"] = rules.Count.ToString(),
            ["active_rule_count"] = activeRules.Count.ToString(),
            ["affected_zones"] = zones.Count > 0
                ? string.Join(", ", zones)
                : "All zones / Khong xac dinh",
            ["affected_users"] = affectedEmployeeIds > 0
                ? $"{affectedEmployeeIds} employees + all {totalEmployees} employees via group rules"
                : $"Tat ca {totalEmployees} nhan vien",
            ["conflicts"] = conflicts.Count > 0
                ? string.Join("; ", conflicts.Take(10))
                : "Khong phat hien conflict",
            ["total_employees"] = totalEmployees.ToString(),
            ["created_by"] = version.CreatedByUserId?.ToString() ?? "N/A"
        };

        return await _aiRec.AnalyzeAsync(
            "policy", "policy", policyVersionId.ToString(),
            "policy-simulation",
            inputData,
            requestedByUserId);
    }

    public async Task<AiRecommendationResult> ExplainPolicyAsync(int policyVersionId, int? requestedByUserId)
    {
        var version = await _db.AccessPolicyVersions.AsNoTracking()
            .FirstOrDefaultAsync(v => v.AccessPolicyVersionId == policyVersionId);
        if (version == null)
            throw new KeyNotFoundException($"Policy version {policyVersionId} not found.");

        var rules = await _db.AccessRules.AsNoTracking()
            .Include(r => r.Schedule)
            .Include(r => r.AccessLevel)
            .Where(r => r.AccessPolicyVersionId == policyVersionId)
            .ToListAsync();

        var allowRules = rules.Count(r => r.AllowAccess);
        var denyRules = rules.Count(r => !r.AllowAccess);
        var scheduledRules = rules.Count(r => r.AccessScheduleId.HasValue);

        var inputData = new Dictionary<string, string>
        {
            ["policy_name"] = version.Name,
            ["policy_status"] = version.Status,
            ["change_summary"] = version.ChangeSummary ?? "N/A",
            ["allow_rules"] = allowRules.ToString(),
            ["deny_rules"] = denyRules.ToString(),
            ["scheduled_rules"] = scheduledRules.ToString(),
            ["total_rules"] = rules.Count.ToString(),
            ["created_by"] = version.CreatedByUserId?.ToString() ?? "N/A",
            ["created_at"] = version.CreatedAtUtc.ToString("yyyy-MM-dd HH:mm UTC"),
            ["submitted_at"] = version.SubmittedAtUtc?.ToString("yyyy-MM-dd HH:mm UTC") ?? "Chua submit",
            ["approved_at"] = version.ApprovedAtUtc?.ToString("yyyy-MM-dd HH:mm UTC") ?? "Chua duyet",
            ["activated_at"] = version.ActivatedAtUtc?.ToString("yyyy-MM-dd HH:mm UTC") ?? "Chua kich hoat",
            ["retired_at"] = version.RetiredAtUtc?.ToString("yyyy-MM-dd HH:mm UTC") ?? "Chua retired"
        };

        return await _aiRec.AnalyzeAsync(
            "policy", "policy", policyVersionId.ToString(),
            "policy-explanation",
            inputData,
            requestedByUserId);
    }
}
