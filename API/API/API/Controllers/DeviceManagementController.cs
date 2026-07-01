using API.Data;
using API.Middleware;
using API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using System.Text;

namespace API.Controllers;

[ApiController]
[Route("api/device-management")]
[Authorize]
[RequireOperationalTask("device-mgmt")]
public class DeviceManagementController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly IConfiguration _configuration;
    private readonly IHttpClientFactory _httpClientFactory;

    public DeviceManagementController(ApplicationDbContext context, IConfiguration configuration, IHttpClientFactory httpClientFactory)
    {
        _context = context;
        _configuration = configuration;
        _httpClientFactory = httpClientFactory;
    }

    [HttpGet("overview")]
    public async Task<IActionResult> GetOverview()
    {
        // DbContext is scoped per request and is not safe for concurrent EF operations.
        var cameraCount = await _context.Cameras.AsNoTracking().CountAsync();
        var gateCount = await _context.Gates.AsNoTracking().CountAsync();
        var linkedCameraCount = await _context.Cameras.AsNoTracking().CountAsync(camera => camera.GateId != null);

        var cameras = await BuildCameraProjectionQuery()
            .OrderBy(camera => camera.CameraName)
            .ToListAsync();

        var gates = await _context.Gates.AsNoTracking()
            .OrderBy(gate => gate.GateName)
            .Select(gate => new
            {
                gate.GateId,
                gate.GateName,
                gate.Location,
                cameraCount = gate.Cameras.Count(),
                accessLogCount = gate.AccessLogs.Count(),
                lastAccessAt = gate.AccessLogs
                    .OrderByDescending(log => log.Timestamp)
                    .Select(log => (DateTime?)log.Timestamp)
                    .FirstOrDefault()
            })
            .ToListAsync();

        return Ok(new
        {
            generatedAt = DateTime.Now,
            summary = new
            {
                camerasConfigured = cameraCount,
                gatesConfigured = gateCount,
                camerasLinkedToGate = linkedCameraCount,
                unassignedCameras = cameraCount - linkedCameraCount
            },
            cameras,
            gates
        });
    }

    [HttpGet("cameras")]
    public async Task<IActionResult> GetCameras([FromQuery] string? query = null, [FromQuery] int? gateId = null)
    {
        var camerasQuery = BuildCameraProjectionQuery();

        if (!string.IsNullOrWhiteSpace(query))
        {
            var normalized = query.Trim();
            camerasQuery = camerasQuery.Where(camera =>
                camera.CameraName.Contains(normalized) ||
                (camera.CameraType != null && camera.CameraType.Contains(normalized)) ||
                (camera.GateName != null && camera.GateName.Contains(normalized)));
        }

        if (gateId.HasValue)
        {
            camerasQuery = camerasQuery.Where(camera => camera.GateId == gateId.Value);
        }

        var items = await camerasQuery
            .OrderBy(camera => camera.CameraName)
            .ToListAsync();

        return Ok(items);
    }

    [HttpPost("cameras")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> CreateCamera([FromBody] UpsertCameraRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.CameraName))
        {
            return BadRequest(new { message = "Tên camera là bắt buộc" });
        }

        if (request.GateId.HasValue && !await _context.Gates.AnyAsync(gate => gate.GateId == request.GateId.Value))
        {
            return BadRequest(new { message = "Cổng được chọn không tồn tại" });
        }

        var camera = new Camera
        {
            CameraName = request.CameraName.Trim(),
            CameraType = NormalizeOptionalText(request.CameraType),
            GateId = request.GateId,
            StreamUrl = NormalizeOptionalText(request.StreamUrl)
        };

        _context.Cameras.Add(camera);
        await _context.SaveChangesAsync();
        camera.UrlView = BuildCameraWebViewUrl(camera.StreamUrl, camera.CameraId);
        await _context.SaveChangesAsync();
        await TryReloadGo2RtcRuntimeAsync();

        return CreatedAtAction(nameof(GetCameras), new { id = camera.CameraId }, new
        {
            camera.CameraId,
            camera.CameraName,
            camera.CameraType,
            camera.GateId,
            camera.StreamUrl,
            camera.UrlView
        });
    }

    [HttpPut("cameras/{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateCamera(int id, [FromBody] UpsertCameraRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.CameraName))
        {
            return BadRequest(new { message = "Tên camera là bắt buộc" });
        }

        if (request.GateId.HasValue && !await _context.Gates.AnyAsync(gate => gate.GateId == request.GateId.Value))
        {
            return BadRequest(new { message = "Cổng được chọn không tồn tại" });
        }

        var camera = await _context.Cameras.FindAsync(id);
        if (camera == null)
        {
            return NotFound(new { message = $"Không tìm thấy camera #{id}" });
        }

        camera.CameraName = request.CameraName.Trim();
        camera.CameraType = NormalizeOptionalText(request.CameraType);
        camera.GateId = request.GateId;
        camera.StreamUrl = NormalizeOptionalText(request.StreamUrl);
        camera.UrlView = BuildCameraWebViewUrl(camera.StreamUrl, camera.CameraId);

        await _context.SaveChangesAsync();
        await TryReloadGo2RtcRuntimeAsync();

        return Ok(new
        {
            camera.CameraId,
            camera.CameraName,
            camera.CameraType,
            camera.GateId,
            camera.StreamUrl,
            camera.UrlView
        });
    }

    [HttpDelete("cameras/{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteCamera(int id)
    {
        var camera = await _context.Cameras
            .Include(item => item.AccessLogs)
            .FirstOrDefaultAsync(item => item.CameraId == id);

        if (camera == null)
        {
            return NotFound(new { message = $"Không tìm thấy camera #{id}" });
        }

        if (camera.AccessLogs.Any())
        {
            return BadRequest(new
            {
                message = $"Không thể xóa camera đang có {camera.AccessLogs.Count} bản ghi truy cập liên quan"
            });
        }

        _context.Cameras.Remove(camera);
        await _context.SaveChangesAsync();
        await TryReloadGo2RtcRuntimeAsync();

        return NoContent();
    }

    [HttpGet("gates")]
    public async Task<IActionResult> GetGates([FromQuery] string? query = null)
    {
        var gatesQuery = _context.Gates.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(query))
        {
            var normalized = query.Trim();
            gatesQuery = gatesQuery.Where(gate =>
                gate.GateName.Contains(normalized) ||
                (gate.Location != null && gate.Location.Contains(normalized)));
        }

        var items = await gatesQuery
            .OrderBy(gate => gate.GateName)
            .Select(gate => new
            {
                gate.GateId,
                gate.GateName,
                gate.Location,
                cameraCount = gate.Cameras.Count(),
                accessLogCount = gate.AccessLogs.Count(),
                lastAccessAt = gate.AccessLogs
                    .OrderByDescending(log => log.Timestamp)
                    .Select(log => (DateTime?)log.Timestamp)
                    .FirstOrDefault()
            })
            .ToListAsync();

        return Ok(items);
    }

    [HttpPost("gates")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> CreateGate([FromBody] UpsertGateRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.GateName))
        {
            return BadRequest(new { message = "Tên cổng là bắt buộc" });
        }

        var gate = new Gate
        {
            GateName = request.GateName.Trim(),
            Location = NormalizeOptionalText(request.Location)
        };

        _context.Gates.Add(gate);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetGates), new { id = gate.GateId }, new
        {
            gate.GateId,
            gate.GateName,
            gate.Location
        });
    }

    [HttpPut("gates/{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateGate(int id, [FromBody] UpsertGateRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.GateName))
        {
            return BadRequest(new { message = "Tên cổng là bắt buộc" });
        }

        var gate = await _context.Gates.FindAsync(id);
        if (gate == null)
        {
            return NotFound(new { message = $"Không tìm thấy cổng #{id}" });
        }

        gate.GateName = request.GateName.Trim();
        gate.Location = NormalizeOptionalText(request.Location);

        await _context.SaveChangesAsync();

        return Ok(new
        {
            gate.GateId,
            gate.GateName,
            gate.Location
        });
    }

    [HttpDelete("gates/{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteGate(int id)
    {
        var gate = await _context.Gates
            .Include(item => item.Cameras)
            .Include(item => item.AccessLogs)
            .FirstOrDefaultAsync(item => item.GateId == id);

        if (gate == null)
        {
            return NotFound(new { message = $"Không tìm thấy cổng #{id}" });
        }

        if (gate.Cameras.Any() || gate.AccessLogs.Any())
        {
            return BadRequest(new
            {
                message = "Không thể xóa cổng đang được dùng bởi camera hoặc bản ghi ra vào"
            });
        }

        _context.Gates.Remove(gate);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    private IQueryable<CameraListItem> BuildCameraProjectionQuery()
    {
        return _context.Cameras.AsNoTracking()
            .Select(camera => new CameraListItem
            {
                CameraId = camera.CameraId,
                CameraName = camera.CameraName,
                CameraType = camera.CameraType,
                StreamUrl = camera.StreamUrl,
                UrlView = camera.UrlView,
                GateId = camera.GateId,
                GateName = camera.Gate != null ? camera.Gate.GateName : null,
                GateLocation = camera.Gate != null ? camera.Gate.Location : null,
                AccessLogCount = camera.AccessLogs.Count(),
                LastAccessAt = camera.AccessLogs
                    .OrderByDescending(log => log.Timestamp)
                    .Select(log => (DateTime?)log.Timestamp)
                    .FirstOrDefault(),
                LatestPlate = _context.CameraPlates
                    .Where(plate => plate.CameraIP == camera.CameraName)
                    .OrderByDescending(plate => plate.LastUpdate)
                    .Select(plate => plate.PlateNumber)
                    .FirstOrDefault(),
                LatestPlateAt = _context.CameraPlates
                    .Where(plate => plate.CameraIP == camera.CameraName)
                    .OrderByDescending(plate => plate.LastUpdate)
                    .Select(plate => (DateTime?)plate.LastUpdate)
                    .FirstOrDefault()
            });
    }

    private static string? NormalizeOptionalText(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    public sealed class UpsertCameraRequest
    {
        public string CameraName { get; set; } = string.Empty;
        public string? CameraType { get; set; }
        public int? GateId { get; set; }
        public string? StreamUrl { get; set; }
    }

    public sealed class UpsertGateRequest
    {
        public string GateName { get; set; } = string.Empty;
        public string? Location { get; set; }
    }

    private sealed class CameraListItem
    {
        public int CameraId { get; set; }
        public string CameraName { get; set; } = string.Empty;
        public string? CameraType { get; set; }
        public string? StreamUrl { get; set; }
        public string? UrlView { get; set; }
        public int? GateId { get; set; }
        public string? GateName { get; set; }
        public string? GateLocation { get; set; }
        public int AccessLogCount { get; set; }
        public DateTime? LastAccessAt { get; set; }
        public string? LatestPlate { get; set; }
        public DateTime? LatestPlateAt { get; set; }
    }

    private static string? NormalizeCameraStreamUrl(string? value)
    {
        var normalized = value?.Trim();
        return string.IsNullOrWhiteSpace(normalized) ? null : normalized;
    }

    private static bool IsHttpOrRelativeStreamUrl(string? streamUrl)
    {
        if (string.IsNullOrWhiteSpace(streamUrl))
        {
            return false;
        }

        if (streamUrl.StartsWith("/", StringComparison.Ordinal))
        {
            return true;
        }

        return Uri.TryCreate(streamUrl, UriKind.Absolute, out var uri) &&
               (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
    }

    private static bool ShouldProxyStreamViaGo2Rtc(string? streamUrl) =>
        !string.IsNullOrWhiteSpace(streamUrl) && !IsHttpOrRelativeStreamUrl(streamUrl);

    private string? BuildCameraWebViewUrl(string? streamUrl, int cameraId)
    {
        var normalizedStreamUrl = NormalizeCameraStreamUrl(streamUrl);
        if (string.IsNullOrWhiteSpace(normalizedStreamUrl))
        {
            return null;
        }

        if (IsHttpOrRelativeStreamUrl(normalizedStreamUrl))
        {
            return normalizedStreamUrl.StartsWith("/", StringComparison.Ordinal)
                ? $"{ResolvePublicApplicationBaseUrl()}{normalizedStreamUrl}"
                : normalizedStreamUrl;
        }

        var go2RtcPublicBaseUrl = ResolveGo2RtcPublicBaseEndpoint();
        return $"{go2RtcPublicBaseUrl}/stream.html?src=cam{cameraId}&mode=webrtc";
    }

    private string ResolveGo2RtcPublicBaseEndpoint()
    {
        var configured = _configuration["AppSettings:Go2RtcPublicBaseUrl"];
        if (!string.IsNullOrWhiteSpace(configured) && !ShouldForceProxyGo2RtcBase(configured))
        {
            return NormalizeUrlBase(configured);
        }

        return $"{ResolvePublicApplicationBaseUrl()}/go2rtc";
    }

    private bool ShouldForceProxyGo2RtcBase(string configuredBaseUrl)
    {
        if (!IsDockerMode())
        {
            return false;
        }

        var allowCrossOrigin = (_configuration["AppSettings:AllowCrossOriginGo2RtcFrame"] ?? "false")
            .Trim()
            .Equals("true", StringComparison.OrdinalIgnoreCase);
        if (allowCrossOrigin)
        {
            return false;
        }

        if (!Uri.TryCreate(configuredBaseUrl.Trim(), UriKind.Absolute, out var configuredUri))
        {
            return false;
        }

        var requestHost = Request.Host.Host;
        if (string.IsNullOrWhiteSpace(requestHost))
        {
            return false;
        }

        return !configuredUri.Host.Equals(requestHost, StringComparison.OrdinalIgnoreCase);
    }

    private string ResolvePublicApplicationBaseUrl()
    {
        var configuredFrontendUrl = _configuration["AppSettings:FrontendUrl"];
        if (!string.IsNullOrWhiteSpace(configuredFrontendUrl) &&
            !ShouldPreferRequestBaseUrl(configuredFrontendUrl))
        {
            return NormalizeUrlBase(configuredFrontendUrl);
        }

        return NormalizeUrlBase($"{Request.Scheme}://{Request.Host}");
    }

    private bool ShouldPreferRequestBaseUrl(string configuredFrontendUrl)
    {
        if (!IsDockerMode())
        {
            return false;
        }

        if (!Uri.TryCreate(configuredFrontendUrl.Trim(), UriKind.Absolute, out var configuredUri))
        {
            return false;
        }

        var requestHost = Request.Host.Host?.Trim();
        if (string.IsNullOrWhiteSpace(requestHost))
        {
            return false;
        }

        return IsLoopbackHost(configuredUri.Host) && !IsLoopbackHost(requestHost);
    }

    private static string NormalizeUrlBase(string value) =>
        value.Trim().TrimEnd('/');

    private static bool IsLoopbackHost(string host) =>
        host.Equals("localhost", StringComparison.OrdinalIgnoreCase) ||
        host.Equals("127.0.0.1", StringComparison.OrdinalIgnoreCase) ||
        host.Equals("::1", StringComparison.OrdinalIgnoreCase) ||
        host.Equals("[::1]", StringComparison.OrdinalIgnoreCase);

    private async Task TryReloadGo2RtcRuntimeAsync()
    {
        try
        {
            var cameras = await _context.Cameras
                .Where(c => !string.IsNullOrWhiteSpace(c.StreamUrl))
                .ToListAsync();

            var yaml = new StringBuilder();
            yaml.AppendLine("streams:");

            foreach (var cam in cameras)
            {
                var normalizedStreamUrl = NormalizeCameraStreamUrl(cam.StreamUrl);
                if (string.IsNullOrWhiteSpace(normalizedStreamUrl))
                {
                    continue;
                }

                cam.UrlView = BuildCameraWebViewUrl(normalizedStreamUrl, cam.CameraId);

                if (!ShouldProxyStreamViaGo2Rtc(normalizedStreamUrl))
                {
                    continue;
                }

                var streamName = $"cam{cam.CameraId}";
                yaml.AppendLine($"  {streamName}:");
                yaml.AppendLine($"    - {normalizedStreamUrl}#transport=tcp");
                if (!normalizedStreamUrl.Contains("#transport=", StringComparison.OrdinalIgnoreCase))
                {
                    yaml.AppendLine($"    - {normalizedStreamUrl}");
                }
            }

            yaml.AppendLine("api:");
            yaml.AppendLine("  origin: \"*\"");
            yaml.AppendLine("webrtc:");
            yaml.AppendLine("  listen: \":8555\"");
            var candidates = ResolveWebRtcCandidates().ToList();
            if (candidates.Count > 0)
            {
                yaml.AppendLine("  candidates:");
            }
            foreach (var candidate in candidates)
            {
                yaml.AppendLine($"    - {candidate}");
            }
            yaml.AppendLine("  ice_servers:");
            yaml.AppendLine("    - urls:");
            yaml.AppendLine("        - stun:stun.l.google.com:19302");

            var yamlPath = ResolveGo2RtcYamlPath();
            var yamlDirectory = Path.GetDirectoryName(yamlPath);
            if (!string.IsNullOrWhiteSpace(yamlDirectory) && !Directory.Exists(yamlDirectory))
            {
                Directory.CreateDirectory(yamlDirectory);
            }

            await System.IO.File.WriteAllTextAsync(yamlPath, yaml.ToString());
            await _context.SaveChangesAsync();

            if (IsDockerMode())
            {
                await TryReloadGo2RtcByHttpAsync();
                return;
            }

            var go2rtcPath = Path.GetDirectoryName(yamlPath) ?? string.Empty;
            var exePath = Path.Combine(go2rtcPath, "go2rtc.exe");
            if (!Directory.Exists(go2rtcPath) || !System.IO.File.Exists(exePath))
            {
                return;
            }

            foreach (var proc in Process.GetProcessesByName("go2rtc"))
            {
                proc.Kill();
            }

            Process.Start(new ProcessStartInfo
            {
                FileName = exePath,
                WorkingDirectory = go2rtcPath,
                UseShellExecute = true
            });
        }
        catch
        {
            // Never block CRUD camera when go2rtc reload fails.
        }
    }

    private string ResolveAiRootFolderName() =>
        _configuration["RuntimePaths:AiRootFolderName"] ?? "AI_Project";

    private bool IsDockerMode()
    {
        var mode = (_configuration["Runtime:Mode"] ?? "local").Trim().ToLowerInvariant();
        return mode == "docker";
    }

    private string ResolveGo2RtcYamlPath()
    {
        var configured = _configuration["Go2Rtc:ConfigPath"];
        if (!string.IsNullOrWhiteSpace(configured))
        {
            return configured.Trim();
        }

        var basePath = Directory.GetCurrentDirectory();
        var go2rtcPath = Path.GetFullPath(Path.Combine(basePath, "..", "..", "..", ResolveAiRootFolderName(), "cam", "go2rtc_win64"));
        return Path.Combine(go2rtcPath, "go2rtc.yaml");
    }

    private async Task TryReloadGo2RtcByHttpAsync()
    {
        var reloadUrl = (_configuration["Go2Rtc:ReloadUrl"] ?? "http://go2rtc:1984/api/restart").Trim();
        if (string.IsNullOrWhiteSpace(reloadUrl))
        {
            return;
        }

        try
        {
            using var http = _httpClientFactory.CreateClient();
            http.Timeout = TimeSpan.FromSeconds(5);
            using var req = new HttpRequestMessage(HttpMethod.Post, reloadUrl);
            using var _ = await http.SendAsync(req, HttpCompletionOption.ResponseHeadersRead);
        }
        catch
        {
            // Never block camera CRUD when go2rtc hot-reload endpoint is unavailable.
        }
    }

    private IEnumerable<string> ResolveWebRtcCandidates()
    {
        var configured = _configuration["Go2Rtc:WebRtcCandidates"];
        if (!string.IsNullOrWhiteSpace(configured))
        {
            return configured
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Where(value => !string.IsNullOrWhiteSpace(value));
        }

        // Keep public-domain safe default (no forced candidate).
        // For localhost docker preview, provide loopback candidate so browser can reach media port.
        if (!IsDockerMode())
        {
            return Array.Empty<string>();
        }

        var requestHost = Request.Host.Host?.Trim();
        if (string.IsNullOrWhiteSpace(requestHost))
        {
            return Array.Empty<string>();
        }

        if (!IsLoopbackHost(requestHost))
        {
            return Array.Empty<string>();
        }

        var port = _configuration["Go2Rtc:WebRtcPort"]?.Trim();
        if (string.IsNullOrWhiteSpace(port))
        {
            port = "8555";
        }

        return new[] { $"{requestHost}:{port}" };
    }

}


