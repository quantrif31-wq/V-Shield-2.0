using System.ComponentModel.DataAnnotations;

namespace API.Models;

public class EvidenceItem
{
    public long EvidenceItemId { get; set; }
    [MaxLength(80)] public string EvidenceType { get; set; } = "Document";
    [MaxLength(80)] public string SourceType { get; set; } = "Manual";
    [MaxLength(240)] public string? SourceReference { get; set; }
    public long? SecurityEventId { get; set; }
    public long? AlarmId { get; set; }
    public long? IncidentId { get; set; }
    [MaxLength(500)] public string StorageReference { get; set; } = string.Empty;
    [MaxLength(128)] public string HashSha256 { get; set; } = string.Empty;
    [MaxLength(80)] public string PrivacyLabel { get; set; } = "Internal";
    [MaxLength(80)] public string RetentionCategory { get; set; } = "Default";
    public int? SiteId { get; set; }
    public bool IsImmutable { get; set; }
    public bool IsLegalHold { get; set; }
    public int? CreatedByUserId { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}

public class EvidenceCollection
{
    public long EvidenceCollectionId { get; set; }
    [MaxLength(160)] public string Name { get; set; } = string.Empty;
    [MaxLength(160)] public string Purpose { get; set; } = "Investigation";
    public long? IncidentId { get; set; }
    [MaxLength(128)] public string? BundleHash { get; set; }
    [MaxLength(40)] public string Status { get; set; } = "Open";
    public int? CreatedByUserId { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}

public class EvidenceCollectionItem
{
    public long EvidenceCollectionItemId { get; set; }
    public long EvidenceCollectionId { get; set; }
    public long EvidenceItemId { get; set; }
    public DateTime AddedAtUtc { get; set; } = DateTime.UtcNow;
    public EvidenceCollection? Collection { get; set; }
    public EvidenceItem? EvidenceItem { get; set; }
}

public class EvidenceAccessLog
{
    public long EvidenceAccessLogId { get; set; }
    public long EvidenceItemId { get; set; }
    public int? UserId { get; set; }
    [MaxLength(80)] public string AccessType { get; set; } = "Read";
    [MaxLength(500)] public string Purpose { get; set; } = string.Empty;
    public DateTime AccessedAtUtc { get; set; } = DateTime.UtcNow;
    public EvidenceItem? EvidenceItem { get; set; }
}

public class EvidenceExportRequest
{
    public long EvidenceExportRequestId { get; set; }
    public long? EvidenceItemId { get; set; }
    public long? EvidenceCollectionId { get; set; }
    [MaxLength(500)] public string Purpose { get; set; } = string.Empty;
    [MaxLength(240)] public string Recipient { get; set; } = string.Empty;
    [MaxLength(40)] public string Status { get; set; } = "PendingApproval";
    public int? RequestedByUserId { get; set; }
    public int? ApprovedByUserId { get; set; }
    public DateTime RequestedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? ApprovedAtUtc { get; set; }
    [MaxLength(128)] public string? ExportHash { get; set; }
    [MaxLength(240)] public string? Watermark { get; set; }
    [MaxLength(240)] public string? SignatureReference { get; set; }
}

public class RetentionPolicy
{
    public int RetentionPolicyId { get; set; }
    [MaxLength(160)] public string Name { get; set; } = string.Empty;
    [MaxLength(80)] public string EvidenceType { get; set; } = "Any";
    [MaxLength(80)] public string RetentionCategory { get; set; } = "Default";
    public int RetentionDays { get; set; } = 365;
    [MaxLength(40)] public string PurgeMode { get; set; } = "ReviewRequired";
    public bool IsActive { get; set; } = true;
}

public class LegalHold
{
    public long LegalHoldId { get; set; }
    public long? EvidenceItemId { get; set; }
    public long? EvidenceCollectionId { get; set; }
    [MaxLength(500)] public string Reason { get; set; } = string.Empty;
    [MaxLength(40)] public string Status { get; set; } = "Active";
    public int? AppliedByUserId { get; set; }
    public int? ReleasedByUserId { get; set; }
    public DateTime AppliedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? ReleasedAtUtc { get; set; }
}

public class ChainOfCustodyEntry
{
    public long ChainOfCustodyEntryId { get; set; }
    public long EvidenceItemId { get; set; }
    [MaxLength(80)] public string Action { get; set; } = "Registered";
    public int? ActorUserId { get; set; }
    [MaxLength(160)] public string? FromCustodian { get; set; }
    [MaxLength(160)] public string? ToCustodian { get; set; }
    [MaxLength(128)] public string? HashBefore { get; set; }
    [MaxLength(128)] public string? HashAfter { get; set; }
    [MaxLength(1000)] public string? Note { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public EvidenceItem? EvidenceItem { get; set; }
}

public class RedactionRequest
{
    public long RedactionRequestId { get; set; }
    public long EvidenceItemId { get; set; }
    [MaxLength(500)] public string Reason { get; set; } = string.Empty;
    [MaxLength(80)] public string PrivacyLabel { get; set; } = "PersonalData";
    [MaxLength(40)] public string Status { get; set; } = "PendingApproval";
    public int? RequestedByUserId { get; set; }
    public int? ApprovedByUserId { get; set; }
    public int? PerformedByUserId { get; set; }
    public int? VerifiedByUserId { get; set; }
    public DateTime RequestedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? ApprovedAtUtc { get; set; }
    public DateTime? PerformedAtUtc { get; set; }
    public DateTime? VerifiedAtUtc { get; set; }
    [MaxLength(500)] public string? RedactedStorageReference { get; set; }
    public EvidenceItem? EvidenceItem { get; set; }
}

public class ComplianceReportRun
{
    public long ComplianceReportRunId { get; set; }
    [MaxLength(120)] public string ReportType { get; set; } = string.Empty;
    public DateTime PeriodStartUtc { get; set; }
    public DateTime PeriodEndUtc { get; set; }
    [MaxLength(40)] public string Status { get; set; } = "Completed";
    [MaxLength(500)] public string? OutputReference { get; set; }
    public int? RequestedByUserId { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAtUtc { get; set; } = DateTime.UtcNow;
}
