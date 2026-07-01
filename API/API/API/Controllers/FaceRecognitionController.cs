using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Text;
using API.Middleware;
using API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;

namespace API.Controllers;

[Route("api/face-recognition")]
[ApiController]
[Authorize]
[RequireOperationalTask("monitoring")]
public class FaceRecognitionController : ControllerBase
{
    private static readonly HttpClient client = new HttpClient();

    private static Process? pythonProcess;
    private static readonly StringBuilder pythonLogBuffer = new StringBuilder();

    private readonly ILocalNetworkCameraDiscoveryService _lanCameraDiscoveryService;
    private readonly IConfiguration _configuration;
    private readonly string _pythonApiBaseUrl;
    private readonly Uri _pythonApiUri;
    private readonly bool _autoStartFaceId;
    private readonly string _pythonFolder;

    public FaceRecognitionController(ILocalNetworkCameraDiscoveryService localNetworkCameraDiscoveryService, IConfiguration configuration)
    {
        _lanCameraDiscoveryService = localNetworkCameraDiscoveryService;
        _configuration = configuration;
        _pythonApiBaseUrl = ResolveNormalizedServiceBaseUrl(
            configuration["AiServices:FaceIdBaseUrl"],
            "http://127.0.0.1:8000"
        );
        _pythonApiUri = new Uri(_pythonApiBaseUrl);
        _autoStartFaceId = configuration.GetValue("AiServices:AutoStartFaceId", true);
        _pythonFolder = ResolveFaceIdPythonFolder();
    }

    private async Task EnsureFaceIdServiceRunningAsync()
    {
        if (await IsFaceIdServiceAvailableAsync())
            return;

        if (!_autoStartFaceId)
            throw new InvalidOperationException($"Khong the ket noi toi FaceID service tai {_pythonApiBaseUrl}.");

        StartFaceIdPythonServer();

        for (int attempt = 0; attempt < 10; attempt++)
        {
            if (pythonProcess != null && pythonProcess.HasExited)
            {
                var log = pythonLogBuffer.ToString().Trim();
                throw new InvalidOperationException(
                    string.IsNullOrWhiteSpace(log)
                        ? "Python FaceID server da thoat truoc khi khoi dong xong."
                        : $"Python FaceID server khoi dong that bai: {log}"
                );
            }

            if (await IsFaceIdServiceAvailableAsync())
                return;

            await Task.Delay(1000);
        }

        throw new InvalidOperationException($"Khong the ket noi toi Python FaceID server tai {_pythonApiBaseUrl}.");
    }

    private async Task<bool> IsFaceIdServiceAvailableAsync()
    {
        try
        {
            var res = await client.GetAsync(BuildFaceIdServiceUrl("/"));
            return res.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    private string BuildFaceIdServiceUrl(string relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath))
            return _pythonApiBaseUrl;

        return $"{_pythonApiBaseUrl}{relativePath}";
    }

    private static string ResolveNormalizedServiceBaseUrl(string? configuredValue, string fallbackValue) =>
        (configuredValue ?? fallbackValue).Trim().TrimEnd('/');

