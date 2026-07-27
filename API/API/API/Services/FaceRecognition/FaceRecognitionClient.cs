using System.Net.Http.Json;
using System.Text.Json;

namespace API.Services.FaceRecognition;

public sealed class FaceRecognitionClient : IFaceRecognitionClient
{
    private static readonly JsonSerializerOptions RequestJsonOptions =
        new(JsonSerializerDefaults.Web);

    private readonly HttpClient _httpClient;

    public FaceRecognitionClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public Task<FaceRuntimeResponse> GetCamerasAsync(
        CancellationToken cancellationToken) =>
        SendAsync(HttpMethod.Get, "cameras", null, cancellationToken);

    public Task<FaceRuntimeResponse> StartCameraAsync(
        string cameraId,
        FaceCameraStartRequest request,
        CancellationToken cancellationToken) =>
        SendAsync(
            HttpMethod.Post,
            CameraPath(cameraId, "start"),
            JsonContent.Create(request, options: RequestJsonOptions),
            cancellationToken);

    public Task<FaceRuntimeResponse> StopCameraAsync(
        string cameraId,
        CancellationToken cancellationToken) =>
        SendAsync(
            HttpMethod.Post,
            CameraPath(cameraId, "stop"),
            null,
            cancellationToken);

    public Task<FaceRuntimeResponse> ResetCameraAsync(
        string cameraId,
        CancellationToken cancellationToken) =>
        SendAsync(
            HttpMethod.Post,
            CameraPath(cameraId, "reset"),
            null,
            cancellationToken);

    public Task<FaceRuntimeResponse> GetCameraStatusAsync(
        string cameraId,
        CancellationToken cancellationToken) =>
        SendAsync(
            HttpMethod.Get,
            CameraPath(cameraId, "status"),
            null,
            cancellationToken);

    public Task<FaceRuntimeResponse> GetRecognitionResultAsync(
        string cameraId,
        CancellationToken cancellationToken) =>
        SendAsync(
            HttpMethod.Get,
            CameraPath(cameraId, "result"),
            null,
            cancellationToken);

    public Task<FaceRuntimeResponse> GetLockedImagesAsync(
        string cameraId,
        CancellationToken cancellationToken) =>
        SendAsync(
            HttpMethod.Get,
            CameraPath(cameraId, "locked-images"),
            null,
            cancellationToken);

    public Task<FaceRuntimeResponse> StartCameraAsync(
        FaceCameraStartRequest request,
        CancellationToken cancellationToken) =>
        StartCameraAsync("default", request, cancellationToken);

    public Task<FaceRuntimeResponse> StopCameraAsync(CancellationToken cancellationToken) =>
        StopCameraAsync("default", cancellationToken);

    public Task<FaceRuntimeResponse> ResetCameraAsync(CancellationToken cancellationToken) =>
        ResetCameraAsync("default", cancellationToken);

    public Task<FaceRuntimeResponse> GetCameraStatusAsync(CancellationToken cancellationToken) =>
        GetCameraStatusAsync("default", cancellationToken);

    public Task<FaceRuntimeResponse> GetRecognitionResultAsync(CancellationToken cancellationToken) =>
        GetRecognitionResultAsync("default", cancellationToken);

    public Task<FaceRuntimeResponse> GetLockedImagesAsync(CancellationToken cancellationToken) =>
        GetLockedImagesAsync("default", cancellationToken);

    public Task<FaceRuntimeResponse> GetModelsAsync(CancellationToken cancellationToken) =>
        SendAsync(HttpMethod.Get, "models", null, cancellationToken);

    public Task<FaceRuntimeResponse> ReloadModelsAsync(CancellationToken cancellationToken) =>
        SendAsync(HttpMethod.Post, "models/reload", null, cancellationToken);

    private static string CameraPath(string cameraId, string operation)
    {
        var validCameraId = FaceCameraIdValidator.Validate(cameraId);
        return $"cameras/{Uri.EscapeDataString(validCameraId)}/{operation}";
    }

    private async Task<FaceRuntimeResponse> SendAsync(
        HttpMethod method,
        string relativePath,
        HttpContent? content,
        CancellationToken cancellationToken)
    {
        try
        {
            using var request = new HttpRequestMessage(method, relativePath)
            {
                Content = content
            };
            using var response = await _httpClient.SendAsync(
                request,
                HttpCompletionOption.ResponseHeadersRead,
                cancellationToken);
            var body = await response.Content.ReadAsStringAsync(cancellationToken);

            return new FaceRuntimeResponse(
                response.StatusCode,
                body,
                response.Content.Headers.ContentType?.ToString());
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (OperationCanceledException ex)
        {
            throw new FaceRuntimeUnavailableException(
                FaceRuntimeFailureKind.Timeout,
                ex.Message,
                ex);
        }
        catch (HttpRequestException ex)
        {
            throw new FaceRuntimeUnavailableException(
                FaceRuntimeFailureKind.ConnectionFailure,
                ex.Message,
                ex);
        }
        catch (Exception ex)
        {
            throw new FaceRuntimeUnavailableException(
                FaceRuntimeFailureKind.UnexpectedFailure,
                "Unexpected Face Runtime failure.",
                ex);
        }
        finally
        {
            content?.Dispose();
        }
    }
}
