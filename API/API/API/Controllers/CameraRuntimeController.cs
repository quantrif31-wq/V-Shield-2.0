using API.Middleware;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using API.Data;
using API.Models;
using API.DTOs;
using System.Diagnostics;
using System.Text;

namespace API.Controllers
{
    [Route("api/camera-runtime")]
    [ApiController]
    [EnableRateLimiting("ops")]
    [Authorize]
    [RequireOperationalTask("monitoring")]
    public class CameraRuntimeController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IWebHostEnvironment _environment;

        public CameraRuntimeController(
            ApplicationDbContext context,
            IConfiguration configuration,
            IHttpClientFactory httpClientFactory,
            IWebHostEnvironment environment)
        {
            _context = context;
            _configuration = configuration;
            _httpClientFactory = httpClientFactory;
            _environment = environment;
        }

        // ================= GET ALL =================
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var cams = await _context.Cameras
                .Include(c => c.Gate)
                .Select(c => new CameraDTO
                {
                    CameraId = c.CameraId,
                    CameraName = c.CameraName,
                    CameraType = c.CameraType,
                    GateId = c.GateId,
                    StreamUrl = c.StreamUrl,
                    UrlView = c.UrlView,
                    IsRecordingEnabled = c.IsRecordingEnabled,
                    RecordingRetentionDays = c.RecordingRetentionDays,
                    GateName = c.Gate != null ? c.Gate.GateName : null,
                    GateLocation = c.Gate != null ? c.Gate.Location : null
                })
                .ToListAsync();

            foreach (var cam in cams)
            {
                cam.UrlView = await BuildCameraViewUrl(cam.StreamUrl, cam.CameraId);
                NormalizeCameraRecordingState(cam);
            }

