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

    public async Task<FaceCameraEventsRuntimeResult> GetCameraEventsAsync(
        string cameraId, long afterSequence, long? sessionGeneration,
        int limit, CancellationToken cancellationToken)
    {
        var id = FaceCameraIdValidator.Validate(cameraId);
        var query = new Dictionary<string, string?> {
            ["afterSequence"] = afterSequence.ToString(System.Globalization.CultureInfo.InvariantCulture),
            ["limit"] = limit.ToString(System.Globalization.CultureInfo.InvariantCulture),
            ["sessionGeneration"] = sessionGeneration?.ToString(System.Globalization.CultureInfo.InvariantCulture)
        };
        var path = Microsoft.AspNetCore.WebUtilities.QueryHelpers.AddQueryString(
            $"cameras/{Uri.EscapeDataString(id)}/events", query);
        var response = await SendAsync(HttpMethod.Get, path, null, cancellationToken);
        FaceCameraEventsResponse? payload = null;
        if ((int)response.StatusCode is >= 200 and < 300)
            payload = JsonSerializer.Deserialize<FaceCameraEventsResponse>(
                response.Body, new JsonSerializerOptions(JsonSerializerDefaults.Web));
        return new(response.StatusCode, payload);
    }

    public Task<FaceRuntimeResponse> PrepareEnrollmentAsync(Guid jobId, FacePrepareEnrollmentRequest request, CancellationToken cancellationToken) =>
        SendAsync(HttpMethod.Post, $"enrollments/{jobId:D}/prepare",
            JsonContent.Create(request, options: RequestJsonOptions), cancellationToken);

    public Task<FaceRuntimeResponse> ActivateEnrollmentAsync(Guid jobId, FaceActivateEnrollmentRequest request, CancellationToken cancellationToken) =>
        SendAsync(HttpMethod.Post, $"enrollments/{jobId:D}/activate",
            JsonContent.Create(request, options: RequestJsonOptions), cancellationToken);

    public Task<FaceRuntimeResponse> DiscardEnrollmentAsync(Guid jobId, CancellationToken cancellationToken) =>
        SendAsync(HttpMethod.Post, $"enrollments/{jobId:D}/discard", null, cancellationToken);

    public Task<FaceRuntimeResponse> RevokeSubjectModelAsync(string subjectId, CancellationToken cancellationToken) =>
        SendAsync(HttpMethod.Post, $"models/subjects/{Uri.EscapeDataString(subjectId)}/revoke", null, cancellationToken);

    public Task<FaceRuntimeResponse> LiveEnrollAsync(string subjectId, IReadOnlyList<string> images, CancellationToken cancellationToken) =>
        SendAsync(HttpMethod.Post, "enrollments/live",
            JsonContent.Create(new FaceLiveEnrollRequest(subjectId, images), options: RequestJsonOptions),
            cancellationToken);

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
