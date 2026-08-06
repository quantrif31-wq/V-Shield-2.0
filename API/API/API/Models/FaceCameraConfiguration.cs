using System.ComponentModel.DataAnnotations;

namespace API.Models;

public static class FaceCameraDesiredStates
{
    public const string Running = "Running";
    public const string Stopped = "Stopped";
}

public static class FaceCameraSyncStatuses
{
    public const string Pending = "Pending";
    public const string Synced = "Synced";
    public const string Unavailable = "Unavailable";
    public const string Error = "Error";
}

public sealed class FaceCameraConfiguration
{
    public int Id { get; set; }
    public int CameraId { get; set; }
    [MaxLength(64)] public string RuntimeCameraId { get; set; } = string.Empty;
    public int? LaneId { get; set; }
    [MaxLength(16)] public string DesiredState { get; set; } = FaceCameraDesiredStates.Stopped;
    public bool AutoRestore { get; set; } = true;
    public long ConfigurationVersion { get; set; } = 1;
    public long LastAppliedVersion { get; set; }
    [MaxLength(32)] public string LastSyncStatus { get; set; } = FaceCameraSyncStatuses.Pending;
    [MaxLength(500)] public string? LastSyncError { get; set; }
    public DateTime? LastSyncAtUtc { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
    [MaxLength(64)] public string ConfigurationFingerprint { get; set; } = string.Empty;
    [Timestamp] public byte[] RowVersion { get; set; } = Array.Empty<byte>();

    public Camera Camera { get; set; } = null!;
    public Lane? Lane { get; set; }
}
