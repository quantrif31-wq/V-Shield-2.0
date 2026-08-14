using API.Middleware;
using API.Services;
using API.Services.FaceRecognition;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
[RequireOperationalTask("monitoring")]
public class FaceCameraController : ControllerBase
{
    private readonly IFaceRecognitionClient _faceRecognitionClient;

    public FaceCameraController(IFaceRecognitionClient faceRecognitionClient)
    {
        _faceRecognitionClient = faceRecognitionClient;
    }

    [HttpGet("cameras")]
    public Task<IActionResult> GetCameras(CancellationToken cancellationToken) =>
        ProxyAsync(_faceRecognitionClient.GetCamerasAsync, cancellationToken);

    [HttpPost("cameras/{cameraId}/start")]
    public Task<IActionResult> StartCamera(
        string cameraId,
        [FromBody] FaceCameraStartRequest request,
        CancellationToken cancellationToken) =>
        ProxyCameraAsync(
            cameraId,
            (validId, token) =>
                _faceRecognitionClient.StartCameraAsync(validId, request, token),
            cancellationToken);

    [HttpPost("cameras/{cameraId}/stop")]
    public Task<IActionResult> StopCamera(
        string cameraId,
        CancellationToken cancellationToken) =>
        ProxyCameraAsync(
            cameraId,
            _faceRecognitionClient.StopCameraAsync,
            cancellationToken);

    [HttpPost("cameras/{cameraId}/reset")]
    public Task<IActionResult> ResetCamera(
        string cameraId,
        CancellationToken cancellationToken) =>
        ProxyCameraAsync(
            cameraId,
            _faceRecognitionClient.ResetCameraAsync,
            cancellationToken);

    [HttpGet("cameras/{cameraId}/status")]
    public Task<IActionResult> GetCameraStatus(
        string cameraId,
        CancellationToken cancellationToken) =>
        ProxyCameraAsync(
            cameraId,
            _faceRecognitionClient.GetCameraStatusAsync,
            cancellationToken);

    [HttpGet("cameras/{cameraId}/result")]
    public Task<IActionResult> GetCameraResult(
        string cameraId,
        CancellationToken cancellationToken) =>
        ProxyCameraAsync(
            cameraId,
            _faceRecognitionClient.GetRecognitionResultAsync,
            cancellationToken);

    [HttpGet("cameras/{cameraId}/locked-images")]
    public Task<IActionResult> GetLockedImages(
        string cameraId,
        CancellationToken cancellationToken) =>
        ProxyCameraAsync(
            cameraId,
            _faceRecognitionClient.GetLockedImagesAsync,
            cancellationToken);

    [HttpPost("camera/on")]
    public Task<IActionResult> TurnOnCamera(
        [FromBody] FaceCameraStartRequest request,
        CancellationToken cancellationToken) =>
        ProxyAsync(
            token => _faceRecognitionClient.StartCameraAsync(request, token),
            cancellationToken);

    [HttpPost("camera/off")]
    public Task<IActionResult> TurnOffCamera(CancellationToken cancellationToken) =>
        ProxyAsync(_faceRecognitionClient.StopCameraAsync, cancellationToken);

    [HttpPost("camera/reset")]
    public Task<IActionResult> ResetCameraState(CancellationToken cancellationToken) =>
        ProxyAsync(_faceRecognitionClient.ResetCameraAsync, cancellationToken);

    [HttpGet("camera/status")]
    public Task<IActionResult> GetCameraStatus(CancellationToken cancellationToken) =>
        ProxyAsync(_faceRecognitionClient.GetCameraStatusAsync, cancellationToken);

    [HttpGet("camera/result")]
    public Task<IActionResult> GetCameraResult(CancellationToken cancellationToken) =>
        ProxyAsync(_faceRecognitionClient.GetRecognitionResultAsync, cancellationToken);

    [HttpGet("camera/locked-images")]
    public Task<IActionResult> GetLockedImages(CancellationToken cancellationToken) =>
        ProxyAsync(_faceRecognitionClient.GetLockedImagesAsync, cancellationToken);

    [HttpGet("models")]
    public Task<IActionResult> GetModels(CancellationToken cancellationToken) =>
        ProxyAsync(_faceRecognitionClient.GetModelsAsync, cancellationToken);

