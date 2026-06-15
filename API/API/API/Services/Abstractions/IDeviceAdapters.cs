using API.Models;

namespace API.Services.Abstractions;

public enum AdapterCapability
{
    None = 0,
    SecureChannel = 1,
    CredentialDownload = 2,
    OfflineDecision = 4,
    TamperDetection = 8,
    HealthHeartbeat = 16,
    ConfigurationPush = 32,
    FirmwareUpdate = 64,
}

public class AdapterStatus
{
    public bool Connected { get; set; }
    public string State { get; set; } = "Unknown";
    public string? FirmwareVersion { get; set; }
    public DateTime? LastContactUtc { get; set; }
    public int? LatencyMs { get; set; }
    public string? ErrorMessage { get; set; }
}

public class AdapterAccessDecision
{
    public bool Allowed { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string? CredentialId { get; set; }
    public DateTime EvaluatedAtUtc { get; set; } = DateTime.UtcNow;
}

public interface IAccessControllerAdapter
{
    string Protocol { get; }
    AdapterCapability Capabilities { get; }
    Task<AdapterStatus> GetStatusAsync(CancellationToken ct = default);
    Task<AdapterAccessDecision> EvaluateOfflineAsync(string credentialId, int? subjectId, string? subjectType, CancellationToken ct = default);
    Task<bool> PushConfigurationAsync(string configJson, CancellationToken ct = default);
    Task<bool> DownloadCredentialsAsync(IEnumerable<string> credentialIds, CancellationToken ct = default);
}

public interface IReaderAdapter
{
    string ReaderProtocol { get; }
    Task<AdapterStatus> GetStatusAsync(CancellationToken ct = default);
    Task<string?> ReadCredentialAsync(CancellationToken ct = default);
    Task<bool> PresentCredentialAsync(string credentialData, CancellationToken ct = default);
}

public interface IRelayAdapter
{
    string Name { get; }
    Task<AdapterStatus> GetStatusAsync(CancellationToken ct = default);
    Task<bool> OpenAsync(CancellationToken ct = default);
    Task<bool> CloseAsync(CancellationToken ct = default);
    Task<bool> HoldOpenAsync(CancellationToken ct = default);
    Task<bool> LockClosedAsync(CancellationToken ct = default);
    Task<string> GetStateAsync(CancellationToken ct = default);
}

public interface IBarrierAdapter
{
    Task<AdapterStatus> GetStatusAsync(CancellationToken ct = default);
    Task<bool> RaiseBarrierAsync(CancellationToken ct = default);
    Task<bool> LowerBarrierAsync(CancellationToken ct = default);
    Task<bool> HoldOpenAsync(CancellationToken ct = default);
    Task<string> GetStateAsync(CancellationToken ct = default);
}

public interface ICameraAccessAdapter
{
    Task<AdapterStatus> GetStatusAsync(CancellationToken ct = default);
    Task<string?> CaptureSnapshotAsync(CancellationToken ct = default);
    Task<bool> StreamHealthAsync(CancellationToken ct = default);
}

public interface IDeviceSimulator
{
    string SimulatorType { get; }
    Task<SecurityDevice> CreateVirtualDeviceAsync(string name, int? siteId, int? accessPointId, CancellationToken ct = default);
    Task<AccessDecision> SimulateOfflineDecisionAsync(int deviceId, string subjectType, int? subjectId, string credentialType, CancellationToken ct = default);
    Task<bool> InjectFaultAsync(int deviceId, string faultType, string severity, string? message, CancellationToken ct = default);
    Task<bool> RestoreNormalAsync(int deviceId, CancellationToken ct = default);
}
