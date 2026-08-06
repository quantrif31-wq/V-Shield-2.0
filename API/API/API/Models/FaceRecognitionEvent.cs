using System.ComponentModel.DataAnnotations;

namespace API.Models;

public sealed class FaceRecognitionEvent
{
    public long Id { get; set; }
    public Guid RuntimeEventId { get; set; }
    [MaxLength(64)] public string CameraId { get; set; } = string.Empty;
    public int? FaceCameraConfigurationId { get; set; }
    public int? LaneId { get; set; }
    public int? EmployeeId { get; set; }
    [MaxLength(40)] public string? RuntimeSubjectId { get; set; }
    [MaxLength(20)] public string EventType { get; set; } = "Recognized";
    public DateTime OccurredAtUtc { get; set; }
    public DateTime ReceivedAtUtc { get; set; }
    public long RuntimeSequence { get; set; }
    public long RuntimeSessionGeneration { get; set; }
    public double? RecognitionDistance { get; set; }
    public long? ModelRegistryVersion { get; set; }
    public int? EmployeeFaceModelId { get; set; }
    [MaxLength(255)] public string? ModelFileName { get; set; }
    [MaxLength(12)] public string? ModelChecksumPrefix { get; set; }
    [MaxLength(32)] public string MatchStatus { get; set; } = FaceRecognitionMatchStatuses.InvalidRuntimeEvent;
    public Guid SyncRunId { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public Employee? Employee { get; set; }
    public EmployeeFaceModel? EmployeeFaceModel { get; set; }
    public FaceCameraConfiguration? FaceCameraConfiguration { get; set; }
    public Lane? Lane { get; set; }
}

public static class FaceRecognitionMatchStatuses
{
    public const string Matched = "Matched";
    public const string EmployeeMissing = "EmployeeMissing";
    public const string ModelMissing = "ModelMissing";
    public const string ModelMismatch = "ModelMismatch";
    public const string CameraUnmanaged = "CameraUnmanaged";
    public const string IgnoredUnknown = "IgnoredUnknown";
    public const string InvalidRuntimeEvent = "InvalidRuntimeEvent";
}

public sealed class FaceRecognitionCollectorCheckpoint
{
    [Key, MaxLength(64)] public string CameraId { get; set; } = string.Empty;
    public long RuntimeSessionGeneration { get; set; }
    public long LastSequence { get; set; }
    public DateTime? LastEventOccurredAtUtc { get; set; }
    public DateTime LastPollAtUtc { get; set; }
    public DateTime? LastSuccessAtUtc { get; set; }
    [MaxLength(80)] public string? LastErrorCode { get; set; }
    [MaxLength(500)] public string? LastErrorMessage { get; set; }
    public DateTime? GapDetectedAtUtc { get; set; }
    [Timestamp] public byte[] RowVersion { get; set; } = Array.Empty<byte>();
}
