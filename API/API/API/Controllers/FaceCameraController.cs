using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[Route("api/[controller]")]
[ApiController]
public class FaceCameraController : ControllerBase
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly string _serviceBaseUrl;

    public FaceCameraController(IHttpClientFactory httpClientFactory, IConfiguration configuration)
    {
        _httpClientFactory = httpClientFactory;
        _serviceBaseUrl = ResolveNormalizedServiceBaseUrl(
            configuration["AiServices:FaceCameraBaseUrl"],
            "http://127.0.0.1:5001/api"
        );
    }

    [HttpPost("camera/on")]
    public Task<IActionResult> TurnOnCamera([FromBody] CameraOnRequest request) =>
        ProxyPostJsonToServiceAsync("/camera/on", request);

    [HttpPost("camera/off")]
    public Task<IActionResult> TurnOffCamera() =>
        ProxyPostToServiceAsync("/camera/off");

    [HttpPost("camera/reset")]
    public Task<IActionResult> ResetCameraState() =>
        ProxyPostToServiceAsync("/camera/reset");

    [HttpGet("camera/status")]
    public Task<IActionResult> GetCameraStatus() =>
        ProxyGetFromServiceAsync("/camera/status");

    [HttpGet("camera/result")]
    public Task<IActionResult> GetCameraResult() =>
        ProxyGetFromServiceAsync("/camera/result");

    [HttpGet("camera/locked-images")]
    public Task<IActionResult> GetLockedImages() =>
        ProxyGetFromServiceAsync("/camera/locked-images");

    private async Task<IActionResult> ProxyGetFromServiceAsync(string relativePath)
    {
        try
        {
            var client = _httpClientFactory.CreateClient();
            var response = await client.GetAsync(BuildServiceEndpointUrl(relativePath));
            var content = await response.Content.ReadAsStringAsync();

            return new ContentResult
            {
                StatusCode = (int)response.StatusCode,
                Content = content,
                ContentType = "application/json"
            };
        }
        catch (Exception ex)
        {
            return StatusCode(503, new { message = $"Khong the ket noi toi Face camera service: {ex.Message}" });
        }
    }

    private async Task<IActionResult> ProxyPostToServiceAsync(string relativePath)
    {
        try
        {
            var client = _httpClientFactory.CreateClient();
            var response = await client.PostAsync(BuildServiceEndpointUrl(relativePath), null);
            var content = await response.Content.ReadAsStringAsync();

            return new ContentResult
            {
                StatusCode = (int)response.StatusCode,
                Content = content,
                ContentType = "application/json"
            };
        }
        catch (Exception ex)
        {
            return StatusCode(503, new { message = $"Khong the ket noi toi Face camera service: {ex.Message}" });
        }
    }

    private async Task<IActionResult> ProxyPostJsonToServiceAsync<TRequest>(string relativePath, TRequest request)
    {
        try
        {
            var client = _httpClientFactory.CreateClient();
            var response = await client.PostAsJsonAsync(BuildServiceEndpointUrl(relativePath), request);
            var content = await response.Content.ReadAsStringAsync();

            return new ContentResult
            {
                StatusCode = (int)response.StatusCode,
                Content = content,
                ContentType = "application/json"
            };
        }
        catch (Exception ex)
        {
            return StatusCode(503, new { message = $"Khong the ket noi toi Face camera service: {ex.Message}" });
        }
    }

    private string BuildServiceEndpointUrl(string relativePath) => $"{_serviceBaseUrl}{relativePath}";

    private static string ResolveNormalizedServiceBaseUrl(string? configuredValue, string fallbackValue) =>
        (configuredValue ?? fallbackValue).Trim().TrimEnd('/');

    public sealed class CameraOnRequest
    {
        public string Ip { get; set; } = string.Empty;
    }
}

