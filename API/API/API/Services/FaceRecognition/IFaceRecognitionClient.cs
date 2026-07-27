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
}
