using System.ComponentModel.DataAnnotations;

namespace API.Models;

public class LostItemReport
{
    public long LostItemReportId { get; set; }
    [MaxLength(120)] public string ReporterName { get; set; } = string.Empty;
    [MaxLength(20)] public string ReporterPhone { get; set; } = string.Empty;
    [MaxLength(240)] public string? ReporterEmail { get; set; }
    [MaxLength(1000)] public string ItemDescription { get; set; } = string.Empty;
    [MaxLength(240)] public string? LastSeenLocation { get; set; }
    public DateTime LostAtUtc { get; set; }
    [MaxLength(500)] public string? PhotoUrl { get; set; }
    [MaxLength(40)] public string Status { get; set; } = "Pending";
    public int? CreatedByUserId { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? ClosedAtUtc { get; set; }
}

public class FoundItemReport
{
    public long FoundItemReportId { get; set; }
    [MaxLength(120)] public string FoundByName { get; set; } = string.Empty;
    [MaxLength(240)] public string FoundLocation { get; set; } = string.Empty;
    public DateTime FoundAtUtc { get; set; } = DateTime.UtcNow;
    [MaxLength(1000)] public string ItemDescription { get; set; } = string.Empty;
    [MaxLength(500)] public string? PhotoUrl { get; set; }
    [MaxLength(500)] public string? StorageLocation { get; set; }
    public int? LockerCompartmentId { get; set; }
    [MaxLength(40)] public string Status { get; set; } = "Unclaimed";
    public int? CreatedByUserId { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? ReturnedAtUtc { get; set; }
    public LockerCompartment? LockerCompartment { get; set; }
}

public class ItemMatch
{
    public long ItemMatchId { get; set; }
    public long LostItemReportId { get; set; }
    public long FoundItemReportId { get; set; }
    public double ConfidenceScore { get; set; }
    public int? MatchedByUserId { get; set; }
    public DateTime MatchedAtUtc { get; set; } = DateTime.UtcNow;
    [MaxLength(40)] public string Status { get; set; } = "Suggested";
    [MaxLength(500)] public string? Note { get; set; }
    public LostItemReport? LostItem { get; set; }
    public FoundItemReport? FoundItem { get; set; }
}

public class ClaimRequest
{
    public long ClaimRequestId { get; set; }
    public long FoundItemReportId { get; set; }
    public long? LostItemReportId { get; set; }
    [MaxLength(120)] public string ClaimantName { get; set; } = string.Empty;
    [MaxLength(40)] public string ClaimantIdNumber { get; set; } = string.Empty;
    [MaxLength(20)] public string ClaimantPhone { get; set; } = string.Empty;
    [MaxLength(500)] public string? ProofDocumentUrl { get; set; }
    [MaxLength(40)] public string Status { get; set; } = "Pending";
    public int? ReviewedByUserId { get; set; }
    public DateTime RequestedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? ReviewedAtUtc { get; set; }
    public DateTime? CompletedAtUtc { get; set; }
    public FoundItemReport? FoundItem { get; set; }
    public LostItemReport? LostItem { get; set; }
}

public class LockerCabinet
{
    public int LockerCabinetId { get; set; }
    [MaxLength(120)] public string Name { get; set; } = string.Empty;
    [MaxLength(240)] public string? Location { get; set; }
    [MaxLength(500)] public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}

public class LockerCompartment
{
    public int LockerCompartmentId { get; set; }
    public int LockerCabinetId { get; set; }
    [MaxLength(20)] public string Code { get; set; } = string.Empty;
    [MaxLength(40)] public string Status { get; set; } = "Empty";
    public long? EvidenceItemId { get; set; }
    public int? OccupiedByUserId { get; set; }
    public DateTime? OccupiedAtUtc { get; set; }
    public DateTime? ReleasedAtUtc { get; set; }
    public LockerCabinet? Cabinet { get; set; }
    public EvidenceItem? EvidenceItem { get; set; }
}

public class LockerAccessLog
{
    public long LockerAccessLogId { get; set; }
    public int LockerCompartmentId { get; set; }
    public int? UserId { get; set; }
    [MaxLength(80)] public string Action { get; set; } = string.Empty;
    [MaxLength(500)] public string? Purpose { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public LockerCompartment? Compartment { get; set; }
}
