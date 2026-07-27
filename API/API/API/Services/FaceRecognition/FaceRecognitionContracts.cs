using System.Net;

namespace API.Services.FaceRecognition;

public sealed class FaceCameraStartRequest
{
    public string Ip { get; init; } = string.Empty;
}

public sealed record FaceRuntimeResponse(
    HttpStatusCode StatusCode,
    string Body,
    string? ContentType);

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
