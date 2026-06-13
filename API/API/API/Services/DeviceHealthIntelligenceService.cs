using API.Data;
using API.Models;
using API.Services.AI;
using Microsoft.EntityFrameworkCore;

namespace API.Services;

public interface IDeviceHealthIntelligenceService
{
    Task<List<DeviceHealthInsight>> GetAllInsightsAsync();
    Task<AiRecommendationResult> DiagnoseDeviceAsync(int deviceId, int? requestedByUserId);
}

public class DeviceHealthInsight
{
    public int DeviceId { get; set; }
    public string DeviceName { get; set; } = string.Empty;
    public string DeviceType { get; set; } = string.Empty;
    public string Status { get; set; } = "Unknown";
    public string PredictedStatus { get; set; } = "Unknown";
    public DateTime? LastSeenAtUtc { get; set; }
    public int? LatencyMs { get; set; }
    public int RestartCount { get; set; }
    public double FailureRate { get; set; }
    public string? Insight { get; set; }
}

public class DeviceHealthIntelligenceService : IDeviceHealthIntelligenceService
{
    private readonly ApplicationDbContext _db;
    private readonly IAiRecommendationService _aiRec;

    public DeviceHealthIntelligenceService(ApplicationDbContext db, IAiRecommendationService aiRec)
    {
        _db = db;
        _aiRec = aiRec;
    }

    public async Task<List<DeviceHealthInsight>> GetAllInsightsAsync()
    {
        var devices = await _db.SecurityDevices.AsNoTracking()
            .Where(d => d.IsActive)
            .ToListAsync();

        var snapshots = await _db.DeviceHealthSnapshots.AsNoTracking()
            .OrderByDescending(s => s.CapturedAtUtc)
            .ToListAsync();

        var insights = new List<DeviceHealthInsight>();
        foreach (var device in devices)
        {
            var deviceSnapshots = snapshots.Where(s => s.SecurityDeviceId == device.SecurityDeviceId).ToList();
            var latestSnapshot = deviceSnapshots.FirstOrDefault();

            var now = DateTime.UtcNow;
            var staleThreshold = now.AddMinutes(-15);
            var warningThreshold = now.AddMinutes(-60);

            string predictedStatus;
            string? insight = null;

            if (device.Status == "Offline" || device.Status == "Tamper" || device.Status == "Fault")
            {
                predictedStatus = device.Status;
                insight = $"Thiet bi dang o trang thai '{device.Status}'. Can kiem tra ngay.";
            }
            else if (device.LastSeenAtUtc < staleThreshold || latestSnapshot == null)
            {
                predictedStatus = "Stale";
                var lastSeenForCalc = device.LastSeenAtUtc ?? latestSnapshot?.CapturedAtUtc ?? now.AddDays(-1);
                insight = $"Thiet bi khong co heartbeat trong {(now - lastSeenForCalc).TotalMinutes:F0} phut. Co the da offline.";
            }
            else if (device.LastSeenAtUtc < warningThreshold)
            {
                predictedStatus = "Degraded";
                insight = "Thiet bi co heartbeat nhung canh bao tre. Kiem tra ket noi mang va nguon.";
            }
            else
            {
                predictedStatus = "Online";
                if (deviceSnapshots.Count > 5)
                {
                    var failureCount = deviceSnapshots.Count(s => s.Status == "Fault" || s.Status == "Tamper");
                    var failureRate = failureCount * 100.0 / deviceSnapshots.Count;
                    if (failureRate > 20)
                    {
                        predictedStatus = "AtRisk";
                        insight = $"Ty le loi {failureRate:F0}% trong {deviceSnapshots.Count} lan kiem tra gan day. Can bao tri.";
                    }
                }
            }

            insights.Add(new DeviceHealthInsight
            {
                DeviceId = device.SecurityDeviceId,
                DeviceName = device.Name,
                DeviceType = device.DeviceType,
                Status = device.Status,
                PredictedStatus = predictedStatus,
                LastSeenAtUtc = device.LastSeenAtUtc ?? latestSnapshot?.CapturedAtUtc,
                LatencyMs = latestSnapshot?.LatencyMs,
                RestartCount = deviceSnapshots.Count(s => s.Status == "Restart" || s.Status == "Reboot"),
                FailureRate = deviceSnapshots.Count > 0
                    ? deviceSnapshots.Count(s => s.Status == "Fault" || s.Status == "Tamper") * 100.0 / deviceSnapshots.Count
                    : 0,
                Insight = insight
            });
        }

        return insights;
    }

    public async Task<AiRecommendationResult> DiagnoseDeviceAsync(int deviceId, int? requestedByUserId)
    {
        var device = await _db.SecurityDevices.AsNoTracking()
            .FirstOrDefaultAsync(d => d.SecurityDeviceId == deviceId);
        if (device == null)
            throw new KeyNotFoundException($"Device {deviceId} not found.");

        var deviceSnapshots = await _db.DeviceHealthSnapshots.AsNoTracking()
            .Where(s => s.SecurityDeviceId == deviceId)
            .OrderByDescending(s => s.CapturedAtUtc)
            .Take(20)
            .ToListAsync();

        var inputData = new Dictionary<string, string>
        {
            ["device_name"] = device.Name,
            ["device_type"] = device.DeviceType,
            ["status"] = device.Status,
            ["last_seen"] = device.LastSeenAtUtc?.ToString("yyyy-MM-dd HH:mm:ss UTC") ?? "Chua co du lieu",
            ["latency"] = deviceSnapshots.Any() ? $"{deviceSnapshots.Average(s => s.LatencyMs ?? 0):F0}ms" : "N/A",
            ["restart_count"] = deviceSnapshots.Count(s => s.Status == "Restart" || s.Status == "Reboot").ToString(),
            ["failure_rate"] = deviceSnapshots.Count > 0
                ? $"{deviceSnapshots.Count(s => s.Status == "Fault" || s.Status == "Tamper") * 100.0 / deviceSnapshots.Count:F0}%"
                : "0%",
            ["firmware"] = device.FirmwareVersion ?? "N/A",
            ["serial"] = device.SerialNumber ?? "N/A",
            ["vendor"] = device.Vendor ?? "N/A"
        };

        return await _aiRec.AnalyzeAsync(
            "device", "device", deviceId.ToString(),
            "device-health-diagnosis", inputData, requestedByUserId);
    }
}
