using System.Net;

namespace API.Services.FaceRecognition;

public sealed class FaceCameraStartRequest
{
    public string Ip { get; init; } = string.Empty;

    public string? LaneId { get; init; }
}

public sealed record FaceRuntimeResponse(
    HttpStatusCode StatusCode,
    string Body,
    string? ContentType);

public sealed record FaceRuntimeRecognitionEvent(
    string EventId, string CameraId, string? LaneId, long Sequence,
    long SessionGeneration, string EventType, string? SubjectId,
    DateTime OccurredAtUtc, double? Distance, long? ModelRegistryVersion,
    string? ModelFileName, string? ModelChecksumPrefix);

public sealed record FaceCameraEventsResponse(
    string CameraId, long SessionGeneration, long? OldestSequence,
    long LatestSequence, IReadOnlyList<FaceRuntimeRecognitionEvent> Events,
    bool HasMore, bool GapDetected);

public sealed record FaceCameraEventsRuntimeResult(
    HttpStatusCode StatusCode, FaceCameraEventsResponse? Payload);

public enum FaceRuntimeFailureKind
{
    ConnectionFailure,
    Timeout,
    UnexpectedFailure
}

public sealed class FaceRuntimeUnavailableException : Exception
{
    public FaceRuntimeUnavailableException(
        FaceRuntimeFailureKind failureKind,
        string message,
        Exception innerException)
        : base(message, innerException)
    {
        FailureKind = failureKind;
    }

    public FaceRuntimeFailureKind FailureKind { get; }
}
