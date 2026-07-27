namespace API.Services.FaceRecognition;

public sealed record FaceCameraConfigurationDto(
    int Id,
    int CameraId,
    string CameraName,
    string RuntimeCameraId,
    int? LaneId,
    string? LaneName,
    string DesiredState,
    bool AutoRestore,
    long ConfigurationVersion,
    long LastAppliedVersion,
    string LastSyncStatus,
    string? LastSyncError,
    DateTime? LastSyncAtUtc,
    string? StreamUrlMasked,
    string? PreviewUrl,
    bool? RuntimeEnabled,
    bool? RuntimeConnected,
    string RuntimeStatus,
    string RowVersion);

public sealed class UpdateFaceCameraConfigurationRequest
{
    public int CameraId { get; init; }
    public int? LaneId { get; init; }
    public bool AutoRestore { get; init; } = true;
    public string? RowVersion { get; init; }
}

public sealed record FaceCameraRuntimeSessionDto(
    string RuntimeCameraId,
    string? LaneId,
    bool Enabled,
    bool Connected,
    string Status,
    bool IsManaged);

public sealed record FaceCameraConfigurationOverviewDto(
    IReadOnlyList<FaceCameraConfigurationDto> Configurations,
    IReadOnlyList<FaceCameraRuntimeSessionDto> UnmanagedSessions,
    bool RuntimeAvailable);

public sealed record FaceCameraDesiredStateDto(
    FaceCameraConfigurationDto Configuration,
    bool RuntimeApplied,
    int? RuntimeStatusCode);

public sealed record FaceCameraReconcileResultDto(
    bool Completed,
    bool SkippedBecauseRunning,
    bool RuntimeAvailable,
    int ManagedCount,
    int StartedCount,
    int StoppedCount,
    int RestartedCount,
    int FailedCount,
    int UnmanagedCount);

public sealed record FaceRuntimeSession(
    string CameraId,
    string? LaneId,
    bool Enabled,
    bool Connected,
    string Status);

public sealed record FaceRuntimeInventory(
    bool Available,
    IReadOnlyDictionary<string, FaceRuntimeSession> Sessions);
