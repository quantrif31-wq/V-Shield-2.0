using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace API.Models;

public static class ContractorStatuses
{
    public const string Active = "Active";
    public const string Expiring = "Expiring";
    public const string Expired = "Expired";
    public const string Revoked = "Revoked";
}

[Table("Contractors")]
public class Contractor
{
    [Key] public int ContractorId { get; set; }
    public int? EmployeeId { get; set; }
    [MaxLength(180)] public string FullName { get; set; } = string.Empty;
    [MaxLength(180)] public string Company { get; set; } = string.Empty;
    [MaxLength(80)] public string? Phone { get; set; }
    [MaxLength(160)] public string? Email { get; set; }
    public DateTime ContractFromUtc { get; set; }
    public DateTime ContractToUtc { get; set; }
    [MaxLength(40)] public string Status { get; set; } = ContractorStatuses.Active;
    public int? SiteId { get; set; }
    [MaxLength(1000)] public string? RequiredTraining { get; set; }
    public bool AccessReviewCompleted { get; set; }
    public DateTime? AccessReviewDateUtc { get; set; }
    public DateTime? RevokedAtUtc { get; set; }
    public int? RevokedByUserId { get; set; }
    [MaxLength(1000)] public string? RevocationReason { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public Employee? Employee { get; set; }
    public Site? Site { get; set; }
}

public static class VisitStatuses
{
    public const string Invited = "Invited";
    public const string Approved = "Approved";
    public const string CheckedIn = "CheckedIn";
    public const string Overstay = "Overstay";
    public const string CheckedOut = "CheckedOut";
    public const string Denied = "Denied";
}

public class Visit
{
    public int VisitId { get; set; }
    public int? SiteId { get; set; }
    public int? HostEmployeeId { get; set; }
    [MaxLength(180)] public string VisitorName { get; set; } = string.Empty;
    [MaxLength(40)] public string VisitorType { get; set; } = "Visitor";
    [MaxLength(80)] public string? VisitorPhone { get; set; }
    [MaxLength(160)] public string? VisitorEmail { get; set; }
    [MaxLength(40)] public string Status { get; set; } = VisitStatuses.Invited;
    public DateTime ExpectedInUtc { get; set; }
    public DateTime ExpectedOutUtc { get; set; }
    public bool EscortRequired { get; set; }
    public bool NdaRequired { get; set; }
    public bool SafetyBriefingRequired { get; set; }
    public bool HostNotified { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public Site? Site { get; set; }
    public Employee? HostEmployee { get; set; }
}

public class VisitorCredential
{
    public int VisitorCredentialId { get; set; }
    public int VisitId { get; set; }
    [MaxLength(40)] public string CredentialType { get; set; } = "QR";
    [MaxLength(200)] public string CredentialReference { get; set; } = string.Empty;
    public DateTime ValidFromUtc { get; set; }
    public DateTime ValidToUtc { get; set; }
    public bool IsRevoked { get; set; }
    public Visit? Visit { get; set; }
}

public class VisitorCheckIn
{
    public int VisitorCheckInId { get; set; }
    public int VisitId { get; set; }
    public DateTime? CheckedInAtUtc { get; set; }
    public DateTime? CheckedOutAtUtc { get; set; }
    public int? CheckedInByUserId { get; set; }
    public int? CheckedOutByUserId { get; set; }
    [MaxLength(120)] public string? IdDocumentType { get; set; }
    [MaxLength(256)] public string? IdDocumentReference { get; set; }
    [MaxLength(40)] public string VerificationStatus { get; set; } = "Pending";
    public Visit? Visit { get; set; }
}

public class VisitorFormTemplate
{
    public int VisitorFormTemplateId { get; set; }
    [MaxLength(160)] public string Name { get; set; } = string.Empty;
    [MaxLength(40)] public string FormType { get; set; } = "NDA";
    public int Version { get; set; } = 1;
    [MaxLength(4000)] public string Body { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}

public class VisitorFormAcceptance
{
    public int VisitorFormAcceptanceId { get; set; }
    public int VisitId { get; set; }
    public int VisitorFormTemplateId { get; set; }
    public DateTime AcceptedAtUtc { get; set; } = DateTime.UtcNow;
    [MaxLength(180)] public string AcceptedByName { get; set; } = string.Empty;
    public Visit? Visit { get; set; }
    public VisitorFormTemplate? Template { get; set; }
}

public class WatchlistEntry
{
    public int WatchlistEntryId { get; set; }
    [MaxLength(40)] public string EntityType { get; set; } = "Person";
    [MaxLength(180)] public string DisplayName { get; set; } = string.Empty;
    [MaxLength(80)] public string? Identifier { get; set; }
    [MaxLength(40)] public string Severity { get; set; } = "Medium";
    [MaxLength(1000)] public string Reason { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}

public class WatchlistMatch
{
    public int WatchlistMatchId { get; set; }
    public int WatchlistEntryId { get; set; }
    public int? VisitId { get; set; }
    public int? VehicleId { get; set; }
    [MaxLength(40)] public string Status { get; set; } = "Pending";
    [MaxLength(1000)] public string? ReviewNote { get; set; }
    public DateTime MatchedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? ReviewedAtUtc { get; set; }
    public int? ReviewedByUserId { get; set; }
    public WatchlistEntry? WatchlistEntry { get; set; }
    public Visit? Visit { get; set; }
    public Vehicle? Vehicle { get; set; }
}

public class ParkingArea
{
    public int ParkingAreaId { get; set; }
    public int? SiteId { get; set; }
    [MaxLength(160)] public string Name { get; set; } = string.Empty;
    public int? Capacity { get; set; }
    public bool IsActive { get; set; } = true;
    public Site? Site { get; set; }
}

public class ParkingPermit
{
    public int ParkingPermitId { get; set; }
    public int ParkingAreaId { get; set; }
    public int? VehicleId { get; set; }
    public int? VisitId { get; set; }
    [MaxLength(60)] public string PermitType { get; set; } = "Temporary";
    public DateTime ValidFromUtc { get; set; }
    public DateTime ValidToUtc { get; set; }
    public bool IsRevoked { get; set; }
    public ParkingArea? ParkingArea { get; set; }
    public Vehicle? Vehicle { get; set; }
    public Visit? Visit { get; set; }
}

public static class ReceptionInteractionTypes
{
    public const string HostContact = "HostContact";
    public const string VisitorSupport = "VisitorSupport";
    public const string SecurityDispatch = "SecurityDispatch";
    public const string ParkingInquiry = "ParkingInquiry";
    public const string LostFoundSupport = "LostFoundSupport";
    public const string Wayfinding = "Wayfinding";
    public const string FollowUp = "FollowUp";
}

public static class ReceptionInteractionStatuses
{
    public const string Open = "Open";
    public const string InProgress = "InProgress";
    public const string Resolved = "Resolved";
    public const string Escalated = "Escalated";
    public const string Cancelled = "Cancelled";
}

public class ReceptionInteraction
{
    public long ReceptionInteractionId { get; set; }
    public int? VisitId { get; set; }
    public long? LostItemReportId { get; set; }
    public long? FoundItemReportId { get; set; }
    [MaxLength(60)] public string InteractionType { get; set; } = ReceptionInteractionTypes.VisitorSupport;
    [MaxLength(200)] public string Summary { get; set; } = string.Empty;
    [MaxLength(2000)] public string? DetailNote { get; set; }
    [MaxLength(180)] public string? ContactPersonName { get; set; }
    [MaxLength(80)] public string? ContactPersonPhone { get; set; }
    [MaxLength(40)] public string? RelatedVehiclePlate { get; set; }
    [MaxLength(40)] public string Status { get; set; } = ReceptionInteractionStatuses.Open;
    public bool SecurityRequested { get; set; }
    [MaxLength(1000)] public string? ResolutionNote { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAtUtc { get; set; }
    public int? CreatedByUserId { get; set; }
    public int? UpdatedByUserId { get; set; }
    public Visit? Visit { get; set; }
    public LostItemReport? LostItemReport { get; set; }
    public FoundItemReport? FoundItemReport { get; set; }
}

public class SecurityBarrier
{
    public int BarrierId { get; set; }
    public int? LaneId { get; set; }
    [MaxLength(160)] public string Name { get; set; } = string.Empty;
    [MaxLength(40)] public string State { get; set; } = "Unknown";
    public bool IsActive { get; set; } = true;
    public Lane? Lane { get; set; }
}

public class LaneEvent
{
    public long LaneEventId { get; set; }
    public int? LaneId { get; set; }
    public int? VehicleId { get; set; }
    [MaxLength(40)] public string EventType { get; set; } = "VehicleSeen";
    [MaxLength(40)] public string Direction { get; set; } = "Entry";
    [MaxLength(80)] public string? PlateText { get; set; }
    [MaxLength(1000)] public string? Note { get; set; }
    public DateTime OccurredAtUtc { get; set; } = DateTime.UtcNow;
    public Lane? Lane { get; set; }
    public Vehicle? Vehicle { get; set; }
}

public class BarrierCommandAudit
{
    public long BarrierCommandAuditId { get; set; }
    public int BarrierId { get; set; }
    [MaxLength(40)] public string Command { get; set; } = "Open";
    [MaxLength(1000)] public string Reason { get; set; } = string.Empty;
    public int? RequestedByUserId { get; set; }
    public DateTime RequestedAtUtc { get; set; } = DateTime.UtcNow;
    [MaxLength(40)] public string Result { get; set; } = "Recorded";
    public SecurityBarrier? Barrier { get; set; }
}