    [HttpGet("discover-ipwebcam")]
    public async Task<IActionResult> DiscoverIpWebcam(
        [FromServices] ILocalNetworkCameraDiscoveryService cameraDiscoveryService,
        CancellationToken cancellationToken)
    {
        var cameras = await cameraDiscoveryService.DiscoverIpWebcamsAsync(cancellationToken);

        return Ok(new
        {
            count = cameras.Count,
            cameras
        });
    }

    [HttpPost("models/reload")]
    public Task<IActionResult> ReloadModels(CancellationToken cancellationToken)
    {
        if ((Request.ContentLength ?? 0) > 0 || Request.Headers.TransferEncoding.Count > 0)
        {
            return Task.FromResult<IActionResult>(
                BadRequest(new { message = "Không được phép gửi nội dung yêu cầu." }));
        }

        return ProxyAsync(_faceRecognitionClient.ReloadModelsAsync, cancellationToken);
    }

    [HttpPost("enroll-live")]
    public Task<IActionResult> LiveEnroll(
        [FromBody] FaceLiveEnrollRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.SubjectId) ||
            request.Images is null ||
            request.Images.Count == 0 ||
            request.Images.Count > 200)
        {
            return Task.FromResult<IActionResult>(
                BadRequest(new { message = "Vui lòng nhập SubjectId và từ 1 đến 200 ảnh." }));
        }

        return ProxyAsync(
            token => _faceRecognitionClient.LiveEnrollAsync(
                request.SubjectId, request.Images, token),
            cancellationToken);
    }

    [HttpPost("guided/start")]
    public Task<IActionResult> GuidedEnrollStart(
        [FromBody] FaceGuidedEnrollStartRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.StreamUrl))
        {
            return Task.FromResult<IActionResult>(
                BadRequest(new { message = "Vui lòng chọn camera để bắt đầu thu thập mẫu." }));
        }

        return ProxyAsync(
            token => _faceRecognitionClient.GuidedEnrollStartAsync(
                request.StreamUrl, request.PoseMode, token),
            cancellationToken);
    }

    [HttpGet("guided/progress")]
    public Task<IActionResult> GuidedEnrollProgress(CancellationToken cancellationToken) =>
        ProxyAsync(_faceRecognitionClient.GuidedEnrollProgressAsync, cancellationToken);

    [HttpPost("guided/stop")]
    public Task<IActionResult> GuidedEnrollStop(CancellationToken cancellationToken) =>
        ProxyAsync(_faceRecognitionClient.GuidedEnrollStopAsync, cancellationToken);

    [HttpPost("guided/confirm")]
    public Task<IActionResult> GuidedEnrollConfirm(
        [FromBody] FaceGuidedEnrollConfirmRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.SubjectId))
        {
            return Task.FromResult<IActionResult>(
                BadRequest(new { message = "Vui lòng nhập mã đối tượng để xác nhận." }));
        }

        return ProxyAsync(
            token => _faceRecognitionClient.GuidedEnrollConfirmAsync(
                request.SubjectId, token),
            cancellationToken);
    }

    private async Task<IActionResult> ProxyAsync(
        Func<CancellationToken, Task<FaceRuntimeResponse>> operation,
        CancellationToken cancellationToken)
    {
        try
        {
            var response = await operation(cancellationToken);

            return new ContentResult
            {
                StatusCode = (int)response.StatusCode,
                Content = response.Body,
                ContentType = response.ContentType ?? "application/json"
            };
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (FaceRuntimeUnavailableException ex)
        {
            return StatusCode(
                StatusCodes.Status503ServiceUnavailable,
                new { message = $"Khong the ket noi toi Face camera service: {ex.Message}" });
        }
    }

    private Task<IActionResult> ProxyCameraAsync(
        string cameraId,
        Func<string, CancellationToken, Task<FaceRuntimeResponse>> operation,
        CancellationToken cancellationToken)
    {
        if (!FaceCameraIdValidator.TryValidate(cameraId, out var validCameraId))
        {
            return Task.FromResult<IActionResult>(
                BadRequest(new { message = "cameraId không hợp lệ." }));
        }

        return ProxyAsync(
            token => operation(validCameraId, token),
            cancellationToken);
    }
}
