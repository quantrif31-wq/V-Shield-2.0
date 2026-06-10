using System.ComponentModel.DataAnnotations;

namespace API.Models;

public class SecurityDevice
{
    public int SecurityDeviceId { get; set; }
    public int? SiteId { get; set; }
    public int? AccessPointId { get; set; }
    [MaxLength(80)] public string DeviceType { get; set; } = "Controller";
    [MaxLength(160)] public string Name { get; set; } = string.Empty;
    [MaxLength(120)] public string? Vendor { get; set; }
    [MaxLength(120)] public string? Model { get; set; }
    [MaxLength(120)] public string? SerialNumber { get; set; }
    [MaxLength(60)] public string Status { get; set; } = "Pending";
    [MaxLength(80)] public string? FirmwareVersion { get; set; }
    [MaxLength(80)] public string? ConfigurationVersion { get; set; }
    public DateTime? LastSeenAtUtc { get; set; }
    public bool IsActive { get; set; } = true;
    public Site? Site { get; set; }
    public AccessPoint? AccessPoint { get; set; }
}

public class AccessControllerDevice
{
    public int AccessControllerDeviceId { get; set; }
    public int SecurityDeviceId { get; set; }
    [MaxLength(60)] public string Protocol { get; set; } = "OSDP";
    public bool SupportsOfflineDecision { get; set; }
    public int MaxCredentials { get; set; }
    public SecurityDevice? SecurityDevice { get; set; }
}

public class ReaderDevice
{
    public int ReaderDeviceId { get; set; }
    public int SecurityDeviceId { get; set; }
    public int? AccessControllerDeviceId { get; set; }
    [MaxLength(60)] public string ReaderProtocol { get; set; } = "OSDP";
    [MaxLength(80)] public string CredentialFormats { get; set; } = "QR,Badge,PIN";
    public SecurityDevice? SecurityDevice { get; set; }
    public AccessControllerDevice? Controller { get; set; }
}

public class DeviceRelay
{
    public int DeviceRelayId { get; set; }
    public int SecurityDeviceId { get; set; }
    [MaxLength(80)] public string Name { get; set; } = string.Empty;
    [MaxLength(40)] public string State { get; set; } = "Unknown";
    public SecurityDevice? SecurityDevice { get; set; }
}

public class DeviceSensor
{
    public int DeviceSensorId { get; set; }
    public int SecurityDeviceId { get; set; }
    [MaxLength(80)] public string SensorType { get; set; } = "DoorContact";
    [MaxLength(40)] public string State { get; set; } = "Unknown";
    public bool IsTamperSensor { get; set; }
    public SecurityDevice? SecurityDevice { get; set; }
}

public class DeviceCredential
{
    public int DeviceCredentialId { get; set; }
    public int SecurityDeviceId { get; set; }
    [MaxLength(80)] public string CredentialType { get; set; } = "ApiKey";
    [MaxLength(120)] public string CredentialReference { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? RotatedAtUtc { get; set; }
    public DateTime? RevokedAtUtc { get; set; }
    public SecurityDevice? SecurityDevice { get; set; }
}

public class DeviceHealthSnapshot
{
    public long DeviceHealthSnapshotId { get; set; }
    public int SecurityDeviceId { get; set; }
    [MaxLength(40)] public string Status { get; set; } = "Unknown";
    [MaxLength(1000)] public string? Message { get; set; }
    public int? LatencyMs { get; set; }
    public DateTime CapturedAtUtc { get; set; } = DateTime.UtcNow;
    public SecurityDevice? SecurityDevice { get; set; }
}

public class DeviceConfigurationVersion
{
    public int DeviceConfigurationVersionId { get; set; }
    public int SecurityDeviceId { get; set; }
    [MaxLength(80)] public string Version { get; set; } = string.Empty;
    [MaxLength(4000)] public string ConfigurationJson { get; set; } = "{}";
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public int? CreatedByUserId { get; set; }
    public SecurityDevice? SecurityDevice { get; set; }
}

public class DeviceProvisioningRequest
{
    public int DeviceProvisioningRequestId { get; set; }
    public int? SecurityDeviceId { get; set; }
    [MaxLength(80)] public string DeviceType { get; set; } = "Controller";
    [MaxLength(160)] public string RequestedName { get; set; } = string.Empty;
    [MaxLength(40)] public string Status { get; set; } = "Pending";
    [MaxLength(1000)] public string? ApprovalNote { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? ApprovedAtUtc { get; set; }
    public int? ApprovedByUserId { get; set; }
    public SecurityDevice? SecurityDevice { get; set; }
}

public class OfflinePolicyPackage
{
    public int OfflinePolicyPackageId { get; set; }
    public int? SecurityDeviceId { get; set; }
    [MaxLength(80)] public string PackageVersion { get; set; } = string.Empty;
    [MaxLength(4000)] public string PayloadJson { get; set; } = "{}";
    [MaxLength(128)] public string? PayloadHash { get; set; }
    [MaxLength(40)] public string Status { get; set; } = "Draft";
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? PublishedAtUtc { get; set; }
    public SecurityDevice? SecurityDevice { get; set; }
}

