using System.ComponentModel.DataAnnotations;

namespace API.Models;

public class QaTestRun
{
    public long QaTestRunId { get; set; }
    [MaxLength(80)] public string TestType { get; set; } = string.Empty;
    [MaxLength(120)] public string Profile { get; set; } = "MediumCompany";
    [MaxLength(40)] public string Status { get; set; } = "Running";
    public int PassedCount { get; set; }
    public int FailedCount { get; set; }
    [MaxLength(500)] public string? EvidenceReference { get; set; }
    public DateTime StartedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAtUtc { get; set; }
    [MaxLength(1000)] public string? Notes { get; set; }
}

public class ReleaseCandidate
{
    public long ReleaseCandidateId { get; set; }
    [MaxLength(80)] public string Version { get; set; } = string.Empty;
    [MaxLength(40)] public string Status { get; set; } = "Draft";
    [MaxLength(160)] public string? MigrationId { get; set; }
    [MaxLength(240)] public string? BuildReference { get; set; }
    public int? CreatedByUserId { get; set; }
    public int? ApprovedByUserId { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? ApprovedAtUtc { get; set; }
}

public class ReleaseGateCheck
{
    public long ReleaseGateCheckId { get; set; }
    public long ReleaseCandidateId { get; set; }
    [MaxLength(120)] public string GateName { get; set; } = string.Empty;
    [MaxLength(40)] public string Status { get; set; } = "Pending";
    public bool Required { get; set; } = true;
    [MaxLength(500)] public string? EvidenceReference { get; set; }
    [MaxLength(1000)] public string? Notes { get; set; }
    public int? VerifiedByUserId { get; set; }
    public DateTime VerifiedAtUtc { get; set; } = DateTime.UtcNow;
    public ReleaseCandidate? ReleaseCandidate { get; set; }
}

public class RunbookAcknowledgement
{
    public long RunbookAcknowledgementId { get; set; }
    [MaxLength(120)] public string RunbookName { get; set; } = string.Empty;
    [MaxLength(80)] public string RoleName { get; set; } = string.Empty;
    public int? AcknowledgedByUserId { get; set; }
    [MaxLength(240)] public string? EvidenceReference { get; set; }
    public DateTime AcknowledgedAtUtc { get; set; } = DateTime.UtcNow;
}