    private string ResolveFaceIdPythonFolder()
    {
        var aiRootFolderName = _configuration["RuntimePaths:AiRootFolderName"] ?? "AI_Project";
        var candidateFolders = new[]
        {
            Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", aiRootFolderName, "face_recognition")),
            Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "..", aiRootFolderName, "face_recognition")),
            Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), aiRootFolderName, "face_recognition"))
        };

        return candidateFolders.FirstOrDefault(Directory.Exists) ?? candidateFolders[0];
    }

    private static string ResolvePythonExecutablePath(string pythonFolder)
    {
        var candidates = OperatingSystem.IsWindows()
            ? new[]
            {
                Path.Combine(pythonFolder, "venv", "Scripts", "python.exe"),
                "python",
                "py"
            }
            : new[]
            {
                Path.Combine(pythonFolder, "venv", "bin", "python"),
                "python3",
                "python"
            };

        foreach (var candidate in candidates)
        {
            if (!Path.IsPathRooted(candidate) || System.IO.File.Exists(candidate))
            {
                return candidate;
            }
        }

        return candidates[0];
    }

    private void StartFaceIdPythonServer()
    {
        if (pythonProcess != null && !pythonProcess.HasExited)
            return;

        pythonLogBuffer.Clear();
        var pythonExe = ResolvePythonExecutablePath(_pythonFolder);

        if (Path.IsPathRooted(pythonExe) && !System.IO.File.Exists(pythonExe))
            throw new FileNotFoundException($"Khong tim thay Python venv: {pythonExe}");

        ProcessStartInfo psi = new ProcessStartInfo
        {
            FileName = pythonExe,
            WorkingDirectory = _pythonFolder,
            Arguments = $"-m uvicorn FaceID:app --host {ResolveUvicornBindHost(_pythonApiUri.Host)} --port {_pythonApiUri.Port}",
            CreateNoWindow = true,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };

        pythonProcess = Process.Start(psi);

        if (pythonProcess == null)
            throw new InvalidOperationException("Khong the khoi dong Python FaceID process.");

        pythonProcess.OutputDataReceived += (_, e) =>
        {
            if (!string.IsNullOrWhiteSpace(e.Data))
                pythonLogBuffer.AppendLine(e.Data);
        };
        pythonProcess.ErrorDataReceived += (_, e) =>
        {
            if (!string.IsNullOrWhiteSpace(e.Data))
                pythonLogBuffer.AppendLine(e.Data);
        };
        pythonProcess.BeginOutputReadLine();
        pythonProcess.BeginErrorReadLine();
    }

    private static string ResolveUvicornBindHost(string host)
    {
        if (string.Equals(host, "localhost", StringComparison.OrdinalIgnoreCase))
            return "127.0.0.1";

        return IPAddress.TryParse(host, out _) ? host : "127.0.0.1";
    }

    private void StopFaceIdPythonServer()
    {
        try
        {
            if (pythonProcess != null && !pythonProcess.HasExited)
            {
                pythonProcess.Kill(true);
                pythonProcess.Dispose();
                pythonProcess = null;
            }
        }
        catch { }
    }

    [HttpGet("status")]
    public async Task<IActionResult> Status()
    {
        try
        {
            await EnsureFaceIdServiceRunningAsync();

            var res = await client.GetAsync(BuildFaceIdServiceUrl("/camera/status"));
            var data = await res.Content.ReadAsStringAsync();

            return Content(data, "application/json");
        }
        catch (Exception ex)
        {
            return StatusCode(503, new { message = ex.Message });
        }
    }

    [HttpPost("start")]
    public async Task<IActionResult> StartCamera(string ip)
    {
        try
        {
            await EnsureFaceIdServiceRunningAsync();

            var url = QueryHelpers.AddQueryString(BuildFaceIdServiceUrl("/camera/start"), "ip_url", ip);
            var res = await client.PostAsync(url, null);
            var data = await res.Content.ReadAsStringAsync();

            return new ContentResult
            {
                StatusCode = (int)res.StatusCode,
                Content = data,
                ContentType = "application/json"
            };
        }
        catch (Exception ex)
        {
            return StatusCode(503, new { message = ex.Message });
        }
    }

    [HttpPost("stop")]
    public async Task<IActionResult> StopCamera()
    {
        try
        {
            await EnsureFaceIdServiceRunningAsync();

            var res = await client.PostAsync(BuildFaceIdServiceUrl("/camera/stop"), null);
            var data = await res.Content.ReadAsStringAsync();

            return new ContentResult
            {
                StatusCode = (int)res.StatusCode,
                Content = data,
                ContentType = "application/json"
            };
        }
        catch (Exception ex)
        {
            return StatusCode(503, new { message = ex.Message });
        }
    }

    [HttpGet("discover-ipwebcam")]
    public async Task<IActionResult> DiscoverIpWebcam(CancellationToken cancellationToken)
    {
        var cameras = await _lanCameraDiscoveryService.DiscoverIpWebcamsAsync(cancellationToken);

        return Ok(new
        {
            count = cameras.Count,
            cameras
        });
    }

    [HttpPost("shutdown")]
    public IActionResult ShutdownPython()
    {
        StopFaceIdPythonServer();

        return Ok(new
        {
            message = "Python server stopped"
        });
    }
}



