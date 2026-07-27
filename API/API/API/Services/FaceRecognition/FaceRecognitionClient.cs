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

    public Task<FaceRuntimeResponse> StartCameraAsync(
        FaceCameraStartRequest request,
        CancellationToken cancellationToken) =>
        SendAsync(
            HttpMethod.Post,
            "camera/on",
            JsonContent.Create(request, options: RequestJsonOptions),
            cancellationToken);

    public Task<FaceRuntimeResponse> StopCameraAsync(CancellationToken cancellationToken) =>
        SendAsync(HttpMethod.Post, "camera/off", null, cancellationToken);

    public Task<FaceRuntimeResponse> ResetCameraAsync(CancellationToken cancellationToken) =>
        SendAsync(HttpMethod.Post, "camera/reset", null, cancellationToken);

    public Task<FaceRuntimeResponse> GetCameraStatusAsync(CancellationToken cancellationToken) =>
        SendAsync(HttpMethod.Get, "camera/status", null, cancellationToken);

    public Task<FaceRuntimeResponse> GetRecognitionResultAsync(CancellationToken cancellationToken) =>
        SendAsync(HttpMethod.Get, "camera/result", null, cancellationToken);

    public Task<FaceRuntimeResponse> GetLockedImagesAsync(CancellationToken cancellationToken) =>
        SendAsync(HttpMethod.Get, "camera/locked-images", null, cancellationToken);

    public Task<FaceRuntimeResponse> GetModelsAsync(CancellationToken cancellationToken) =>
        SendAsync(HttpMethod.Get, "models", null, cancellationToken);

    public Task<FaceRuntimeResponse> ReloadModelsAsync(CancellationToken cancellationToken) =>
        SendAsync(HttpMethod.Post, "models/reload", null, cancellationToken);

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
