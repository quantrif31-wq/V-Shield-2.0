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
    public class CameraRuntimeController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly IHttpClientFactory _httpClientFactory;

        public CameraRuntimeController(ApplicationDbContext context, IConfiguration configuration, IHttpClientFactory httpClientFactory)
        {
            _context = context;
            _configuration = configuration;
            _httpClientFactory = httpClientFactory;
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
                    GateName = c.Gate != null ? c.Gate.GateName : null
                })
                .ToListAsync();

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
                    GateName = c.Gate != null ? c.Gate.GateName : null
                })
                .FirstOrDefaultAsync();

            if (cam == null)
                return NotFound("Không tìm thấy camera");

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
                StreamUrl = string.IsNullOrWhiteSpace(streamUrl) ? null : streamUrl
            };

            _context.Cameras.Add(camera);
            await _context.SaveChangesAsync();

            camera.UrlView = BuildCameraViewUrl(streamUrl, camera.CameraId);
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
            cam.UrlView = BuildCameraViewUrl(cam.StreamUrl, cam.CameraId);

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
                    cam.UrlView = BuildCameraViewUrl(normalizedStreamUrl, cam.CameraId);

                    if (!ShouldProxyViaGo2Rtc(normalizedStreamUrl))
                    {
                        continue;
                    }

                    // Dùng CameraId để không bị lệch cam
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

                if (!IsDockerMode())
                {
                    var go2rtcPath = Path.GetDirectoryName(yamlPath) ?? string.Empty;
                    var exePath = Path.Combine(go2rtcPath, "go2rtc.exe");

                    // ===== STOP PROCESS CŨ =====
                    foreach (var proc in Process.GetProcessesByName("go2rtc"))
                    {
                        proc.Kill();
                    }

                    // ===== START GO2RTC =====
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = exePath,
                        WorkingDirectory = go2rtcPath,
                        UseShellExecute = true
                    });

                    // ===== AUTO CLOUDFLARE =====
                    EnsureCloudflaredTunnelConfig();
                    StartCloudflaredTunnel();
                }
                else
                {
                    await TryReloadGo2RtcByHttpAsync();
                }

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
            try
            {
                var processes = Process.GetProcessesByName("go2rtc");

                foreach (var proc in processes)
                {
                    proc.Kill();
                }

                return Ok("Đã tắt go2rtc");
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        // ================= START/STOP PYTHON PROCESSES =================
        [HttpPost("start-python-qr")]
        public IActionResult StartPythonQr()
        {
            return StartPythonWorkerScript("QR_Dong", "QR_Dong.py");
        }

        [HttpPost("stop-python-qr")]
        public IActionResult StopPythonQr()
        {
            return StopPythonWorkerScript("QR_Dong.py");
        }

        [HttpPost("start-python-plate")]
        public IActionResult StartPythonPlate()
        {
            return StartPythonWorkerScript("doc_bien_gpu", "docbien.py");
        }

        [HttpPost("stop-python-plate")]
        public IActionResult StopPythonPlate()
        {
            return StopPythonWorkerScript("docbien.py");
        }

        [HttpGet("status-python")]
        public IActionResult StatusPython()
        {
            try
            {
                var isQrRunning = IsPythonWorkerScriptRunning("QR_Dong.py");
                var isPlateRunning = IsPythonWorkerScriptRunning("docbien.py");
                
                return Ok(new
                {
                    qr = isQrRunning,
                    plate = isPlateRunning
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        private IActionResult StartPythonWorkerScript(string folderName, string scriptName)
        {
            try
            {
                if (IsPythonWorkerScriptRunning(scriptName))
                {
                    return Ok($"Đã bật {scriptName} từ trước.");
                }

                var basePath = Directory.GetCurrentDirectory();
                var projectPath = Path.GetFullPath(Path.Combine(basePath, "..", "..", "..", ResolveAiRootFolderName(), folderName));
                var scriptPath = Path.Combine(projectPath, scriptName);
                var pythonExe = Path.Combine(projectPath, "venv", "Scripts", "python.exe");

                if (!System.IO.File.Exists(pythonExe))
                {
                    pythonExe = "python"; // fallback
                }

                var psi = new ProcessStartInfo
                {
                    FileName = pythonExe,
                    Arguments = $"\"{scriptPath}\"",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    WorkingDirectory = projectPath
                };

                Process.Start(psi);

                return Ok($"Đã bật {scriptName}");
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        private IActionResult StopPythonWorkerScript(string scriptName)
        {
            try
            {
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "powershell.exe",
                        Arguments = $"-Command \"Get-WmiObject Win32_Process | Where-Object {{ $_.CommandLine -match '{scriptName}' -and $_.Name -eq 'python.exe' }} | ForEach-Object {{ $_.Terminate() }}\"",
                        UseShellExecute = false,
                        CreateNoWindow = true
                    }
                };
                process.Start();
                process.WaitForExit();

                return Ok($"Đã tắt {scriptName}");
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        private bool IsPythonWorkerScriptRunning(string scriptName)
        {
            try
            {
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "powershell.exe",
                        Arguments = $"-Command \"@(Get-WmiObject Win32_Process | Where-Object {{ $_.CommandLine -match '{scriptName}' -and $_.Name -eq 'python.exe' }}).Count\"",
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        RedirectStandardOutput = true
                    }
                };
                process.Start();
                string output = process.StandardOutput.ReadToEnd();
                process.WaitForExit();

                if (int.TryParse(output.Trim(), out int count))
                {
                    return count > 0;
                }
                return false;
            }
            catch
            {
                return false;
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

        private static bool IsDirectWebStream(string? streamUrl)
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

        private static bool ShouldProxyViaGo2Rtc(string? streamUrl) =>
            !string.IsNullOrWhiteSpace(streamUrl) && !IsDirectWebStream(streamUrl);

        private string? BuildCameraViewUrl(string? streamUrl, int cameraId)
        {
            var normalizedStreamUrl = NormalizeCameraUrl(streamUrl);
            if (string.IsNullOrWhiteSpace(normalizedStreamUrl))
            {
                return null;
            }

            if (IsDirectWebStream(normalizedStreamUrl))
            {
                return BuildDirectWebStreamUrl(normalizedStreamUrl);
            }

            var go2RtcPublicBaseUrl = ResolveGo2RtcPublicBaseUrl();
            return $"{go2RtcPublicBaseUrl}/stream.html?src=cam{cameraId}&mode=webrtc";
            //return $"{go2RtcPublicBaseUrl}/stream.html?src=cam{cameraId}&mode=webrtc,mse";
        }

        private string BuildDirectWebStreamUrl(string streamUrl)
        {
            if (!streamUrl.StartsWith("/", StringComparison.Ordinal))
            {
                return streamUrl;
            }

            return $"{ResolvePublicAppBaseUrl()}{streamUrl}";
        }

        private string ResolveGo2RtcPublicBaseUrl()
        {
            var configured = _configuration["AppSettings:Go2RtcPublicBaseUrl"];
            if (!string.IsNullOrWhiteSpace(configured) && !ShouldForceProxyGo2RtcBase(configured))
            {
                return NormalizeBaseUrl(configured);
            }

            // fallback: auto detect nếu có cloudflare host
            var host = Request.Host.Value;

            if (!string.IsNullOrEmpty(host) && host.Contains("maiai06.site"))
            {
                return $"https://{host}";
            }

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
            var configuredFrontendUrl = _configuration["AppSettings:FrontendUrl"];
            if (!string.IsNullOrWhiteSpace(configuredFrontendUrl))
            {
                return NormalizeBaseUrl(configuredFrontendUrl);
            }

            return NormalizeBaseUrl($"{Request.Scheme}://{Request.Host}");
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
        private void EnsureCloudflaredTunnelConfig()
        {
            var cloudflareDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                ".cloudflared"
            );

            if (!Directory.Exists(cloudflareDir))
                Directory.CreateDirectory(cloudflareDir);

            // Tìm file json tunnel
            var jsonFile = Directory.GetFiles(cloudflareDir, "*.json")
                                    .FirstOrDefault();

            if (jsonFile == null)
            {
                throw new Exception("Không tìm thấy file tunnel .json (bạn chưa create tunnel)");
            }

            var configPath = Path.Combine(cloudflareDir, "config.yml");

            var tunnelName = _configuration["Cloudflared:TunnelName"]?.Trim();
            if (string.IsNullOrWhiteSpace(tunnelName))
            {
                tunnelName = Path.GetFileNameWithoutExtension(jsonFile);
            }

            var publicHostname = _configuration["Cloudflared:PublicHostname"]?.Trim();
            if (string.IsNullOrWhiteSpace(publicHostname))
            {
                throw new Exception("Thieu Cloudflared:PublicHostname trong cau hinh.");
            }

            var targetService = _configuration["Cloudflared:TargetService"]?.Trim();
            if (string.IsNullOrWhiteSpace(targetService))
            {
                targetService = "http://localhost:1984";
            }

            var configContent = $@"
tunnel: {tunnelName}
credentials-file: {jsonFile.Replace("\\", "/")}

ingress:
  - hostname: {publicHostname}
    service: {targetService}
  - service: http_status:404
";

            System.IO.File.WriteAllText(configPath, configContent);
        }
        private void StartCloudflaredTunnel()
        {
            var cloudflareDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                ".cloudflared"
            );

            var configPath = Path.Combine(cloudflareDir, "config.yml");

            // kill cũ
            foreach (var proc in Process.GetProcessesByName("cloudflared"))
            {
                proc.Kill();
            }

            Process.Start(new ProcessStartInfo
            {
                FileName = "cloudflared",
                Arguments = $"tunnel --config \"{configPath}\" run",
                UseShellExecute = true,
                CreateNoWindow = true
            });
        }
    }
    }


