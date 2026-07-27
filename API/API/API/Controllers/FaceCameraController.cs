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
                BadRequest(new { message = "Request body is not allowed." }));
        }

        return ProxyAsync(_faceRecognitionClient.ReloadModelsAsync, cancellationToken);
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
                ContentType = "application/json"
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
}
