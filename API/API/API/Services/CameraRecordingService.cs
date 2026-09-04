using System.Collections.Concurrent;
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
    // DVR HLS: one continuous, seekable timeline per camera/day. The small fMP4
    // pieces are implementation details; the operator sees one video timeline.
    private const int DvrSegmentSeconds = 4;
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(3);
    private static readonly TimeSpan SegmentPublishDelay = TimeSpan.FromSeconds(2);
    private static readonly TimeSpan StartupTimeout = TimeSpan.FromSeconds(15);

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
                    .Where(c =>
                        (!string.IsNullOrWhiteSpace(c.StreamUrl) || !string.IsNullOrWhiteSpace(c.UrlView)))
                    .ToListAsync(stoppingToken);

                var enabledIds = new ConcurrentDictionary<int, byte>();
                var parallelOpts = new ParallelOptions { MaxDegreeOfParallelism = 4 };
                await Parallel.ForEachAsync(cameras, parallelOpts, (cam, ct) =>
                {
                    enabledIds.TryAdd(cam.CameraId, 0);
                    lock (_sync)
                    {
                        if (_processes.TryGetValue(cam.CameraId, out var p) && p.Process != null && !p.Process.HasExited)
                        {
                            if (p.RecordingDayLocal == DateOnly.FromDateTime(DateTime.Now))
                                return ValueTask.CompletedTask;
                            StopRecording(cam.CameraId); // Close yesterday's DVR cleanly at day rollover.
                        }
                    }
                    StartRecording(cam);
                    return ValueTask.CompletedTask;
                });

                lock (_sync)
                {
                    var toRemove = _processes.Keys.Where(id => !enabledIds.ContainsKey(id)).ToList();
                    foreach (var id in toRemove)
                        StopRecording(id);
                }

                PublishDvrTimelines();
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
        var day = DateOnly.FromDateTime(DateTime.Now);
        var recordsDir = Path.Combine(_env.WebRootPath, "uploads", "recordings", $"cam{cam.CameraId}", "dvr", day.ToString("yyyy-MM-dd"));
        Directory.CreateDirectory(recordsDir);

        // Older releases wrote ffmpeg directly into index.m3u8. Preserve that
        // timeline before new recorder attempts begin, then treat index.m3u8 as
        // an atomically-published read model only.
        PreserveLegacyTimeline(recordsDir);

        var candidates = ResolveInputCandidates(cam).ToList();
        if (candidates.Count == 0)
        {
            lock (_sync) _processes[cam.CameraId] = new RecordingProcess { Error = "No stream URL" };
            return;
        }

        var sessionId = $"{DateTime.UtcNow:HHmmssfff}_{Guid.NewGuid():N}";
        var playlistPath = Path.Combine(recordsDir, $"session_{sessionId}.m3u8").Replace("\\", "/");
        TryStartWithCandidate(cam.CameraId, candidates, 0, playlistPath, day, sessionId);
    }

    private void TryStartWithCandidate(int cameraId, List<RecordingInputCandidate> candidates, int index, string playlistPath, DateOnly recordingDayLocal, string sessionId)
    {
        if (index >= candidates.Count)
        {
            lock (_sync) _processes[cameraId] = new RecordingProcess
            {
                CameraId = cameraId,
                Error = "All candidates failed"
            };
            return;
        }

        var candidate = candidates[index];

        var psi = new ProcessStartInfo("ffmpeg")
        {
            Arguments = BuildFfmpegArguments(candidate.InputUrl, playlistPath, sessionId),
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
        };

        try
        {
            var proc = new Process { StartInfo = psi };
            proc.Start();

            if (proc.WaitForExit((int)StartupTimeout.TotalMilliseconds))
            {
                var error = proc.StandardError.ReadToEnd();
                Console.WriteLine($"[recording] Camera {cameraId} failed on {candidate.SourceLabel}: {TrimForLog(error)}");
                proc.Dispose();
                TryStartWithCandidate(cameraId, candidates, index + 1, playlistPath, recordingDayLocal, sessionId);
                return;
            }

            var rp = new RecordingProcess
            {
                Process = proc,
                StartedAt = DateTime.UtcNow,
                CameraId = cameraId,
                ActiveInput = candidate.InputUrl,
                SourceLabel = candidate.SourceLabel,
                Candidates = candidates,
                CandidateIndex = index,
                OutputPattern = playlistPath,
                RecordingDayLocal = recordingDayLocal,
                SessionId = sessionId,
                Error = null
            };

            Console.WriteLine($"[recording] Camera {cameraId} recording started via {candidate.SourceLabel}: {candidate.InputUrl}");
            _ = Task.Run(() => MonitorProcess(rp));

            lock (_sync) _processes[cameraId] = rp;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[recording] Camera {cameraId} start error on {candidate.SourceLabel}: {ex.Message}");
            TryStartWithCandidate(cameraId, candidates, index + 1, playlistPath, recordingDayLocal, sessionId);
        }
    }

    private async Task MonitorProcess(RecordingProcess rp)
    {
        if (rp.Process == null) return;

        try
        {
            var stderr = await rp.Process.StandardError.ReadToEndAsync();
            rp.Process.WaitForExit();
            rp.Error = TrimForLog(stderr);
            Console.WriteLine($"[recording] Camera {rp.CameraId} recorder exited from {rp.SourceLabel ?? "unknown"}: {rp.Error}");

            if (rp.Candidates != null && rp.SourceLabel != null)
            {
                TryStartWithCandidate(rp.CameraId, rp.Candidates, rp.CandidateIndex + 1, rp.OutputPattern ?? "", rp.RecordingDayLocal, rp.SessionId ?? Guid.NewGuid().ToString("N"));
            }
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

    private static void PreserveLegacyTimeline(string recordsDir)
    {
        var legacyPath = Path.Combine(recordsDir, "legacy.m3u8");
        var publishedPath = Path.Combine(recordsDir, "index.m3u8");
        if (File.Exists(legacyPath) || !File.Exists(publishedPath)) return;

        try
        {
            File.Copy(publishedPath, legacyPath, overwrite: false);
        }
        catch
        {
            // The next recorder loop retries. Do not block camera recording for
            // a transient file-system race.
        }
    }

    // index.m3u8 is never a direct ffmpeg output. It is a stable, atomically
    // replaced day timeline assembled from completed/current recording sessions.
    // A failed reconnect can therefore leave only an ignored empty session and
    // can never truncate the evidence already visible to the archive player.
    private void PublishDvrTimelines()
    {
        var recordingsRoot = Path.Combine(_env.WebRootPath, "uploads", "recordings");
        if (!Directory.Exists(recordingsRoot)) return;

        foreach (var dvrDir in Directory.GetDirectories(recordingsRoot, "dvr", SearchOption.AllDirectories))
        {
            foreach (var dayDir in Directory.GetDirectories(dvrDir))
            {
                try
                {
                    PublishDvrTimeline(dayDir);
                }
                catch
                {
                    // A playlist can be mid-write while ffmpeg rolls a segment.
                    // Keep the previous atomically-published version until the
                    // following loop rather than serving a partial manifest.
                }
            }
        }
    }

    private static void PublishDvrTimeline(string recordsDir)
    {
        var outputPath = Path.Combine(recordsDir, "index.m3u8");
        var sources = Directory.GetFiles(recordsDir, "*.m3u8")
            .Where(path => !string.Equals(Path.GetFileName(path), "index.m3u8", StringComparison.OrdinalIgnoreCase))
            .Where(path => string.Equals(Path.GetFileName(path), "legacy.m3u8", StringComparison.OrdinalIgnoreCase)
                || Path.GetFileName(path).StartsWith("session_", StringComparison.OrdinalIgnoreCase))
            .OrderBy(path => string.Equals(Path.GetFileName(path), "legacy.m3u8", StringComparison.OrdinalIgnoreCase) ? 0 : 1)
            .ThenBy(path => path, StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (sources.Count == 0) return;

        var merged = new List<string>
        {
            "#EXTM3U",
            "#EXT-X-VERSION:7",
            "#EXT-X-TARGETDURATION:10",
            "#EXT-X-PLAYLIST-TYPE:EVENT",
            "#EXT-X-INDEPENDENT-SEGMENTS"
        };
        var publishedSessionCount = 0;

        foreach (var sourcePath in sources)
        {
            var lines = File.ReadAllLines(sourcePath);
            if (!HasPlayableMedia(lines, recordsDir)) continue;

            if (publishedSessionCount > 0)
                merged.Add("#EXT-X-DISCONTINUITY");

            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line) ||
                    line.StartsWith("#EXTM3U", StringComparison.Ordinal) ||
                    line.StartsWith("#EXT-X-VERSION", StringComparison.Ordinal) ||
                    line.StartsWith("#EXT-X-TARGETDURATION", StringComparison.Ordinal) ||
                    line.StartsWith("#EXT-X-MEDIA-SEQUENCE", StringComparison.Ordinal) ||
                    line.StartsWith("#EXT-X-PLAYLIST-TYPE", StringComparison.Ordinal) ||
                    line.StartsWith("#EXT-X-INDEPENDENT-SEGMENTS", StringComparison.Ordinal) ||
                    line.StartsWith("#EXT-X-ENDLIST", StringComparison.Ordinal) ||
                    line.StartsWith("#EXT-X-DISCONTINUITY", StringComparison.Ordinal))
                {
                    continue;
                }

                merged.Add(line);
            }

            publishedSessionCount += 1;
        }

        if (publishedSessionCount == 0) return;

        var tempPath = outputPath + ".tmp";
        File.WriteAllLines(tempPath, merged);
        File.Move(tempPath, outputPath, overwrite: true);
    }

    private static bool HasPlayableMedia(IEnumerable<string> lines, string recordsDir)
    {
        var snapshot = lines.ToList();
        var hasTimedSegment = snapshot
            .Where(line => line.StartsWith("#EXTINF:", StringComparison.Ordinal))
            .Select(line => double.TryParse(
                line[8..].TrimEnd(','),
                System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture,
                out var duration) ? duration : 0)
            .Any(duration => duration > 0.01);
        if (!hasTimedSegment) return false;

        var mapLine = snapshot.LastOrDefault(line => line.StartsWith("#EXT-X-MAP:", StringComparison.Ordinal));
        if (string.IsNullOrWhiteSpace(mapLine)) return false;
        var marker = "URI=\"";
        var start = mapLine.IndexOf(marker, StringComparison.Ordinal);
        if (start < 0) return false;
        start += marker.Length;
        var end = mapLine.IndexOf('"', start);
        if (end <= start) return false;

        var initPath = Path.Combine(recordsDir, mapLine[start..end]);
        return File.Exists(initPath) && new FileInfo(initPath).Length > 0;
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

            // DVR is stored as one HLS timeline per local calendar day. Keep the
            // current timeline intact while it is recording; remove only complete
            // days that have passed the same camera retention policy.
            var dvrDir = Path.Combine(dir, "dvr");
            if (Directory.Exists(dvrDir))
            {
                var firstKeptDay = DateOnly.FromDateTime(DateTime.Now.AddDays(-cam.RecordingRetentionDays));
                foreach (var dayDir in Directory.GetDirectories(dvrDir))
                {
                    if (DateOnly.TryParseExact(
                            Path.GetFileName(dayDir),
                            "yyyy-MM-dd",
                            System.Globalization.CultureInfo.InvariantCulture,
                            System.Globalization.DateTimeStyles.None,
                            out var recordingDay) && recordingDay < firstKeptDay)
                    {
                        try { Directory.Delete(dayDir, recursive: true); } catch { }
                    }
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

                // The current segment is still being written and its MP4 index
                // does not exist yet. Publishing it produced unplayable rows.
                var fileInfo = new FileInfo(filePath);
                if (fileInfo.LastWriteTimeUtc > DateTime.UtcNow.Subtract(SegmentPublishDelay)) continue;

                var startedAt = TryResolveSegmentStartedAt(filePath) ?? DateTime.UtcNow;
                var duration = await ProbeDuration(filePath);
                if (duration <= 0) continue;

                var relativePath = Path.GetRelativePath(
                    Path.Combine(_env.WebRootPath, "uploads"),
                    filePath
                ).Replace("\\", "/");
                var storageUrl = $"/uploads/{relativePath}";

                var segment = new RecordedSegment
                {
                    CameraId = cameraId,
                    StartedAt = startedAt,
                    EndedAt = startedAt.AddSeconds(duration),
                    FilePath = filePath,
                    FileSizeBytes = fileInfo.Length,
                    DurationSeconds = duration,
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

    private static IEnumerable<RecordingInputCandidate> ResolveInputCandidates(Camera cam)
    {
        var candidates = new List<RecordingInputCandidate>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        void AddCandidate(string? inputUrl, string sourceLabel)
        {
            if (string.IsNullOrWhiteSpace(inputUrl)) return;

            var trimmed = inputUrl.Trim();
            if (!seen.Add(trimmed)) return;
            candidates.Add(new RecordingInputCandidate(trimmed, sourceLabel));
        }

        if (TryBuildInternalPreviewUrl(cam.StreamUrl, out var internalPreviewUrl))
        {
            AddCandidate(internalPreviewUrl, "internal-preview");
        }
        else
        {
            // Demo stream URLs are routing markers, not resolvable RTSP hosts.
            // Real camera URLs remain the first direct recording candidate.
            AddCandidate(cam.StreamUrl, "stream-url");
        }

        if (TryGetGo2RtcSource(cam.UrlView, out var src) && src != null)
        {
            AddCandidate($"rtsp://go2rtc:8554/{src}", "go2rtc-relay");
            AddCandidate($"http://go2rtc:1984/api/stream.mjpeg?src={Uri.EscapeDataString(src)}", "go2rtc-mjpeg");
        }

        AddCandidate(RewriteContainerLocalUrl(cam.UrlView), "url-view");
        return candidates;
    }

    private static bool TryBuildInternalPreviewUrl(string? streamUrl, out string? internalPreviewUrl)
    {
        internalPreviewUrl = null;
        if (string.IsNullOrWhiteSpace(streamUrl))
        {
            return false;
        }

        if (streamUrl.Equals("rtsp://demo.local/qr", StringComparison.OrdinalIgnoreCase))
        {
            internalPreviewUrl = "http://frontend/qr-api/qr/frame.jpg";
            return true;
        }

        if (streamUrl.Equals("rtsp://demo.local/plate", StringComparison.OrdinalIgnoreCase))
        {
            internalPreviewUrl = "http://frontend/plate-api/api/camera/stream";
            return true;
        }

        return false;
    }

    private static string? RewriteContainerLocalUrl(string? inputUrl)
    {
        if (string.IsNullOrWhiteSpace(inputUrl) ||
            !Uri.TryCreate(inputUrl, UriKind.Absolute, out var uri))
        {
            return inputUrl;
        }

        if (!string.Equals(uri.Host, "localhost", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(uri.Host, "127.0.0.1", StringComparison.OrdinalIgnoreCase))
        {
            return inputUrl;
        }

        var builder = new UriBuilder(uri)
        {
            Scheme = Uri.UriSchemeHttp,
            Host = "frontend",
            Port = 80
        };
        return builder.Uri.ToString();
    }

    private static bool TryGetGo2RtcSource(string? urlView, out string? src)
    {
        src = null;
        if (string.IsNullOrWhiteSpace(urlView)) return false;
        if (!Uri.TryCreate(urlView, UriKind.Absolute, out var parsed)) return false;
        if (!parsed.AbsolutePath.Contains("/stream.html", StringComparison.OrdinalIgnoreCase)) return false;

        src = GetQueryValue(parsed.Query, "src");
        return !string.IsNullOrWhiteSpace(src);
    }

    private static string? GetQueryValue(string query, string key)
    {
        if (string.IsNullOrWhiteSpace(query)) return null;

        foreach (var pair in query.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var parts = pair.Split('=', 2);
            if (!string.Equals(parts[0], key, StringComparison.OrdinalIgnoreCase)) continue;
            return parts.Length > 1 ? Uri.UnescapeDataString(parts[1]) : string.Empty;
        }

        return null;
    }

    private static string BuildFfmpegArguments(string inputUrl, string playlistPath, string sessionId)
    {
        var segmentPattern = Path.Combine(Path.GetDirectoryName(playlistPath)!, $"segment_{sessionId}_%06d.m4s").Replace("\\", "/");
        var initFileName = $"init_{sessionId}.mp4";
        var codec = inputUrl.StartsWith("rtsp://", StringComparison.OrdinalIgnoreCase)
            ? "-rtsp_transport tcp -timeout 8000000 -i \"" + inputUrl + "\" -map 0:v:0 -c copy"
            : "-fflags nobuffer -flags low_delay -rw_timeout 8000000 -i \"" + inputUrl + "\" -c:v libx264 -preset ultrafast -tune zerolatency -pix_fmt yuv420p";

        return $"{codec} -an -sn -dn -f hls -hls_time {DvrSegmentSeconds} -hls_list_size 0 -hls_playlist_type event -hls_segment_type fmp4 -hls_fmp4_init_filename {initFileName} -hls_flags independent_segments+program_date_time+temp_file -hls_segment_filename \"{segmentPattern}\" \"{playlistPath}\"";

        /* Legacy MP4 segmentation retained below for reference of old files.
        if (inputUrl.StartsWith("rtsp://", StringComparison.OrdinalIgnoreCase))
        {
            return string.Empty;
        }
        */
    }

    private static string TrimForLog(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return "no details";

        var normalized = value.ReplaceLineEndings(" ").Trim();
        return normalized.Length > 240 ? normalized[^240..] : normalized;
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
    public string? ActiveInput { get; set; }
    public string? SourceLabel { get; set; }
    public List<RecordingInputCandidate>? Candidates { get; set; }
    public int CandidateIndex { get; set; }
    public string? OutputPattern { get; set; }
    public DateOnly RecordingDayLocal { get; set; }
    public string? SessionId { get; set; }
}

public record RecordingInputCandidate(string InputUrl, string SourceLabel);
