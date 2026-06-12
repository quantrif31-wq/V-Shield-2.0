using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using API.Data;
using API.Middleware;
using API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace API.Controllers;

[ApiController]
[Route("api/enterprise/evidence")]
[Authorize(Roles = "Admin,BaoVe")]
public class EnterpriseEvidenceController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly IConfiguration _configuration;

    public EnterpriseEvidenceController(ApplicationDbContext context, IConfiguration configuration)
    {
        _context = context;
        _configuration = configuration;
    }

    [HttpGet("overview")]
    public async Task<IActionResult> GetOverview()
    {
        return Ok(new
        {
            EvidenceItems = await _context.EvidenceItems.CountAsync(),
            Collections = await _context.EvidenceCollections.CountAsync(),
            AccessLogs = await _context.EvidenceAccessLogs.CountAsync(),
            PendingExports = await _context.EvidenceExportRequests.CountAsync(request => request.Status == "PendingApproval"),
            ActiveLegalHolds = await _context.LegalHolds.CountAsync(hold => hold.Status == "Active"),
            PendingRedactions = await _context.RedactionRequests.CountAsync(request => request.Status == "PendingApproval"),
            RetentionPolicies = await _context.RetentionPolicies.CountAsync(policy => policy.IsActive),
            ComplianceReports = await _context.ComplianceReportRuns.CountAsync()
        });
    }

    [HttpPost("retention-policies")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> CreateRetentionPolicy([FromBody] RetentionPolicyRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            return BadRequest(new { message = "Name is required." });
        if (request.RetentionDays <= 0)
            return BadRequest(new { message = "RetentionDays must be positive." });

        var policy = new RetentionPolicy
        {
            Name = request.Name.Trim(),
            EvidenceType = string.IsNullOrWhiteSpace(request.EvidenceType) ? "Any" : request.EvidenceType.Trim(),
            RetentionCategory = string.IsNullOrWhiteSpace(request.RetentionCategory) ? "Default" : request.RetentionCategory.Trim(),
            RetentionDays = request.RetentionDays,
            PurgeMode = string.IsNullOrWhiteSpace(request.PurgeMode) ? "ReviewRequired" : request.PurgeMode.Trim(),
            IsActive = request.IsActive
        };

        _context.RetentionPolicies.Add(policy);
        await _context.SaveChangesAsync();
        return Ok(policy);
    }

    [HttpPost("items")]
    public async Task<IActionResult> CreateEvidenceItem([FromBody] EvidenceItemRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.StorageReference))
            return BadRequest(new { message = "StorageReference is required." });

        var hash = string.IsNullOrWhiteSpace(request.HashSha256)
            ? ComputeHash($"{request.StorageReference}|{request.SourceReference}|{DateTime.UtcNow:O}")
            : request.HashSha256.Trim();

        var item = new EvidenceItem
        {
            EvidenceType = string.IsNullOrWhiteSpace(request.EvidenceType) ? "Document" : request.EvidenceType.Trim(),
            SourceType = string.IsNullOrWhiteSpace(request.SourceType) ? "Manual" : request.SourceType.Trim(),
            SourceReference = request.SourceReference?.Trim(),
            SecurityEventId = request.SecurityEventId,
            AlarmId = request.AlarmId,
            IncidentId = request.IncidentId,
            StorageReference = request.StorageReference.Trim(),
            HashSha256 = hash,
            PrivacyLabel = string.IsNullOrWhiteSpace(request.PrivacyLabel) ? "Internal" : request.PrivacyLabel.Trim(),
            RetentionCategory = string.IsNullOrWhiteSpace(request.RetentionCategory) ? "Default" : request.RetentionCategory.Trim(),
            SiteId = request.SiteId,
            IsImmutable = request.IsImmutable,
            CreatedByUserId = GetCurrentUserId()
        };

        _context.EvidenceItems.Add(item);
        await _context.SaveChangesAsync();

        _context.ChainOfCustodyEntries.Add(new ChainOfCustodyEntry
        {
            EvidenceItemId = item.EvidenceItemId,
            Action = "Registered",
            ActorUserId = GetCurrentUserId(),
            ToCustodian = "V-Shield Evidence Repository",
            HashAfter = item.HashSha256,
            Note = "Evidence item registered."
        });
        await _context.SaveChangesAsync();

        return Ok(item);
    }

    [HttpGet("items/{itemId:long}")]
    public async Task<IActionResult> GetEvidenceItem(long itemId, [FromQuery] string? purpose)
    {
        var item = await _context.EvidenceItems.FindAsync(itemId);
        if (item == null)
            return NotFound(new { message = "Evidence item not found." });

        _context.EvidenceAccessLogs.Add(new EvidenceAccessLog
        {
            EvidenceItemId = itemId,
            UserId = GetCurrentUserId(),
            AccessType = "Read",
            Purpose = string.IsNullOrWhiteSpace(purpose) ? "Operator review" : purpose.Trim()
        });
        await _context.SaveChangesAsync();

        return Ok(item);
    }

    [HttpPost("items/{itemId:long}/verify-hash")]
    public async Task<IActionResult> VerifyEvidenceHash(long itemId, [FromBody] EvidenceHashVerificationRequest request)
    {
        var item = await _context.EvidenceItems.FindAsync(itemId);
        if (item == null)
            return NotFound(new { message = "Evidence item not found." });
        if (string.IsNullOrWhiteSpace(request.ObservedHashSha256))
            return BadRequest(new { message = "ObservedHashSha256 is required." });

        var observed = request.ObservedHashSha256.Trim().ToLowerInvariant();
        var expected = item.HashSha256.Trim().ToLowerInvariant();
        var matched = string.Equals(observed, expected, StringComparison.OrdinalIgnoreCase);

        item.LastHashVerificationStatus = matched ? "Matched" : "Mismatch";
        item.CurrentHashVerifiedAtUtc = DateTime.UtcNow;

        _context.EvidenceAccessLogs.Add(new EvidenceAccessLog
        {
            EvidenceItemId = itemId,
            UserId = GetCurrentUserId(),
            AccessType = "HashVerification",
            Purpose = request.Purpose?.Trim() ?? "Integrity check"
        });
        _context.ChainOfCustodyEntries.Add(new ChainOfCustodyEntry
        {
            EvidenceItemId = itemId,
            Action = matched ? "HashVerified" : "HashMismatch",
            ActorUserId = GetCurrentUserId(),
            HashBefore = item.HashSha256,
            HashAfter = observed,
            Note = request.Purpose?.Trim()
        });

        await _context.SaveChangesAsync();
        return Ok(new { item.EvidenceItemId, matched, expectedHash = item.HashSha256, observedHash = observed });
    }

    [HttpPost("retention/dry-run")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> RunRetentionDryRun([FromBody] RetentionDryRunRequest request)
    {
        var now = request.AsOfUtc ?? DateTime.UtcNow;
        var policies = await _context.RetentionPolicies
            .Where(policy => policy.IsActive)
            .ToListAsync();

        var candidates = new List<object>();
        foreach (var policy in policies)
        {
            var cutoff = now.AddDays(-policy.RetentionDays);
            var query = _context.EvidenceItems
                .Where(item =>
                    item.PurgedAtUtc == null &&
                    !item.IsLegalHold &&
                    !item.IsImmutable &&
                    item.CreatedAtUtc <= cutoff);

            if (!string.Equals(policy.EvidenceType, "Any", StringComparison.OrdinalIgnoreCase))
                query = query.Where(item => item.EvidenceType == policy.EvidenceType);
            if (!string.Equals(policy.RetentionCategory, "Any", StringComparison.OrdinalIgnoreCase))
                query = query.Where(item => item.RetentionCategory == policy.RetentionCategory);

            var items = await query
                .Take(request.Limit <= 0 ? 50 : Math.Min(request.Limit, 500))
                .Select(item => new
                {
                    item.EvidenceItemId,
                    item.EvidenceType,
                    item.RetentionCategory,
                    item.CreatedAtUtc,
                    Policy = policy.Name,
                    policy.RetentionDays
                })
                .ToListAsync();

            candidates.AddRange(items);
        }

        return Ok(new { asOfUtc = now, candidates });
    }

    [HttpPost("retention/purge")]
    [Authorize(Roles = "Admin")]
    [RequireStepUp(PrivilegedActions.EvidenceRetentionPurge)]
    public async Task<IActionResult> PurgeEvidence([FromBody] EvidencePurgeRequest request)
    {
        if (request.EvidenceItemIds.Count == 0)
            return BadRequest(new { message = "EvidenceItemIds are required." });
        if (string.IsNullOrWhiteSpace(request.Reason))
            return BadRequest(new { message = "Reason is required." });

        var items = await _context.EvidenceItems
            .Where(item => request.EvidenceItemIds.Contains(item.EvidenceItemId))
            .ToListAsync();

        var purged = new List<long>();
        var blocked = new List<object>();
        foreach (var item in items)
        {
            if (item.IsLegalHold || item.IsImmutable)
            {
                blocked.Add(new { item.EvidenceItemId, item.IsLegalHold, item.IsImmutable });
                continue;
            }

            item.PurgedAtUtc = DateTime.UtcNow;
            item.PurgedByUserId = GetCurrentUserId();
            item.PurgeReason = request.Reason.Trim();
            purged.Add(item.EvidenceItemId);
            _context.ChainOfCustodyEntries.Add(new ChainOfCustodyEntry
            {
                EvidenceItemId = item.EvidenceItemId,
                Action = "Purged",
                ActorUserId = GetCurrentUserId(),
                HashBefore = item.HashSha256,
                Note = request.Reason.Trim()
            });
        }

        await _context.SaveChangesAsync();
        return Ok(new { purged, blocked });
    }

    [HttpPost("collections")]
    public async Task<IActionResult> CreateCollection([FromBody] EvidenceCollectionRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            return BadRequest(new { message = "Name is required." });

        var collection = new EvidenceCollection
        {
            Name = request.Name.Trim(),
            Purpose = string.IsNullOrWhiteSpace(request.Purpose) ? "Investigation" : request.Purpose.Trim(),
            IncidentId = request.IncidentId,
            Status = "Open",
            CreatedByUserId = GetCurrentUserId()
        };

        _context.EvidenceCollections.Add(collection);
        await _context.SaveChangesAsync();
        return Ok(collection);
    }

    [HttpPost("collections/{collectionId:long}/items")]
    public async Task<IActionResult> AddItemToCollection(long collectionId, [FromBody] EvidenceCollectionItemRequest request)
    {
        var collection = await _context.EvidenceCollections.FindAsync(collectionId);
        if (collection == null)
            return NotFound(new { message = "Collection not found." });
        var item = await _context.EvidenceItems.FindAsync(request.EvidenceItemId);
        if (item == null)
            return BadRequest(new { message = "Evidence item not found." });

        var exists = await _context.EvidenceCollectionItems
            .AnyAsync(link => link.EvidenceCollectionId == collectionId && link.EvidenceItemId == request.EvidenceItemId);
        if (!exists)
        {
            _context.EvidenceCollectionItems.Add(new EvidenceCollectionItem
            {
                EvidenceCollectionId = collectionId,
                EvidenceItemId = request.EvidenceItemId
            });
        }

        collection.BundleHash = await ComputeCollectionHashAsync(collectionId, item.HashSha256);
        await _context.SaveChangesAsync();
        return Ok(collection);
    }

    [HttpPost("items/{itemId:long}/custody")]
    public async Task<IActionResult> AddCustodyEntry(long itemId, [FromBody] ChainOfCustodyRequest request)
    {
        var item = await _context.EvidenceItems.FindAsync(itemId);
        if (item == null)
            return NotFound(new { message = "Evidence item not found." });

        var entry = new ChainOfCustodyEntry
        {
            EvidenceItemId = itemId,
            Action = string.IsNullOrWhiteSpace(request.Action) ? "Transferred" : request.Action.Trim(),
            ActorUserId = GetCurrentUserId(),
            FromCustodian = request.FromCustodian?.Trim(),
            ToCustodian = request.ToCustodian?.Trim(),
            HashBefore = request.HashBefore?.Trim(),
            HashAfter = string.IsNullOrWhiteSpace(request.HashAfter) ? item.HashSha256 : request.HashAfter.Trim(),
            Note = request.Note?.Trim()
        };

        _context.ChainOfCustodyEntries.Add(entry);
        await _context.SaveChangesAsync();
        return Ok(entry);
    }

    [HttpPost("legal-holds")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> ApplyLegalHold([FromBody] LegalHoldRequest request)
    {
        if (request.EvidenceItemId == null && request.EvidenceCollectionId == null)
            return BadRequest(new { message = "EvidenceItemId or EvidenceCollectionId is required." });

        var hold = new LegalHold
        {
            EvidenceItemId = request.EvidenceItemId,
            EvidenceCollectionId = request.EvidenceCollectionId,
            Reason = string.IsNullOrWhiteSpace(request.Reason) ? "Legal hold" : request.Reason.Trim(),
            Status = "Active",
            AppliedByUserId = GetCurrentUserId()
        };

        _context.LegalHolds.Add(hold);
        await MarkLegalHoldAsync(request.EvidenceItemId, request.EvidenceCollectionId, true);
        await _context.SaveChangesAsync();
        return Ok(hold);
    }

    [HttpPatch("legal-holds/{legalHoldId:long}/release")]
    [Authorize(Roles = "Admin")]
    [RequireStepUp(PrivilegedActions.EvidenceLegalHoldRelease)]
    public async Task<IActionResult> ReleaseLegalHold(long legalHoldId, [FromBody] CloseRequest request)
    {
        var hold = await _context.LegalHolds.FindAsync(legalHoldId);
        if (hold == null)
            return NotFound(new { message = "Legal hold not found." });

        hold.Status = "Released";
        hold.ReleasedAtUtc = DateTime.UtcNow;
        hold.ReleasedByUserId = GetCurrentUserId();
        await MarkLegalHoldAsync(hold.EvidenceItemId, hold.EvidenceCollectionId, false);
        await _context.SaveChangesAsync();
        return Ok(hold);
    }

    [HttpPost("export-requests")]
    public async Task<IActionResult> RequestExport([FromBody] EvidenceExportRequestRequest request)
    {
        if (request.EvidenceItemId == null && request.EvidenceCollectionId == null)
            return BadRequest(new { message = "EvidenceItemId or EvidenceCollectionId is required." });
        if (string.IsNullOrWhiteSpace(request.Purpose) || string.IsNullOrWhiteSpace(request.Recipient))
            return BadRequest(new { message = "Purpose and Recipient are required." });

        var export = new EvidenceExportRequest
        {
            EvidenceItemId = request.EvidenceItemId,
            EvidenceCollectionId = request.EvidenceCollectionId,
            Purpose = request.Purpose.Trim(),
            Recipient = request.Recipient.Trim(),
            Status = "PendingApproval",
            RequestedByUserId = GetCurrentUserId()
        };

        _context.EvidenceExportRequests.Add(export);
        await _context.SaveChangesAsync();
        return Ok(export);
    }

    [HttpPatch("export-requests/{exportRequestId:long}/approve")]
    [Authorize(Roles = "Admin")]
    [RequireStepUp(PrivilegedActions.EvidenceExportApproval)]
    public async Task<IActionResult> ApproveExport(long exportRequestId, [FromBody] ExportApprovalRequest request)
    {
        var export = await _context.EvidenceExportRequests.FindAsync(exportRequestId);
        if (export == null)
            return NotFound(new { message = "Export request not found." });

        if (export.EvidenceItemId.HasValue)
        {
            var item = await _context.EvidenceItems.FindAsync(export.EvidenceItemId.Value);
            if (item == null)
                return BadRequest(new { message = "Evidence item not found." });
            if (item.PurgedAtUtc.HasValue)
                return BadRequest(new { message = "Purged evidence cannot be exported." });
            if (item.LastHashVerificationStatus == "Mismatch")
                return BadRequest(new { message = "Evidence hash mismatch blocks export approval." });
        }

        export.Status = "Approved";
        export.ApprovedAtUtc = DateTime.UtcNow;
        export.ApprovedByUserId = GetCurrentUserId();
        export.Watermark = string.IsNullOrWhiteSpace(request.Watermark) ? $"V-Shield export {DateTime.UtcNow:O}" : request.Watermark.Trim();
        export.ExportHash = ComputeHash($"{export.EvidenceItemId}|{export.EvidenceCollectionId}|{export.Purpose}|{export.Recipient}|{export.Watermark}");
        export.SignatureReference = string.IsNullOrWhiteSpace(request.SignatureReference)
            ? $"hmac-sha256:{ComputeHmac(GetEvidenceExportSigningKey(), export.ExportHash)}"
            : request.SignatureReference.Trim();

        if (export.EvidenceItemId.HasValue)
        {
            _context.EvidenceAccessLogs.Add(new EvidenceAccessLog
            {
                EvidenceItemId = export.EvidenceItemId.Value,
                UserId = GetCurrentUserId(),
                AccessType = "ExportApproved",
                Purpose = export.Purpose
            });
        }

        await _context.SaveChangesAsync();
        return Ok(export);
    }

    [HttpPost("redaction-requests")]
    public async Task<IActionResult> RequestRedaction([FromBody] RedactionRequestRequest request)
    {
        if (!await _context.EvidenceItems.AnyAsync(item => item.EvidenceItemId == request.EvidenceItemId))
            return BadRequest(new { message = "Evidence item not found." });
        if (string.IsNullOrWhiteSpace(request.Reason))
            return BadRequest(new { message = "Reason is required." });

        var redaction = new RedactionRequest
        {
            EvidenceItemId = request.EvidenceItemId,
            Reason = request.Reason.Trim(),
            PrivacyLabel = string.IsNullOrWhiteSpace(request.PrivacyLabel) ? "PersonalData" : request.PrivacyLabel.Trim(),
            Status = "PendingApproval",
            RequestedByUserId = GetCurrentUserId()
        };

        _context.RedactionRequests.Add(redaction);
        await _context.SaveChangesAsync();
        return Ok(redaction);
    }

    [HttpPatch("redaction-requests/{redactionRequestId:long}/approve")]
    [Authorize(Roles = "Admin")]
    [RequireStepUp(PrivilegedActions.EvidenceRedactionApproval)]
    public async Task<IActionResult> ApproveRedaction(long redactionRequestId)
    {
        var redaction = await _context.RedactionRequests.FindAsync(redactionRequestId);
        if (redaction == null)
            return NotFound(new { message = "Redaction request not found." });

        redaction.Status = "Approved";
        redaction.ApprovedAtUtc = DateTime.UtcNow;
        redaction.ApprovedByUserId = GetCurrentUserId();
        await _context.SaveChangesAsync();
        return Ok(redaction);
    }

    [HttpPatch("redaction-requests/{redactionRequestId:long}/perform")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> PerformRedaction(long redactionRequestId, [FromBody] RedactionPerformRequest request)
    {
        var redaction = await _context.RedactionRequests.FindAsync(redactionRequestId);
        if (redaction == null)
            return NotFound(new { message = "Redaction request not found." });
        if (string.IsNullOrWhiteSpace(request.RedactedStorageReference))
            return BadRequest(new { message = "RedactedStorageReference is required." });

        redaction.Status = "Performed";
        redaction.PerformedAtUtc = DateTime.UtcNow;
        redaction.PerformedByUserId = GetCurrentUserId();
        redaction.RedactedStorageReference = request.RedactedStorageReference.Trim();
        await _context.SaveChangesAsync();
        return Ok(redaction);
    }

    [HttpPatch("redaction-requests/{redactionRequestId:long}/verify")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> VerifyRedaction(long redactionRequestId)
    {
        var redaction = await _context.RedactionRequests.FindAsync(redactionRequestId);
        if (redaction == null)
            return NotFound(new { message = "Redaction request not found." });

        redaction.Status = "Verified";
        redaction.VerifiedAtUtc = DateTime.UtcNow;
        redaction.VerifiedByUserId = GetCurrentUserId();
        await _context.SaveChangesAsync();
        return Ok(redaction);
    }

    [HttpPost("compliance-reports")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> RunComplianceReport([FromBody] ComplianceReportRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.ReportType))
            return BadRequest(new { message = "ReportType is required." });
        if (request.PeriodEndUtc <= request.PeriodStartUtc)
            return BadRequest(new { message = "PeriodEndUtc must be after PeriodStartUtc." });

        var outputReference = string.IsNullOrWhiteSpace(request.OutputReference)
            ? $"/reports/compliance/{request.ReportType.Trim()}-{DateTime.UtcNow:yyyyMMddHHmmss}.json"
            : request.OutputReference.Trim();

        var report = new ComplianceReportRun
        {
            ReportType = request.ReportType.Trim(),
            PeriodStartUtc = request.PeriodStartUtc,
            PeriodEndUtc = request.PeriodEndUtc,
            Status = "Completed",
            OutputReference = outputReference,
            RequestedByUserId = GetCurrentUserId(),
            CompletedAtUtc = DateTime.UtcNow
        };

        _context.ComplianceReportRuns.Add(report);
        await _context.SaveChangesAsync();
        return Ok(report);
    }

    private async Task<string> ComputeCollectionHashAsync(long collectionId, string newHash)
    {
        var hashes = await _context.EvidenceCollectionItems
            .Where(link => link.EvidenceCollectionId == collectionId)
            .Join(_context.EvidenceItems, link => link.EvidenceItemId, item => item.EvidenceItemId, (_, item) => item.HashSha256)
            .ToListAsync();

        hashes.Add(newHash);
        return ComputeHash(string.Join("|", hashes.OrderBy(hash => hash, StringComparer.Ordinal)));
    }

    private async Task MarkLegalHoldAsync(long? evidenceItemId, long? collectionId, bool isLegalHold)
    {
        if (evidenceItemId.HasValue)
        {
            var item = await _context.EvidenceItems.FindAsync(evidenceItemId.Value);
            if (item != null)
                item.IsLegalHold = isLegalHold;
        }

        if (collectionId.HasValue)
        {
            var itemIds = await _context.EvidenceCollectionItems
                .Where(link => link.EvidenceCollectionId == collectionId.Value)
                .Select(link => link.EvidenceItemId)
                .ToListAsync();
            var items = await _context.EvidenceItems.Where(item => itemIds.Contains(item.EvidenceItemId)).ToListAsync();
            foreach (var item in items)
                item.IsLegalHold = isLegalHold;
        }
    }

    private int? GetCurrentUserId()
    {
        var userIdClaim = User.FindFirstValue(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub);
        return int.TryParse(userIdClaim, out var userId) ? userId : null;
    }

    private static string ComputeHash(string input)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    private string GetEvidenceExportSigningKey() =>
        Environment.GetEnvironmentVariable("VSHIELD_EVIDENCE_EXPORT_SIGNING_KEY") ??
        _configuration["Evidence:ExportSigningKey"] ??
        _configuration["JwtSettings:Secret"] ??
        "development-export-signing-key-change-me";

    private static string ComputeHmac(string key, string payload)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(key));
        var bytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    public sealed record RetentionPolicyRequest(string Name, string? EvidenceType, string? RetentionCategory, int RetentionDays, string? PurgeMode, bool IsActive);
    public sealed record EvidenceItemRequest(string? EvidenceType, string? SourceType, string? SourceReference, long? SecurityEventId, long? AlarmId, long? IncidentId, string StorageReference, string? HashSha256, string? PrivacyLabel, string? RetentionCategory, int? SiteId, bool IsImmutable);
    public sealed record EvidenceHashVerificationRequest(string ObservedHashSha256, string? Purpose);
    public sealed record EvidenceCollectionRequest(string Name, string? Purpose, long? IncidentId);
    public sealed record EvidenceCollectionItemRequest(long EvidenceItemId);
    public sealed record ChainOfCustodyRequest(string? Action, string? FromCustodian, string? ToCustodian, string? HashBefore, string? HashAfter, string? Note);
    public sealed record LegalHoldRequest(long? EvidenceItemId, long? EvidenceCollectionId, string? Reason);
    public sealed record EvidenceExportRequestRequest(long? EvidenceItemId, long? EvidenceCollectionId, string Purpose, string Recipient);
    public sealed record ExportApprovalRequest(string? Watermark, string? SignatureReference);
    public sealed record RedactionRequestRequest(long EvidenceItemId, string Reason, string? PrivacyLabel);
    public sealed record RedactionPerformRequest(string RedactedStorageReference);
    public sealed record ComplianceReportRequest(string ReportType, DateTime PeriodStartUtc, DateTime PeriodEndUtc, string? OutputReference);
    public sealed record CloseRequest(string? Note);
    public sealed record RetentionDryRunRequest(DateTime? AsOfUtc, int Limit);
    public sealed record EvidencePurgeRequest(List<long> EvidenceItemIds, string Reason);
}
