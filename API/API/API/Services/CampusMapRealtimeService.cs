using API.Data;
using Microsoft.EntityFrameworkCore;

namespace API.Services;

public interface ICampusMapRealtimeService
{
    Task<CampusMapRealtimeSnapshot> BuildSnapshotAsync(DateTime nowLocal, CancellationToken cancellationToken = default);
}

public class CampusMapRealtimeService : ICampusMapRealtimeService
{
    private static readonly string[] SuccessStatuses = { "APPROVED", "SUCCESS", "GRANTED", "OK", "MATCHED" };
    private readonly ApplicationDbContext _context;

    public CampusMapRealtimeService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<CampusMapRealtimeSnapshot> BuildSnapshotAsync(DateTime nowLocal, CancellationToken cancellationToken = default)
    {
        var fromTime = nowLocal.AddMinutes(-5);

        var gates = await _context.Gates
            .AsNoTracking()
            .Select(g => new
            {
                g.GateId,
                g.GateName,
                g.Location,
                Cameras = g.Cameras.Select(c => new
                {
                    c.CameraId,
                    c.CameraName,
                    c.StreamUrl,
                    c.UrlView
                }).ToList()
            })
            .ToListAsync(cancellationToken);

        var recentLogs = await _context.AccessLogs
            .AsNoTracking()
            .Where(l => l.Timestamp.HasValue && l.Timestamp.Value >= fromTime)
            .OrderByDescending(l => l.Timestamp)
            .Select(l => new
            {
                l.LogId,
                l.Timestamp,
                l.GateId,
                l.CameraId,
                l.Direction,
                l.ResultStatus,
                l.IsBypass,
                l.ExceptionReasonId,
                l.CapturedLicensePlate,
                GateName = l.Gate != null ? l.Gate.GateName : null,
                CameraName = l.Camera != null ? l.Camera.CameraName : null,
                EmployeeName = l.Employee != null ? l.Employee.FullName : null,
                VisitorName = l.VisitorDetail != null ? l.VisitorDetail.FullName : null
            })
            .ToListAsync(cancellationToken);

        var lastAccessByGate = await _context.AccessLogs
            .AsNoTracking()
            .Where(l => l.Timestamp.HasValue && l.GateId.HasValue)
            .GroupBy(l => l.GateId!.Value)
            .Select(g => new
            {
                GateId = g.Key,
                LastAccessAt = g.Max(x => x.Timestamp)
            })
            .ToDictionaryAsync(x => x.GateId, x => x.LastAccessAt, cancellationToken);

        var gateRealtime = new List<CampusGateRealtimeItem>(gates.Count);

        foreach (var gate in gates)
        {
            var gateLogs = recentLogs.Where(l => l.GateId == gate.GateId).ToList();
            var cameraCount = gate.Cameras.Count;
            var offlineCameraCount = gate.Cameras.Count(c => string.IsNullOrWhiteSpace(c.StreamUrl) && string.IsNullOrWhiteSpace(c.UrlView));
            var onlineCameraCount = Math.Max(0, cameraCount - offlineCameraCount);
            var recentAccessCount = gateLogs.Count;
            var hasWarningEvents = gateLogs.Any(l =>
                l.IsBypass == true ||
                l.ExceptionReasonId.HasValue ||
                (!string.IsNullOrWhiteSpace(l.ResultStatus) &&
                 !SuccessStatuses.Contains(l.ResultStatus.Trim().ToUpperInvariant())));

            var status = "Normal";
            var message = "Khong co hoat dong bat thuong.";

            if (cameraCount > 0 && offlineCameraCount == cameraCount)
            {
                status = "Offline";
                message = "Tat ca camera cua cong nay dang offline hoac chua cau hinh stream.";
            }
            else if (offlineCameraCount > 0 || hasWarningEvents)
            {
                status = "Warning";
                message = "Phat hien camera offline hoac su kien canh bao.";
            }
            else if (recentAccessCount > 0)
            {
                status = "Active";
                message = "Co hoat dong moi trong 5 phut gan nhat.";
            }

            gateRealtime.Add(new CampusGateRealtimeItem
            {
                GateId = gate.GateId,
                GateName = gate.GateName,
                Location = gate.Location,
                CameraCount = cameraCount,
                OnlineCameraCount = onlineCameraCount,
                OfflineCameraCount = offlineCameraCount,
                LastAccessAt = lastAccessByGate.TryGetValue(gate.GateId, out var lastAccessAt) ? lastAccessAt : null,
                RecentAccessCount = recentAccessCount,
                Status = status,
                Message = message
            });
        }

        var recentEvents = recentLogs
            .Take(30)
            .Select(l => new CampusMapRecentEvent
            {
                LogId = l.LogId,
                Timestamp = l.Timestamp,
                GateId = l.GateId,
                GateName = l.GateName,
                CameraId = l.CameraId,
                CameraName = l.CameraName,
                Direction = l.Direction,
                ResultStatus = l.ResultStatus,
                CapturedLicensePlate = l.CapturedLicensePlate,
                ActorName = l.EmployeeName ?? l.VisitorName
            })
            .ToList();

        return new CampusMapRealtimeSnapshot
        {
            UpdatedAt = nowLocal,
            Summary = new CampusMapRealtimeSummary
            {
                ActiveGateCount = gateRealtime.Count(g => g.Status == "Active"),
                WarningGateCount = gateRealtime.Count(g => g.Status == "Warning"),
                OfflineCameraCount = gateRealtime.Sum(g => g.OfflineCameraCount),
                RecentEventCount = recentLogs.Count
            },
            Gates = gateRealtime,
            RecentEvents = recentEvents
        };
    }
}

public class CampusMapRealtimeSnapshot
{
    public DateTime UpdatedAt { get; set; }
    public CampusMapRealtimeSummary Summary { get; set; } = new();
    public List<CampusGateRealtimeItem> Gates { get; set; } = new();
    public List<CampusMapRecentEvent> RecentEvents { get; set; } = new();
}

public class CampusMapRealtimeSummary
{
    public int ActiveGateCount { get; set; }
    public int WarningGateCount { get; set; }
    public int OfflineCameraCount { get; set; }
    public int RecentEventCount { get; set; }
}

public class CampusGateRealtimeItem
{
    public int GateId { get; set; }
    public string GateName { get; set; } = string.Empty;
    public string? Location { get; set; }
    public int CameraCount { get; set; }
    public int OnlineCameraCount { get; set; }
    public int OfflineCameraCount { get; set; }
    public DateTime? LastAccessAt { get; set; }
    public int RecentAccessCount { get; set; }
    public string Status { get; set; } = "Normal";
    public string Message { get; set; } = string.Empty;
}

public class CampusMapRecentEvent
{
    public int LogId { get; set; }
    public DateTime? Timestamp { get; set; }
    public int? GateId { get; set; }
    public string? GateName { get; set; }
    public int? CameraId { get; set; }
    public string? CameraName { get; set; }
    public string Direction { get; set; } = string.Empty;
    public string? ResultStatus { get; set; }
    public string? CapturedLicensePlate { get; set; }
    public string? ActorName { get; set; }
}
