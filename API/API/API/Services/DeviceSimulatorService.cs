using System.Text.Json;
using API.Data;
using API.Models;
using API.Services.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace API.Services;

public class DeviceSimulatorService : IDeviceSimulator
{
    private readonly ApplicationDbContext _context;

    public string SimulatorType => "V-Shield EnterpriseEdge-Sim";

    public DeviceSimulatorService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<SecurityDevice> CreateVirtualDeviceAsync(string name, int? siteId, int? accessPointId, CancellationToken ct = default)
    {
        var device = new SecurityDevice
        {
            SiteId = siteId,
            AccessPointId = accessPointId,
            DeviceType = "VirtualController",
            Name = name.Trim(),
            Vendor = "V-Shield Simulator",
            Model = "EnterpriseEdge-Sim",
            SerialNumber = $"SIM-{Guid.NewGuid():N}"[..16],
            FirmwareVersion = "sim-1.0",
            ConfigurationVersion = "initial",
            Status = "Ok",
            LastSeenAtUtc = DateTime.UtcNow
        };
        _context.SecurityDevices.Add(device);
        await _context.SaveChangesAsync(ct);

        _context.AccessControllerDevices.Add(new AccessControllerDevice
        {
            SecurityDeviceId = device.SecurityDeviceId,
            Protocol = "OSDP-Sim",
            SupportsOfflineDecision = true,
            MaxCredentials = 50000
        });
        _context.ReaderDevices.Add(new ReaderDevice
        {
            SecurityDeviceId = device.SecurityDeviceId,
            ReaderProtocol = "OSDP-Sim",
            CredentialFormats = "QR,Card,Pin"
        });
        _context.DeviceRelays.Add(new DeviceRelay
        {
            SecurityDeviceId = device.SecurityDeviceId,
            Name = "door-relay",
            State = "Secure"
        });
        _context.DeviceSensors.Add(new DeviceSensor
        {
            SecurityDeviceId = device.SecurityDeviceId,
            SensorType = "Tamper",
            State = "Normal"
        });
        _context.DeviceHealthSnapshots.Add(new DeviceHealthSnapshot
        {
            SecurityDeviceId = device.SecurityDeviceId,
            Status = "Ok",
                Message = "Bộ điều khiển ảo được tạo qua DeviceSimulatorService."
        });
        await _context.SaveChangesAsync(ct);
        return device;
    }

    public async Task<AccessDecision> SimulateOfflineDecisionAsync(int deviceId, string subjectType, int? subjectId, string credentialType, CancellationToken ct = default)
    {
        var device = await _context.SecurityDevices.FindAsync(new object[] { deviceId }, ct);
        if (device == null)
            throw new KeyNotFoundException("Device not found.");

        var package = await _context.OfflinePolicyPackages
            .Where(p => p.SecurityDeviceId == deviceId && p.Status == "Published")
            .OrderByDescending(p => p.PublishedAtUtc)
            .FirstOrDefaultAsync(ct);

        var now = DateTime.UtcNow;
        var allowed = PackageAllows(package?.PayloadJson, subjectType, subjectId, credentialType);

        var decision = new AccessDecision
        {
            SubjectType = subjectType.Trim(),
            SubjectId = subjectId,
            SiteId = device.SiteId,
            AccessPointId = device.AccessPointId,
            CredentialType = credentialType.Trim(),
            Result = allowed ? AccessDecisionResults.Allow : AccessDecisionResults.Deny,
            Reason = allowed
                ? $"Offline simulator package {package?.PackageVersion} allowed credential."
                : "Offline simulator denied because no published package rule matched.",
            EvaluatedAtUtc = now
        };
        _context.AccessDecisions.Add(decision);

        _context.SecurityEvents.Add(new SecurityEvent
        {
            SourceType = "DeviceSimulator",
            SourceId = deviceId.ToString(),
            EventType = allowed ? "OfflineAccessGranted" : "OfflineAccessDenied",
            Severity = allowed ? "Info" : "Medium",
            SiteId = device.SiteId,
            AccessPointId = device.AccessPointId,
            SubjectType = decision.SubjectType,
            SubjectId = decision.SubjectId,
            Summary = decision.Reason,
            OccurredAtUtc = now
        });

        device.Status = "Ok";
        device.LastSeenAtUtc = now;
        await _context.SaveChangesAsync(ct);
        return decision;
    }

    public async Task<bool> InjectFaultAsync(int deviceId, string faultType, string severity, string? message, CancellationToken ct = default)
    {
        var device = await _context.SecurityDevices.FindAsync(new object[] { deviceId }, ct);
        if (device == null) return false;

        device.Status = faultType.Trim();
        device.LastSeenAtUtc = DateTime.UtcNow;

        _context.DeviceHealthSnapshots.Add(new DeviceHealthSnapshot
        {
            SecurityDeviceId = deviceId,
            Status = faultType.Trim(),
            Message = message?.Trim() ?? "Đã chèn lỗi mô phỏng."
        });
        _context.Alarms.Add(new Alarm
        {
            AlarmType = faultType.Trim(),
            Severity = severity.Trim(),
            State = "New",
            Summary = $"Lỗi mô phỏng thiết bị {device.Name}: {faultType}.",
            SiteId = device.SiteId
        });
        await _context.SaveChangesAsync(ct);
        return true;
    }

    public async Task<bool> RestoreNormalAsync(int deviceId, CancellationToken ct = default)
    {
        var device = await _context.SecurityDevices.FindAsync(new object[] { deviceId }, ct);
        if (device == null) return false;

        device.Status = "Ok";
        device.LastSeenAtUtc = DateTime.UtcNow;

        _context.DeviceHealthSnapshots.Add(new DeviceHealthSnapshot
        {
            SecurityDeviceId = deviceId,
            Status = "Ok",
            Message = "Thiết bị đã khôi phục bình thường qua mô phỏng."
        });
        await _context.SaveChangesAsync(ct);
        return true;
    }

    private static bool PackageAllows(string? payloadJson, string? subjectType, int? subjectId, string? credentialType)
    {
        if (string.IsNullOrWhiteSpace(payloadJson)) return false;
        try
        {
            using var json = JsonDocument.Parse(payloadJson);
            var root = json.RootElement;
            if (root.TryGetProperty("allowAll", out var allowAll) && allowAll.ValueKind == JsonValueKind.True)
                return true;
            if (!root.TryGetProperty("allowedSubjects", out var subjects) || subjects.ValueKind != JsonValueKind.Array)
                return false;
            foreach (var subject in subjects.EnumerateArray())
            {
                var typeMatches = !subject.TryGetProperty("subjectType", out var type) ||
                                  string.Equals(type.GetString(), subjectType, StringComparison.OrdinalIgnoreCase);
                var idMatches = !subject.TryGetProperty("subjectId", out var id) ||
                                (subjectId.HasValue && id.GetInt32() == subjectId.Value);
                var credentialMatches = !subject.TryGetProperty("credentialType", out var credential) ||
                                        string.Equals(credential.GetString(), "Any", StringComparison.OrdinalIgnoreCase) ||
                                        string.Equals(credential.GetString(), credentialType, StringComparison.OrdinalIgnoreCase);
                if (typeMatches && idMatches && credentialMatches) return true;
            }
        }
        catch (JsonException)
        {
            return payloadJson.Contains("\"allowAll\":true", StringComparison.OrdinalIgnoreCase);
        }
        return false;
    }
}
