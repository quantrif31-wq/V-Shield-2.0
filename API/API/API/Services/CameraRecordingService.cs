using System.Diagnostics;
using API.Data;
using API.Models;
using Microsoft.EntityFrameworkCore;

namespace API.Services;

public class CameraRecordingService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IWebHostEnvironment _env;
    private readonly Dictionary<int, RecordingProcess> _processes = new();
    private readonly object _sync = new();
    private static readonly TimeSpan SegmentDuration = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(30);

    public CameraRecordingService(IServiceScopeFactory scopeFactory, IWebHostEnvironment env)
    {
        _scopeFactory = scopeFactory;
        _env = env;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var cameras = await db.Cameras
                    .Where(c => !string.IsNullOrWhiteSpace(c.StreamUrl) || !string.IsNullOrWhiteSpace(c.UrlView))
                    .ToListAsync(stoppingToken);

                var changed = false;
                foreach (var cam in cameras)
                {
                    if (!cam.IsRecordingEnabled)
                    {
                        cam.IsRecordingEnabled = true;
                        changed = true;
                    }

                    if (cam.RecordingRetentionDays <= 0)
                    {
                        cam.RecordingRetentionDays = 30;
                        changed = true;
                    }
                }

                if (changed)
                {
                    await db.SaveChangesAsync(stoppingToken);
                }

                var enabledIds = new HashSet<int>();
                foreach (var cam in cameras)
                {
                    enabledIds.Add(cam.CameraId);
                    lock (_sync)
                    {
                        if (_processes.ContainsKey(cam.CameraId))
                        {
                            var p = _processes[cam.CameraId];
                            if (p.Process != null && !p.Process.HasExited)
                                continue;
                        }
                    }
                    StartRecording(cam);
                }

                lock (_sync)
                {
                    var toRemove = _processes.Keys.Where(id => !enabledIds.Contains(id)).ToList();
                    foreach (var id in toRemove)
                        StopRecording(id);
                }

                await ScanAndRecordSegments(db, stoppingToken);
                await CleanupOldRecordings(db, stoppingToken);
            }
            catch when (!stoppingToken.IsCancellationRequested)
            {
            }

            await Task.Delay(PollInterval, stoppingToken);
        }
    }

    private void StartRecording(Camera cam)
    {
        var recordsDir = Path.Combine(_env.WebRootPath, "uploads", "recordings", $"cam{cam.CameraId}");
        Directory.CreateDirectory(recordsDir);

        var streamUrl = !string.IsNullOrWhiteSpace(cam.StreamUrl) ? cam.StreamUrl
            : !string.IsNullOrWhiteSpace(cam.UrlView) ? cam.UrlView
            : null;
        if (string.IsNullOrWhiteSpace(streamUrl))
        {
            lock (_sync) _processes[cam.CameraId] = new RecordingProcess { Error = "No stream URL" };
            return;
        }

        var outputPattern = Path.Combine(recordsDir, "%Y-%m-%d", "%Y%m%d_%H%M%S.mp4").Replace("\\", "/");

        var psi = new ProcessStartInfo("ffmpeg")
        {
            Arguments = $"-rtsp_transport tcp -i \"{streamUrl}\" -map 0:v:0 -c copy -an -sn -dn -f segment -segment_time {SegmentDuration.TotalSeconds:F0} -reset_timestamps 1 -strftime 1 -strftime_mkdir 1 \"{outputPattern}\"",
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
        };

        try
        {
            var proc = new Process { StartInfo = psi };
            proc.Start();

            var rp = new RecordingProcess
            {
                Process = proc,
                StartedAt = DateTime.UtcNow,
                CameraId = cam.CameraId,
                Error = null
            };

            _ = Task.Run(() => MonitorProcess(rp));

            lock (_sync) _processes[cam.CameraId] = rp;
        }
        catch (Exception ex)
        {
            lock (_sync) _processes[cam.CameraId] = new RecordingProcess { Error = ex.Message };
        }
    }

    private async Task MonitorProcess(RecordingProcess rp)
    {
        if (rp.Process == null) return;

        try
        {
            var stderr = await rp.Process.StandardError.ReadToEndAsync();
            rp.Process.WaitForExit();
            rp.Error = stderr.Length > 200 ? stderr[^200..] : stderr;
        }
        catch
        {
        }
    }

    private void StopRecording(int cameraId)
    {
        if (!_processes.TryGetValue(cameraId, out var rp)) return;
        if (rp.Process != null && !rp.Process.HasExited)
        {
            try { rp.Process.Kill(entireProcessTree: true); } catch { }
            try { rp.Process.Dispose(); } catch { }
        }
        _processes.Remove(cameraId);
    }

    private async Task CleanupOldRecordings(ApplicationDbContext db, CancellationToken ct)
    {
        var cameras = await db.Cameras.Where(c => c.RecordingRetentionDays > 0).ToListAsync(ct);
        foreach (var cam in cameras)
        {
            var cutoff = DateTime.UtcNow.AddDays(-cam.RecordingRetentionDays);
            var dir = Path.Combine(_env.WebRootPath, "uploads", "recordings", $"cam{cam.CameraId}");
            if (!Directory.Exists(dir)) continue;

            foreach (var filePath in Directory.GetFiles(dir, "*.mp4", SearchOption.AllDirectories))
            {
                var startedAt = TryResolveSegmentStartedAt(filePath);
                if (startedAt.HasValue && startedAt.Value < cutoff)
                {
                    try { File.Delete(filePath); } catch { }
                }
            }

            var oldSegments = await db.RecordedSegments
                .Where(s => s.CameraId == cam.CameraId && s.StartedAt < cutoff)
                .ToListAsync(ct);
            if (oldSegments.Count > 0)
            {
                db.RecordedSegments.RemoveRange(oldSegments);
                await db.SaveChangesAsync(ct);
            }
        }
    }

    private async Task ScanAndRecordSegments(ApplicationDbContext db, CancellationToken ct)
    {
        var recordingsRoot = Path.Combine(_env.WebRootPath, "uploads", "recordings");
        if (!Directory.Exists(recordingsRoot)) return;

        var existingPaths = new HashSet<string>(
            await db.RecordedSegments.Select(s => s.FilePath).ToListAsync(ct),
            StringComparer.OrdinalIgnoreCase
        );

        foreach (var camDir in Directory.GetDirectories(recordingsRoot))
        {
            var dirName = Path.GetFileName(camDir);
            if (!dirName.StartsWith("cam", StringComparison.OrdinalIgnoreCase)) continue;
            if (!int.TryParse(dirName.AsSpan(3), out var cameraId)) continue;

            foreach (var filePath in Directory.GetFiles(camDir, "*.mp4", SearchOption.AllDirectories))
            {
                if (existingPaths.Contains(filePath)) continue;

                var startedAt = TryResolveSegmentStartedAt(filePath) ?? DateTime.UtcNow;
                var fileInfo = new FileInfo(filePath);
                var duration = await ProbeDuration(filePath);

                var relativePath = Path.GetRelativePath(
                    Path.Combine(_env.WebRootPath, "uploads"),
                    filePath
                ).Replace("\\", "/");
                var storageUrl = $"/uploads/{relativePath}";

                var segment = new RecordedSegment
                {
                    CameraId = cameraId,
                    StartedAt = startedAt,
                    EndedAt = startedAt.AddSeconds(duration > 0 ? duration : 300),
                    FilePath = filePath,
                    FileSizeBytes = fileInfo.Length,
                    DurationSeconds = duration > 0 ? duration : 300,
                    StorageUrl = storageUrl
                };

                db.RecordedSegments.Add(segment);
                existingPaths.Add(filePath);
            }
        }

        await db.SaveChangesAsync(ct);
    }

    private static async Task<double> ProbeDuration(string filePath)
    {
        try
        {
            var psi = new ProcessStartInfo("ffprobe")
            {
                Arguments = $"-v error -show_entries format=duration -of default=noprint_wrappers=1:nokey=1 \"{filePath}\"",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
            };
            using var proc = new Process { StartInfo = psi };
            proc.Start();
            var output = await proc.StandardOutput.ReadToEndAsync();
            await proc.WaitForExitAsync();
            if (double.TryParse(output.Trim(), System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture, out var dur))
                return dur;
        }
        catch
        {
        }
        return 0;
    }

    private static DateTime? TryResolveSegmentStartedAt(string filePath)
    {
        var fileName = Path.GetFileNameWithoutExtension(filePath);
        if (DateTime.TryParseExact(
            fileName,
            "yyyyMMdd_HHmmss",
            System.Globalization.CultureInfo.InvariantCulture,
            System.Globalization.DateTimeStyles.AssumeLocal,
            out var parsed))
        {
            return DateTime.SpecifyKind(parsed, DateTimeKind.Local).ToUniversalTime();
        }

        var dateDir = Path.GetFileName(Path.GetDirectoryName(filePath));
        if (DateTime.TryParseExact(
            dateDir,
            "yyyy-MM-dd",
            System.Globalization.CultureInfo.InvariantCulture,
            System.Globalization.DateTimeStyles.AssumeLocal,
            out var parsedDate))
        {
            return DateTime.SpecifyKind(parsedDate, DateTimeKind.Local).ToUniversalTime();
        }

        return null;
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        lock (_sync)
        {
            foreach (var id in _processes.Keys.ToList())
                StopRecording(id);
        }
        await base.StopAsync(cancellationToken);
    }
}

public class RecordingProcess
{
    public int CameraId { get; set; }
    public Process? Process { get; set; }
    public DateTime StartedAt { get; set; }
    public string? Error { get; set; }
}
