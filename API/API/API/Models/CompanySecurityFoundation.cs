using System.ComponentModel.DataAnnotations;

namespace API.Models;

public static class EmployeeLifecycleStates
{
    public const string PreHire = "PreHire";
    public const string Active = "Active";
    public const string Suspended = "Suspended";
    public const string OnLeave = "OnLeave";
    public const string Terminated = "Terminated";
    public const string ContractorActive = "ContractorActive";
    public const string ContractorExpired = "ContractorExpired";
}

public class Company
{
    public int CompanyId { get; set; }
    [MaxLength(160)] public string Name { get; set; } = string.Empty;
    [MaxLength(50)] public string Code { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public ICollection<Site> Sites { get; set; } = new List<Site>();
}

public class Site
{
    public int SiteId { get; set; }
    public int CompanyId { get; set; }
    [MaxLength(160)] public string Name { get; set; } = string.Empty;
    [MaxLength(50)] public string Code { get; set; } = string.Empty;
    [MaxLength(300)] public string? Address { get; set; }
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
    [MaxLength(80)] public string TimeZoneId { get; set; } = "Asia/Ho_Chi_Minh";
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public Company? Company { get; set; }
    public ICollection<Building> Buildings { get; set; } = new List<Building>();
    public ICollection<SecurityZone> Zones { get; set; } = new List<SecurityZone>();
}

public class Building
{
    public int BuildingId { get; set; }
    public int SiteId { get; set; }
    [MaxLength(160)] public string Name { get; set; } = string.Empty;
    [MaxLength(50)] public string Code { get; set; } = string.Empty;
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
    public int? TotalFloors { get; set; }
    public bool IsActive { get; set; } = true;
    public Site? Site { get; set; }
    public ICollection<FacilityFloor> Floors { get; set; } = new List<FacilityFloor>();
}

public class FacilityFloor
{
    public int FacilityFloorId { get; set; }
    public int BuildingId { get; set; }
    [MaxLength(120)] public string Name { get; set; } = string.Empty;
    [MaxLength(50)] public string Code { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
    public Building? Building { get; set; }
    public ICollection<SecurityZone> Zones { get; set; } = new List<SecurityZone>();
}

public class SecurityZone
{
    public int SecurityZoneId { get; set; }
    public int SiteId { get; set; }
    public int? BuildingId { get; set; }
    public int? FacilityFloorId { get; set; }
    [MaxLength(160)] public string Name { get; set; } = string.Empty;
    [MaxLength(50)] public string Code { get; set; } = string.Empty;
    [MaxLength(40)] public string SecurityLevel { get; set; } = "Normal";
    public bool IsRestricted { get; set; }
    public bool IsActive { get; set; } = true;
    public Site? Site { get; set; }
    public FacilityFloor? FacilityFloor { get; set; }
    public ICollection<AccessPoint> AccessPoints { get; set; } = new List<AccessPoint>();
}

public class AccessPoint
{
    public int AccessPointId { get; set; }
    public int SiteId { get; set; }
    public int? SecurityZoneId { get; set; }
    [MaxLength(160)] public string Name { get; set; } = string.Empty;
    [MaxLength(60)] public string Type { get; set; } = "Door";
    [MaxLength(80)] public string DirectionMode { get; set; } = "Bidirectional";
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
    public bool IsActive { get; set; } = true;
    public Site? Site { get; set; }
    public SecurityZone? SecurityZone { get; set; }
    public Door? Door { get; set; }
}

public class Door
{
    public int DoorId { get; set; }
    public int AccessPointId { get; set; }
    [MaxLength(160)] public string Name { get; set; } = string.Empty;
    [MaxLength(60)] public string DoorMode { get; set; } = "Normal";
    public bool IsActive { get; set; } = true;
    public AccessPoint? AccessPoint { get; set; }
}

public class Lane
{
    public int LaneId { get; set; }
    public int SiteId { get; set; }
    public int? GateId { get; set; }
    public int? AccessPointId { get; set; }
    [MaxLength(160)] public string Name { get; set; } = string.Empty;
    [MaxLength(40)] public string Direction { get; set; } = "Entry";
    public bool IsActive { get; set; } = true;
    public Site? Site { get; set; }
    public Gate? Gate { get; set; }
    public AccessPoint? AccessPoint { get; set; }
}

public class MusterPoint
{
    public int MusterPointId { get; set; }
    public int SiteId { get; set; }
    [MaxLength(160)] public string Name { get; set; } = string.Empty;
    [MaxLength(300)] public string? LocationNote { get; set; }
    public int? Capacity { get; set; }
    public bool IsActive { get; set; } = true;
    public Site? Site { get; set; }
}

public class ExternalIdentityProvider
{
    public int ExternalIdentityProviderId { get; set; }
    [MaxLength(160)] public string Name { get; set; } = string.Empty;
    [MaxLength(40)] public string Protocol { get; set; } = "OIDC";
    [MaxLength(300)] public string Authority { get; set; } = string.Empty;
    [MaxLength(160)] public string? ClientId { get; set; }
    [MaxLength(400)] public string? ClientSecret { get; set; }
    [MaxLength(300)] public string? RedirectUrl { get; set; }
    [MaxLength(200)] public string? Scopes { get; set; }
    public bool IsEnabled { get; set; } = true;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}

public class ExternalIdentityMapping
{
    public int ExternalIdentityMappingId { get; set; }
    public int ExternalIdentityProviderId { get; set; }
    public int? UserId { get; set; }
    public int? EmployeeId { get; set; }
    [MaxLength(240)] public string ExternalSubject { get; set; } = string.Empty;
    [MaxLength(160)] public string? ExternalUsername { get; set; }
    public DateTime? LastSyncedAtUtc { get; set; }
    public bool IsActive { get; set; } = true;
    public ExternalIdentityProvider? Provider { get; set; }
    public AppUser? User { get; set; }
    public Employee? Employee { get; set; }
}

public class EmployeeLifecycleEvent
{
    public int EmployeeLifecycleEventId { get; set; }
    public int EmployeeId { get; set; }
    [MaxLength(40)] public string PreviousState { get; set; } = EmployeeLifecycleStates.Active;
    [MaxLength(40)] public string NewState { get; set; } = EmployeeLifecycleStates.Active;
    [MaxLength(500)] public string? Reason { get; set; }
    public DateTime EffectiveAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public int? ChangedByUserId { get; set; }
    public Employee? Employee { get; set; }
    public AppUser? ChangedByUser { get; set; }
}

public class AccessRecertificationCampaign
{
    public int AccessRecertificationCampaignId { get; set; }
    [MaxLength(160)] public string Name { get; set; } = string.Empty;
    public int? SiteId { get; set; }
    public DateTime PeriodStartUtc { get; set; }
    public DateTime PeriodEndUtc { get; set; }
    [MaxLength(40)] public string Status { get; set; } = "Draft";
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public Site? Site { get; set; }
    public ICollection<AccessRecertificationDecision> Decisions { get; set; } = new List<AccessRecertificationDecision>();
}

public class AccessRecertificationDecision
{
    public int AccessRecertificationDecisionId { get; set; }
    public int AccessRecertificationCampaignId { get; set; }
    public int EmployeeId { get; set; }
    public int? ReviewerUserId { get; set; }
    [MaxLength(40)] public string Decision { get; set; } = "Pending";
    [MaxLength(1000)] public string? Notes { get; set; }
    public DateTime? DecidedAtUtc { get; set; }
    public AccessRecertificationCampaign? Campaign { get; set; }
    public Employee? Employee { get; set; }
    public AppUser? ReviewerUser { get; set; }
}

