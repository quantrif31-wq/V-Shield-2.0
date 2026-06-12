using System.ComponentModel.DataAnnotations;

namespace API.Models;

public static class PrivilegedActions
{
    public const string All = "AllPrivilegedActions";
    public const string UserAdministration = "UserAdministration";
    public const string AccessPolicyEmergency = "AccessPolicyEmergency";
    public const string DeviceConfiguration = "DeviceConfiguration";
    public const string EvidenceExportApproval = "EvidenceExportApproval";
    public const string EvidenceLegalHoldRelease = "EvidenceLegalHoldRelease";
    public const string EvidenceRetentionPurge = "EvidenceRetentionPurge";
    public const string EvidenceRedactionApproval = "EvidenceRedactionApproval";
    public const string ReleaseApproval = "ReleaseApproval";
    public const string SiteHierarchyBackfill = "SiteHierarchyBackfill";
}

public class PrivilegedActionSession
{
    public long PrivilegedActionSessionId { get; set; }
    public int UserId { get; set; }
    [MaxLength(120)] public string Action { get; set; } = PrivilegedActions.All;
    [MaxLength(500)] public string? Reason { get; set; }
    [MaxLength(80)] public string Status { get; set; } = "Pending";
    [MaxLength(120)] public string ChallengeNonce { get; set; } = Guid.NewGuid().ToString("N");
    [MaxLength(80)] public string CorrelationId { get; set; } = Guid.NewGuid().ToString("N");
    [MaxLength(80)] public string? IpAddress { get; set; }
    [MaxLength(500)] public string? UserAgent { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? VerifiedAtUtc { get; set; }
    public DateTime ExpiresAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? RevokedAtUtc { get; set; }
    public AppUser? User { get; set; }
}