            return Ok(cams);
        }

        // ================= GET BY ID =================
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var cam = await _context.Cameras
                .Include(c => c.Gate)
                .Where(c => c.CameraId == id)
                .Select(c => new CameraDTO
                {
                    CameraId = c.CameraId,
                    CameraName = c.CameraName,
                    CameraType = c.CameraType,
                    GateId = c.GateId,
                    StreamUrl = c.StreamUrl,
                    UrlView = c.UrlView,
                    IsRecordingEnabled = c.IsRecordingEnabled,
                    RecordingRetentionDays = c.RecordingRetentionDays,
                    GateName = c.Gate != null ? c.Gate.GateName : null,
                    GateLocation = c.Gate != null ? c.Gate.Location : null
                })
                .FirstOrDefaultAsync();

            if (cam == null)
                return NotFound("Không tìm thấy camera");

            cam.UrlView = await BuildCameraViewUrl(cam.StreamUrl, cam.CameraId);
            NormalizeCameraRecordingState(cam);

            return Ok(cam);
        }

        // ================= CREATE =================
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CameraUpsertRequest request)
        {
            if (request == null)
                return BadRequest(new { message = "Dữ liệu camera không hợp lệ" });

            var cameraName = request.CameraName?.Trim();
            var cameraType = request.CameraType?.Trim();
            var streamUrl = request.StreamUrl?.Trim();

            if (string.IsNullOrWhiteSpace(cameraName))
                return BadRequest(new { message = "Tên camera không được rỗng" });

            if (request.GateId.HasValue)
            {
                var gateExists = await _context.Gates.AnyAsync(g => g.GateId == request.GateId.Value);
                if (!gateExists)
                {
                    return BadRequest(new { message = "Gate ID không tồn tại" });
                }
            }

            var camera = new Camera
            {
                CameraName = cameraName,
                GateId = request.GateId,
                CameraType = string.IsNullOrWhiteSpace(cameraType) ? null : cameraType,
                StreamUrl = string.IsNullOrWhiteSpace(streamUrl) ? null : streamUrl,
                IsRecordingEnabled = request.IsRecordingEnabled ?? HasRecordableInput(streamUrl, null),
                RecordingRetentionDays = request.RecordingRetentionDays ?? 30
            };

            _context.Cameras.Add(camera);
            await _context.SaveChangesAsync();

            camera.UrlView = await BuildCameraViewUrl(streamUrl, camera.CameraId);
            await _context.SaveChangesAsync();

            return Ok(camera);
        }

        // ================= UPDATE =================
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] CameraUpsertRequest request)
        {
            var cam = await _context.Cameras.FindAsync(id);

            if (cam == null)
                return NotFound("Camera không tồn tại");

            var cameraName = request.CameraName?.Trim();
            var cameraType = request.CameraType?.Trim();
            var streamUrl = request.StreamUrl?.Trim();

            if (string.IsNullOrWhiteSpace(cameraName))
                return BadRequest(new { message = "Tên camera không được rỗng" });

            if (request.GateId.HasValue)
            {
                var gateExists = await _context.Gates.AnyAsync(g => g.GateId == request.GateId.Value);
                if (!gateExists)
                {
                    return BadRequest(new { message = "Gate ID không tồn tại" });
                }
            }

            cam.CameraName = cameraName;
            cam.CameraType = string.IsNullOrWhiteSpace(cameraType) ? null : cameraType;
            cam.GateId = request.GateId;
            cam.StreamUrl = string.IsNullOrWhiteSpace(streamUrl) ? null : streamUrl;
            cam.UrlView = await BuildCameraViewUrl(cam.StreamUrl, cam.CameraId);
            if (request.IsRecordingEnabled.HasValue)
                cam.IsRecordingEnabled = request.IsRecordingEnabled.Value;
            if (request.RecordingRetentionDays.HasValue)
                cam.RecordingRetentionDays = request.RecordingRetentionDays.Value;

            if (HasRecordableInput(cam.StreamUrl, cam.UrlView))
            {
                cam.IsRecordingEnabled = true;
                if (cam.RecordingRetentionDays <= 0)
                    cam.RecordingRetentionDays = 30;
            }

            await _context.SaveChangesAsync();

            return Ok(cam);
        }

        // ================= DELETE =================
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var cam = await _context.Cameras.FindAsync(id);

            if (cam == null)
                return NotFound("Không tồn tại");

            _context.Cameras.Remove(cam);
            await _context.SaveChangesAsync();

            return Ok();
        }

        // ================= TOGGLE RECORDING =================
        [HttpPut("{id}/recording")]
        public async Task<IActionResult> ToggleRecording(int id, [FromBody] ToggleRecordingRequest request)
        {
            var cam = await _context.Cameras.FindAsync(id);
            if (cam == null)
                return NotFound("Camera không tồn tại");

            cam.IsRecordingEnabled = HasRecordableInput(cam.StreamUrl, cam.UrlView)
                ? true
                : request.Enabled;
            if (request.RetentionDays.HasValue)
                cam.RecordingRetentionDays = request.RetentionDays.Value;

            if (cam.RecordingRetentionDays <= 0)
                cam.RecordingRetentionDays = 30;

            await _context.SaveChangesAsync();
            return Ok(new { cam.IsRecordingEnabled, cam.RecordingRetentionDays });
        }

        [HttpGet("archive/segments")]
        public async Task<IActionResult> GetArchiveSegments(
            [FromQuery] int? cameraId,
            [FromQuery] int? gateId,
            [FromQuery] string? cameraType,
            [FromQuery] string? search,
            [FromQuery] DateTime? from,
            [FromQuery] DateTime? to,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 50)
        {
            page = Math.Max(page, 1);
            pageSize = Math.Clamp(pageSize, 1, 200);

            var query = _context.RecordedSegments
                .AsNoTracking()
                .Include(s => s.Camera)
                .ThenInclude(c => c!.Gate)
                .AsQueryable();

            if (cameraId.HasValue)
                query = query.Where(s => s.CameraId == cameraId.Value);
            if (gateId.HasValue)
                query = query.Where(s => s.Camera != null && s.Camera.GateId == gateId.Value);
            if (!string.IsNullOrWhiteSpace(cameraType))
            {
                var normalizedType = cameraType.Trim();
                query = query.Where(s => s.Camera != null && s.Camera.CameraType == normalizedType);
            }
            if (from.HasValue)
                query = query.Where(s => s.StartedAt >= from.Value);
            if (to.HasValue)
                query = query.Where(s => s.StartedAt <= to.Value);
            if (!string.IsNullOrWhiteSpace(search))
            {
                var normalizedSearch = search.Trim();
                query = query.Where(s =>
                    (s.Camera != null && s.Camera.CameraName.Contains(normalizedSearch)) ||
                    (s.Camera != null && s.Camera.CameraType != null && s.Camera.CameraType.Contains(normalizedSearch)) ||
                    (s.Camera != null && s.Camera.Gate != null && s.Camera.Gate.GateName.Contains(normalizedSearch)) ||
                    (s.Camera != null && s.Camera.Gate != null && s.Camera.Gate.Location != null && s.Camera.Gate.Location.Contains(normalizedSearch)));
            }

            var total = await query.CountAsync();
            var items = await query
                .OrderByDescending(s => s.StartedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(s => new
                {
                    s.SegmentId,
                    s.CameraId,
                    cameraName = s.Camera != null ? s.Camera.CameraName : null,
                    cameraType = s.Camera != null ? s.Camera.CameraType : null,
                    gateId = s.Camera != null ? s.Camera.GateId : null,
                    gateName = s.Camera != null && s.Camera.Gate != null ? s.Camera.Gate.GateName : null,
                    gateLocation = s.Camera != null && s.Camera.Gate != null ? s.Camera.Gate.Location : null,
                    s.StartedAt,
                    s.EndedAt,
                    s.FileSizeBytes,
                    s.DurationSeconds,
                    s.StorageUrl
                })
                .ToListAsync();

            return Ok(new { total, page, pageSize, items });
        }

        // A DVR recording is one HLS timeline rather than a row per short file.
        // Expose only timelines that already contain media so archive entry points
        // can open a useful camera immediately instead of a blank "all cameras" view.
        [HttpGet("archive/dvr-status")]
        public async Task<IActionResult> GetDvrStatus(
            [FromQuery] int? cameraId,
            [FromQuery] int? gateId,
            [FromQuery] string? cameraType,
            [FromQuery] string? search,
            [FromQuery] DateTime? from,
            [FromQuery] DateTime? to)
        {
            var today = DateOnly.FromDateTime(DateTime.Now);
            var firstDay = DateOnly.FromDateTime((from ?? today.ToDateTime(TimeOnly.MinValue)).Date);
            var lastDay = DateOnly.FromDateTime((to ?? today.ToDateTime(TimeOnly.MaxValue)).Date);
            if (lastDay < firstDay) return Ok(Array.Empty<DvrStatusItem>());

            var webRoot = _environment.WebRootPath ?? Path.Combine(_environment.ContentRootPath, "wwwroot");
            var cameraQuery = _context.Cameras
                .AsNoTracking()
                .Include(camera => camera.Gate)
                .AsQueryable();
            if (cameraId.HasValue) cameraQuery = cameraQuery.Where(camera => camera.CameraId == cameraId.Value);
            if (gateId.HasValue) cameraQuery = cameraQuery.Where(camera => camera.GateId == gateId.Value);
            if (!string.IsNullOrWhiteSpace(cameraType))
                cameraQuery = cameraQuery.Where(camera => camera.CameraType == cameraType.Trim());
            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim();
                cameraQuery = cameraQuery.Where(camera =>
                    camera.CameraName.Contains(term) ||
                    (camera.CameraType != null && camera.CameraType.Contains(term)) ||
                    (camera.Gate != null && camera.Gate.GateName.Contains(term)) ||
                    (camera.Gate != null && camera.Gate.Location != null && camera.Gate.Location.Contains(term)));
            }

            var cameras = await cameraQuery
                .Select(camera => new { camera.CameraId, camera.CameraName })
                .ToListAsync();

            var items = new List<DvrStatusItem>();
            foreach (var camera in cameras)
            {
                var dvrRoot = Path.Combine(
                    webRoot,
                    "uploads",
                    "recordings",
                    $"cam{camera.CameraId}",
                    "dvr");
                if (!Directory.Exists(dvrRoot)) continue;

                foreach (var dayDirectory in Directory.GetDirectories(dvrRoot))
                {
                    if (!DateOnly.TryParseExact(Path.GetFileName(dayDirectory), "yyyy-MM-dd", out var recordingDay) ||
                        recordingDay < firstDay || recordingDay > lastDay)
                        continue;

                    var playlistPath = Path.Combine(dayDirectory, "index.m3u8");
                    if (!System.IO.File.Exists(playlistPath)) continue;

                    var segmentCount = 0;
                    var durationSeconds = 0d;
                    string? initFileName = null;
                    try
                    {
                        foreach (var line in System.IO.File.ReadLines(playlistPath))
                        {
                            if (line.StartsWith("#EXTINF:", StringComparison.Ordinal) &&
                                double.TryParse(line[8..].TrimEnd(','), System.Globalization.NumberStyles.Float,
                                    System.Globalization.CultureInfo.InvariantCulture, out var duration) && duration > 0.01)
                            {
                                segmentCount += 1;
                                durationSeconds += duration;
                            }
                            else if (line.StartsWith("#EXT-X-MAP:URI=\"", StringComparison.Ordinal))
                            {
                                var value = line[16..];
                                var end = value.IndexOf('"');
                                if (end > 0) initFileName = value[..end];
                            }
                        }
                    }
                    catch { continue; }

                    var initPath = string.IsNullOrWhiteSpace(initFileName) ? null : Path.Combine(dayDirectory, initFileName);
                    if (segmentCount == 0 || string.IsNullOrWhiteSpace(initPath) || !System.IO.File.Exists(initPath) || new FileInfo(initPath).Length == 0)
                        continue;

                    var lastMediaAtUtc = Directory.EnumerateFiles(dayDirectory, "*.m4s", SearchOption.TopDirectoryOnly)
                        .Select(path => System.IO.File.GetLastWriteTimeUtc(path)).DefaultIfEmpty(DateTime.MinValue).Max();
                    var isRecording = recordingDay == today && lastMediaAtUtc >= DateTime.UtcNow.AddSeconds(-15);

                    items.Add(new DvrStatusItem(camera.CameraId, camera.CameraName, recordingDay, segmentCount,
                        durationSeconds, lastMediaAtUtc, isRecording));
                }
            }

            return Ok(items.OrderByDescending(item => item.UpdatedAtUtc));
        }

        // ================= LIST RECORDED SEGMENTS =================
        [HttpGet("{id}/recorded-segments")]
        public async Task<IActionResult> GetRecordedSegments(
            int id,
            [FromQuery] DateTime? from,
            [FromQuery] DateTime? to,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 50)
        {
            var query = _context.RecordedSegments
                .Where(s => s.CameraId == id);

            if (from.HasValue)
                query = query.Where(s => s.StartedAt >= from.Value);
            if (to.HasValue)
                query = query.Where(s => s.StartedAt <= to.Value);

            var total = await query.CountAsync();
            var items = await query
                .OrderByDescending(s => s.StartedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(s => new
                {
                    s.SegmentId,
                    s.CameraId,
                    cameraName = s.Camera != null ? s.Camera.CameraName : null,
                    cameraType = s.Camera != null ? s.Camera.CameraType : null,
                    gateId = s.Camera != null ? s.Camera.GateId : null,
                    gateName = s.Camera != null && s.Camera.Gate != null ? s.Camera.Gate.GateName : null,
                    gateLocation = s.Camera != null && s.Camera.Gate != null ? s.Camera.Gate.Location : null,
                    s.StartedAt,
                    s.EndedAt,
                    s.FileSizeBytes,
                    s.DurationSeconds,
                    s.StorageUrl
                })
                .ToListAsync();

            return Ok(new { total, page, pageSize, items });
        }

        // ================= RELOAD GO2RTC =================
        [HttpPost("reload-go2rtc")]
        public async Task<IActionResult> ReloadGo2Rtc()
        {
            try
            {
                // ===== LẤY CAMERA CÓ StreamUrl =====
                var cameras = await _context.Cameras
                    .Where(c => !string.IsNullOrEmpty(c.StreamUrl))
                    .ToListAsync();

                if (!cameras.Any())
                    return BadRequest("Không có camera nào có StreamUrl");

                // ===== BUILD YAML =====
                var yaml = new StringBuilder();
                yaml.AppendLine("streams:");

                foreach (var cam in cameras)
                {
                    var normalizedStreamUrl = NormalizeCameraUrl(cam.StreamUrl);
                    if (string.IsNullOrWhiteSpace(normalizedStreamUrl))
                    {
                        continue;
                    }

                    cam.UrlView = await BuildCameraViewUrl(normalizedStreamUrl, cam.CameraId);

                    if (!ShouldProxyViaGo2Rtc(normalizedStreamUrl))
                    {
                        continue;
                    }

                    // Dùng CameraId để không bị lệch cam
                    var streamName = $"cam{cam.CameraId}";

                    yaml.AppendLine($"  {streamName}:");
                    yaml.AppendLine($"    - {normalizedStreamUrl}#transport=tcp");
                    if (normalizedStreamUrl.StartsWith("rtsp://", StringComparison.OrdinalIgnoreCase))
                    {
                        // Compatibility producer for legacy <img> previews and recording probes.
                        // The primary producer remains H.264 for efficient MSE/WebRTC playback.
                        yaml.AppendLine($"    - ffmpeg:{normalizedStreamUrl}#video=mjpeg");
                    }
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


                // ===== PATH =====
                var yamlPath = ResolveGo2RtcYamlPath();
                var yamlDirectory = Path.GetDirectoryName(yamlPath);
                if (!string.IsNullOrWhiteSpace(yamlDirectory) && !Directory.Exists(yamlDirectory))
                {
                    Directory.CreateDirectory(yamlDirectory);
                }

                // ===== GHI FILE YAML =====
                await System.IO.File.WriteAllTextAsync(yamlPath, yaml.ToString());

                // ===== LƯU DB (QUAN TRỌNG) =====
                await _context.SaveChangesAsync();

                await TryReloadGo2RtcByHttpAsync();

                return Ok(new
                {
                    message = "Reload go2rtc thành công",
                    yaml = yaml.ToString()
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine("ERROR: " + ex.ToString());

                return StatusCode(500, new
                {
                    message = ex.Message,
                    detail = ex.InnerException?.Message,
                    stack = ex.StackTrace
                });
            }
        }

        // ================= STOP =================
        [HttpPost("stop-go2rtc")]
        public IActionResult StopGo2Rtc()
        {
            return Ok("Dịch vụ Go2RTC được quản lý bởi Docker Compose.");
        }

        // ================= START/STOP PYTHON PROCESSES =================
        [HttpPost("start-python-qr")]
        public IActionResult StartPythonQr()
        {
            return Ok("Dịch vụ QR được quản lý bởi Docker Compose.");
        }

        [HttpPost("stop-python-qr")]
        public IActionResult StopPythonQr()
        {
            return Ok("Dịch vụ QR được quản lý bởi Docker Compose.");
        }

        [HttpPost("start-python-plate")]
        public IActionResult StartPythonPlate()
        {
            return Ok("Dịch vụ Biển số được quản lý bởi Docker Compose.");
        }

        [HttpPost("stop-python-plate")]
        public IActionResult StopPythonPlate()
        {
            return Ok("Dịch vụ Biển số được quản lý bởi Docker Compose.");
        }

        [HttpGet("status-python")]
        public IActionResult StatusPython()
        {
            return Ok(new
            {
                qr = true,
                plate = true
            });
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
            var aiRootFolderName = ResolveAiRootFolderName();
            var go2rtcPath = Path.GetFullPath(
                Path.Combine(basePath, "..", "..", "..", aiRootFolderName, "cam", "go2rtc_win64")
            );
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
                // Docker mode: do not crash flow if hot-reload endpoint is unavailable.
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

        private static bool IsLoopbackHost(string host) =>
            host.Equals("localhost", StringComparison.OrdinalIgnoreCase) ||
            host.Equals("127.0.0.1", StringComparison.OrdinalIgnoreCase) ||
            host.Equals("::1", StringComparison.OrdinalIgnoreCase);


        private static string? NormalizeCameraUrl(string? value)
        {
            var normalized = value?.Trim();
            return string.IsNullOrWhiteSpace(normalized) ? null : normalized;
        }

        private static bool HasRecordableInput(string? streamUrl, string? urlView) =>
            !string.IsNullOrWhiteSpace(streamUrl) || !string.IsNullOrWhiteSpace(urlView);

        private static void NormalizeCameraRecordingState(CameraDTO camera)
        {
            if (camera == null)
                return;

            if (HasRecordableInput(camera.StreamUrl, camera.UrlView))
            {
                camera.IsRecordingEnabled = true;
                if (camera.RecordingRetentionDays <= 0)
                    camera.RecordingRetentionDays = 30;
            }
        }

        private static bool IsDirectWebStream(string? streamUrl)
        {
            if (string.IsNullOrWhiteSpace(streamUrl))
            {
                return false;
            }

            if (IsDemoRuntimePreviewStream(streamUrl))
            {
                return true;
            }

            if (streamUrl.StartsWith("/", StringComparison.Ordinal))
            {
                return true;
            }

            return Uri.TryCreate(streamUrl, UriKind.Absolute, out var uri) &&
                   (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
        }

        private static bool ShouldProxyViaGo2Rtc(string? streamUrl) =>
            !string.IsNullOrWhiteSpace(streamUrl) && !IsDirectWebStream(streamUrl);

        private async Task<string?> BuildCameraViewUrl(string? streamUrl, int cameraId)
        {
            var normalizedStreamUrl = NormalizeCameraUrl(streamUrl);
            if (string.IsNullOrWhiteSpace(normalizedStreamUrl))
            {
                return null;
            }

            var demoPreviewUrl = BuildDemoRuntimePreviewUrl(normalizedStreamUrl);
            if (!string.IsNullOrWhiteSpace(demoPreviewUrl))
            {
                return demoPreviewUrl;
            }

            if (IsDirectWebStream(normalizedStreamUrl))
            {
                return BuildDirectWebStreamUrl(normalizedStreamUrl);
            }

            var go2RtcPublicBaseUrl = await ResolveGo2RtcPublicBaseUrl();
            return $"{go2RtcPublicBaseUrl}/stream.html?src=cam{cameraId}&mode=webrtc,mse";
        }

        private string BuildDirectWebStreamUrl(string streamUrl)
        {
            if (!streamUrl.StartsWith("/", StringComparison.Ordinal))
            {
                return streamUrl;
            }

            return $"{ResolvePublicAppBaseUrl()}{streamUrl}";
        }

        private string? BuildDemoRuntimePreviewUrl(string streamUrl)
        {
            if (streamUrl.Equals("rtsp://demo.local/qr", StringComparison.OrdinalIgnoreCase))
            {
                return $"{ResolvePublicAppBaseUrl()}/qr-api/qr/frame.jpg";
            }

            if (streamUrl.Equals("rtsp://demo.local/plate", StringComparison.OrdinalIgnoreCase))
            {
                return $"{ResolvePublicAppBaseUrl()}/plate-api/api/camera/stream";
            }

            return null;
        }

        private static bool IsDemoRuntimePreviewStream(string streamUrl) =>
            streamUrl.Equals("rtsp://demo.local/qr", StringComparison.OrdinalIgnoreCase) ||
            streamUrl.Equals("rtsp://demo.local/plate", StringComparison.OrdinalIgnoreCase);

        private async Task<string> ResolveGo2RtcPublicBaseUrl()
        {
            var cameraMode = await _context.SystemConfigs
                .Where(s => s.Key == "CameraStreamMode")
                .Select(s => s.Value)
                .FirstOrDefaultAsync();

            if (cameraMode == "public")
            {
                var configured = _configuration["AppSettings:Go2RtcPublicBaseUrl"];
                if (!string.IsNullOrWhiteSpace(configured))
                    return NormalizeBaseUrl(configured);
            }

            if (cameraMode == "local")
                return $"{ResolvePublicAppBaseUrl()}/go2rtc";

            // auto mode (default) — current behavior
            var cfg = _configuration["AppSettings:Go2RtcPublicBaseUrl"];
            if (!string.IsNullOrWhiteSpace(cfg) && !ShouldForceProxyGo2RtcBase(cfg))
                return NormalizeBaseUrl(cfg);

            var host = Request.Host.Value;
            if (!string.IsNullOrEmpty(host) && host.Contains("maiai06.site"))
                return $"https://{host}";

            return $"{ResolvePublicAppBaseUrl()}/go2rtc";
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

        private string ResolvePublicAppBaseUrl()
        {
            var requestDerivedBaseUrl = TryResolveRequestBaseUrl();
            if (!string.IsNullOrWhiteSpace(requestDerivedBaseUrl))
            {
                return requestDerivedBaseUrl;
            }

            var configuredFrontendUrl = _configuration["AppSettings:FrontendUrl"];
            if (!string.IsNullOrWhiteSpace(configuredFrontendUrl) &&
                !ShouldPreferRequestBaseUrl(configuredFrontendUrl))
            {
                return NormalizeBaseUrl(configuredFrontendUrl);
            }

            return NormalizeBaseUrl($"{Request.Scheme}://{Request.Host}");
        }

        private string? TryResolveRequestBaseUrl()
        {
            var candidates = new[]
            {
                Request.Headers.Origin.ToString(),
                Request.Headers.Referer.ToString()
            };

            foreach (var candidate in candidates)
            {
                if (string.IsNullOrWhiteSpace(candidate))
                {
                    continue;
                }

                if (!Uri.TryCreate(candidate.Trim(), UriKind.Absolute, out var uri))
                {
                    continue;
                }

                if (uri.IsLoopback)
                {
                    return NormalizeBaseUrl(uri.GetLeftPart(UriPartial.Authority));
                }
            }

            return null;
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

        private static string NormalizeBaseUrl(string value) =>
            value.Trim().TrimEnd('/');

        private static string GetLocalIpv4Address()
        {
            var host = System.Net.Dns.GetHostEntry(System.Net.Dns.GetHostName());

            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                {
                    // bỏ qua localhost
                    if (!ip.ToString().StartsWith("127."))
                    {
                        return ip.ToString();
                    }
                }
            }

            return "127.0.0.1";
        }

        private sealed record DvrStatusItem(
            int CameraId,
            string CameraName,
            DateOnly RecordingDate,
            int SegmentCount,
            double DurationSeconds,
            DateTime UpdatedAtUtc,
            bool IsRecording);
    }
}
