using API.Data;
using API.Models;
using API.Services.AI;
using Microsoft.EntityFrameworkCore;

namespace API.Services;

public interface IEvidenceAiAssistantService
{
    Task<AiRecommendationResult> AnalyzeEvidenceCaseAsync(long evidenceItemId, int? requestedByUserId);
    Task<AiRecommendationResult> AnalyzeExportRequestAsync(long exportRequestId, int? requestedByUserId);
}

public class EvidenceAiAssistantService : IEvidenceAiAssistantService
{
    private readonly ApplicationDbContext _db;
    private readonly IAiRecommendationService _aiRec;

    public EvidenceAiAssistantService(ApplicationDbContext db, IAiRecommendationService aiRec)
    {
        _db = db;
        _aiRec = aiRec;
    }

    public async Task<AiRecommendationResult> AnalyzeEvidenceCaseAsync(long evidenceItemId, int? requestedByUserId)
    {
        var item = await _db.EvidenceItems.AsNoTracking()
            .FirstOrDefaultAsync(e => e.EvidenceItemId == evidenceItemId);
        if (item == null)
            throw new KeyNotFoundException($"Evidence item {evidenceItemId} not found.");

        var custodyEntries = await _db.ChainOfCustodyEntries.AsNoTracking()
            .Where(c => c.EvidenceItemId == evidenceItemId)
            .OrderByDescending(c => c.CreatedAtUtc)
            .Take(10)
            .ToListAsync();

        var legalHolds = await _db.LegalHolds.AsNoTracking()
            .Where(l => l.EvidenceItemId == evidenceItemId && l.Status == "Active")
            .ToListAsync();

        var accessLogs = await _db.EvidenceAccessLogs.AsNoTracking()
            .Where(l => l.EvidenceItemId == evidenceItemId)
            .OrderByDescending(l => l.AccessedAtUtc)
            .Take(10)
            .ToListAsync();

        string Truncate(string? value, int maxLen) =>
            string.IsNullOrEmpty(value) ? "N/A" : (value.Length > maxLen ? value[..maxLen] : value);

        var inputData = new Dictionary<string, string>
        {
            ["evidence_type"] = item.EvidenceType,
            ["source"] = $"{item.SourceType}: {Truncate(item.SourceReference, 100)}",
            ["timestamp"] = item.CreatedAtUtc.ToString("yyyy-MM-dd HH:mm:ss UTC"),
            ["custody_info"] = custodyEntries.Any()
                ? string.Join("; ", custodyEntries.Select(c => $"{c.Action}: {Truncate(c.Note, 80)}"))
                : "Khong co custody log",
            ["legal_hold_info"] = legalHolds.Any()
                ? string.Join("; ", legalHolds.Select(l => $"{l.Reason} (tu {l.AppliedAtUtc:yyyy-MM-dd})"))
                : "Khong co legal hold",
            ["incident_info"] = item.IncidentId.HasValue
                ? $"IncidentId: {item.IncidentId}"
                : "Khong lien ket voi incident",
            ["hash_verification"] = $"{item.LastHashVerificationStatus} (lan cuoi: {item.CurrentHashVerifiedAtUtc?.ToString("yyyy-MM-dd HH:mm") ?? "chua verify"})",
            ["privacy_label"] = item.PrivacyLabel,
            ["retention"] = $"{item.RetentionCategory} - Immutable: {item.IsImmutable} - LegalHold: {item.IsLegalHold}",
            ["access_history"] = accessLogs.Any()
                ? string.Join("; ", accessLogs.Select(l => $"{l.AccessType} by User {l.UserId}"))
                : "Chua co ai truy cap"
        };

        return await _aiRec.AnalyzeAsync(
            "evidence", "evidence_item", evidenceItemId.ToString(),
            "evidence-analysis", inputData, requestedByUserId);
    }

    public async Task<AiRecommendationResult> AnalyzeExportRequestAsync(long exportRequestId, int? requestedByUserId)
    {
        var export = await _db.EvidenceExportRequests.AsNoTracking()
            .FirstOrDefaultAsync(e => e.EvidenceExportRequestId == exportRequestId);
        if (export == null)
            throw new KeyNotFoundException($"Export request {exportRequestId} not found.");

        string Truncate(string? value, int maxLen) =>
            string.IsNullOrEmpty(value) ? "N/A" : (value.Length > maxLen ? value[..maxLen] : value);

        var itemInfo = "N/A";
        if (export.EvidenceItemId.HasValue)
        {
            var item = await _db.EvidenceItems.AsNoTracking()
                .FirstOrDefaultAsync(e => e.EvidenceItemId == export.EvidenceItemId);
            if (item != null)
                itemInfo = $"{item.EvidenceType} - {item.PrivacyLabel} - Hash: {Truncate(item.HashSha256, 20)}";
        }

        var inputData = new Dictionary<string, string>
        {
            ["evidence_type"] = "ExportRequest",
            ["source"] = $"Export #{exportRequestId}",
            ["timestamp"] = export.RequestedAtUtc.ToString("yyyy-MM-dd HH:mm:ss UTC"),
            ["custody_info"] = $"Trang thai: {export.Status}, Nguoi yeu cau: User {export.RequestedByUserId}",
            ["legal_hold_info"] = itemInfo,
            ["incident_info"] = $"Muc dich: {Truncate(export.Purpose, 200)}, Nguoi nhan: {export.Recipient}",
            ["watermark"] = export.Watermark ?? "Chua co watermark",
            ["export_hash"] = export.ExportHash ?? "Chua co hash"
        };

        return await _aiRec.AnalyzeAsync(
            "evidence", "export", exportRequestId.ToString(),
            "evidence-analysis", inputData, requestedByUserId);
    }
}
