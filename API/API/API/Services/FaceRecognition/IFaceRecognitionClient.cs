namespace API.Services.FaceRecognition;

public interface IFaceRecognitionClient
{
    Task<FaceRuntimeResponse> GetCamerasAsync(CancellationToken cancellationToken);

    Task<FaceRuntimeResponse> StartCameraAsync(
        string cameraId,
        FaceCameraStartRequest request,
        CancellationToken cancellationToken);

    Task<FaceRuntimeResponse> StopCameraAsync(
        string cameraId,
        CancellationToken cancellationToken);

    Task<FaceRuntimeResponse> ResetCameraAsync(
        string cameraId,
        CancellationToken cancellationToken);

    Task<FaceRuntimeResponse> GetCameraStatusAsync(
        string cameraId,
        CancellationToken cancellationToken);

    Task<FaceRuntimeResponse> GetRecognitionResultAsync(
        string cameraId,
        CancellationToken cancellationToken);

    Task<FaceRuntimeResponse> GetLockedImagesAsync(
        string cameraId,
        CancellationToken cancellationToken);

    Task<FaceRuntimeResponse> StartCameraAsync(
        FaceCameraStartRequest request,
        CancellationToken cancellationToken);

    Task<FaceRuntimeResponse> StopCameraAsync(CancellationToken cancellationToken);

    Task<FaceRuntimeResponse> ResetCameraAsync(CancellationToken cancellationToken);

    Task<FaceRuntimeResponse> GetCameraStatusAsync(CancellationToken cancellationToken);

    Task<FaceRuntimeResponse> GetRecognitionResultAsync(CancellationToken cancellationToken);

    Task<FaceRuntimeResponse> GetLockedImagesAsync(CancellationToken cancellationToken);

    Task<FaceRuntimeResponse> GetModelsAsync(CancellationToken cancellationToken);

    Task<FaceRuntimeResponse> ReloadModelsAsync(CancellationToken cancellationToken);
    Task<FaceCameraEventsRuntimeResult> GetCameraEventsAsync(
        string cameraId, long afterSequence, long? sessionGeneration,
        int limit, CancellationToken cancellationToken) => throw new NotSupportedException();
    Task<FaceRuntimeResponse> PrepareEnrollmentAsync(Guid jobId, FacePrepareEnrollmentRequest request, CancellationToken cancellationToken) =>
        throw new NotSupportedException();
    Task<FaceRuntimeResponse> ActivateEnrollmentAsync(Guid jobId, FaceActivateEnrollmentRequest request, CancellationToken cancellationToken) =>
        throw new NotSupportedException();
    Task<FaceRuntimeResponse> DiscardEnrollmentAsync(Guid jobId, CancellationToken cancellationToken) =>
        throw new NotSupportedException();
    Task<FaceRuntimeResponse> RevokeSubjectModelAsync(string subjectId, CancellationToken cancellationToken) =>
        throw new NotSupportedException();
}

public sealed record FacePrepareEnrollmentRequest(string SubjectId, string SourceReference);
public sealed record FaceActivateEnrollmentRequest(
    string SubjectId, int Version, string ExpectedChecksum, string ExpectedModelFileName);
