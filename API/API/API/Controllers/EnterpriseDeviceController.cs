using System.Security.Claims;
using System.Text.Json;
using API.Data;
using API.Middleware;
using API.Models;
using API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace API.Controllers;

[ApiController]
[Route("api/enterprise/devices")]
[Authorize(Roles = "Admin,BaoVe")]
public class EnterpriseDeviceController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly RuntimeOrchestrator _runtimeOrchestrator;
    private readonly IDeviceHealthIntelligenceService _deviceHealthAi;

    public EnterpriseDeviceController(
        ApplicationDbContext context,
        RuntimeOrchestrator runtimeOrchestrator,
        IDeviceHealthIntelligenceService deviceHealthAi)
    {
        _context = context;
        _runtimeOrchestrator = runtimeOrchestrator;
        _deviceHealthAi = deviceHealthAi;
    }

    /// <summary>
    /// GET /api/enterprise/devices/health-insights - AI danh gia suc khoe thiet bi
    /// </summary>
    [HttpGet("health-insights")]
    public async Task<IActionResult> GetHealthInsights()
    {
        var insights = await _deviceHealthAi.GetAllInsightsAsync();
        return Ok(insights);
    }

    /// <summary>
    /// POST /api/enterprise/devices/{deviceId}/ai-diagnose - AI chan doan thiet bi
    /// </summary>
    [HttpPost("{deviceId:int}/ai-diagnose")]
    public async Task<IActionResult> DiagnoseDevice(int deviceId)
    {
        try
        {
            var result = await _deviceHealthAi.DiagnoseDeviceAsync(deviceId, GetCurrentUserId());
            return Ok(result);
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { message = "Device not found." });
        }
    }

    [HttpGet("overview")]
    public async Task<IActionResult> GetOverview()
    {
        return Ok(new
        {
            Devices = await _context.SecurityDevices.CountAsync(),
            Controllers = await _context.AccessControllerDevices.CountAsync(),
            Readers = await _context.ReaderDevices.CountAsync(),
            Relays = await _context.DeviceRelays.CountAsync(),
            Sensors = await _context.DeviceSensors.CountAsync(),
            HealthSnapshots = await _context.DeviceHealthSnapshots.CountAsync(),
            ProvisioningRequests = await _context.DeviceProvisioningRequests.CountAsync(),
            OfflinePackages = await _context.OfflinePolicyPackages.CountAsync()
        });
    }

    [HttpGet]
    public async Task<IActionResult> GetDevices()
    {
        var devices = await _context.SecurityDevices
            .OrderBy(device => device.Name)
            .ToListAsync();
        return Ok(devices);
    }

    [HttpPost]
    [Authorize(Roles = "Admin,BaoVe")]
    public async Task<IActionResult> CreateDevice([FromBody] DeviceRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            return BadRequest(new { message = "Name is required." });

        var device = new SecurityDevice
        {
            SiteId = request.SiteId,
            AccessPointId = request.AccessPointId,
            DeviceType = string.IsNullOrWhiteSpace(request.DeviceType) ? "Controller" : request.DeviceType.Trim(),
            Name = request.Name.Trim(),
            Vendor = request.Vendor?.Trim(),
            Model = request.Model?.Trim(),
            SerialNumber = request.SerialNumber?.Trim(),
            FirmwareVersion = request.FirmwareVersion?.Trim(),
            ConfigurationVersion = request.ConfigurationVersion?.Trim(),
            Status = "Active",
            LastSeenAtUtc = DateTime.UtcNow
        };

        _context.SecurityDevices.Add(device);
        await _context.SaveChangesAsync();
        return Ok(device);
    }

    [HttpPost("{deviceId:int}/controllers")]
    [Authorize(Roles = "Admin,BaoVe")]
    public async Task<IActionResult> RegisterController(int deviceId, [FromBody] ControllerDeviceRequest request)
    {
        if (!await _context.SecurityDevices.AnyAsync(device => device.SecurityDeviceId == deviceId))
            return NotFound(new { message = "Device not found." });

        var controller = new AccessControllerDevice
        {
            SecurityDeviceId = deviceId,
            Protocol = string.IsNullOrWhiteSpace(request.Protocol) ? "OSDP" : request.Protocol.Trim(),
            SupportsOfflineDecision = request.SupportsOfflineDecision,
            MaxCredentials = request.MaxCredentials
        };

        _context.AccessControllerDevices.Add(controller);
        await _context.SaveChangesAsync();
        return Ok(controller);
    }

    [HttpPost("{deviceId:int}/health")]
    public async Task<IActionResult> RecordHealth(int deviceId, [FromBody] DeviceHealthRequest request)
    {
        var device = await _context.SecurityDevices.FindAsync(deviceId);
        if (device == null)
            return NotFound(new { message = "Device not found." });

        device.Status = string.IsNullOrWhiteSpace(request.Status) ? "Unknown" : request.Status.Trim();
        device.LastSeenAtUtc = DateTime.UtcNow;
        var snapshot = new DeviceHealthSnapshot
        {
            SecurityDeviceId = deviceId,
            Status = device.Status,
            Message = request.Message?.Trim(),
            LatencyMs = request.LatencyMs
        };

        _context.DeviceHealthSnapshots.Add(snapshot);
        await _context.SaveChangesAsync();
        return Ok(snapshot);
    }

    [HttpPost("{deviceId:int}/configuration-versions")]
    [Authorize(Roles = "Admin,BaoVe")]
    [RequireStepUp(PrivilegedActions.DeviceConfiguration)]
    public async Task<IActionResult> AddConfigurationVersion(int deviceId, [FromBody] DeviceConfigurationRequest request)
    {
        if (!await _context.SecurityDevices.AnyAsync(device => device.SecurityDeviceId == deviceId))
            return NotFound(new { message = "Device not found." });

        var version = new DeviceConfigurationVersion
        {
            SecurityDeviceId = deviceId,
            Version = string.IsNullOrWhiteSpace(request.Version) ? DateTime.UtcNow.ToString("yyyyMMddHHmmss") : request.Version.Trim(),
            ConfigurationJson = string.IsNullOrWhiteSpace(request.ConfigurationJson) ? "{}" : request.ConfigurationJson.Trim(),
            CreatedByUserId = GetCurrentUserId()
        };

        _context.DeviceConfigurationVersions.Add(version);
        await _context.SaveChangesAsync();
        return Ok(version);
    }

    [HttpPost("provisioning-requests")]
    [Authorize(Roles = "Admin,BaoVe")]
    public async Task<IActionResult> CreateProvisioningRequest([FromBody] ProvisioningRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.RequestedName))
            return BadRequest(new { message = "RequestedName is required." });

        var provisioning = new DeviceProvisioningRequest
        {
            SecurityDeviceId = request.SecurityDeviceId,
            DeviceType = string.IsNullOrWhiteSpace(request.DeviceType) ? "Controller" : request.DeviceType.Trim(),
            RequestedName = request.RequestedName.Trim(),
            Status = "Pending"
        };

        _context.DeviceProvisioningRequests.Add(provisioning);
        await _context.SaveChangesAsync();
        return Ok(provisioning);
    }

    [HttpPatch("provisioning-requests/{requestId:int}/approve")]
    [Authorize(Roles = "Admin,BaoVe")]
    [RequireStepUp(PrivilegedActions.DeviceConfiguration)]
    public async Task<IActionResult> ApproveProvisioningRequest(int requestId, [FromBody] ProvisioningApprovalRequest request)
    {
        var provisioning = await _context.DeviceProvisioningRequests.FindAsync(requestId);
        if (provisioning == null)
            return NotFound(new { message = "Provisioning request not found." });

        provisioning.Status = "Approved";
        provisioning.ApprovalNote = request.ApprovalNote?.Trim();
        provisioning.ApprovedAtUtc = DateTime.UtcNow;
        provisioning.ApprovedByUserId = GetCurrentUserId();
        await _context.SaveChangesAsync();
        return Ok(provisioning);
    }

    [HttpPost("offline-policy-packages")]
    [Authorize(Roles = "Admin,BaoVe")]
    [RequireStepUp(PrivilegedActions.DeviceConfiguration)]
    public async Task<IActionResult> CreateOfflinePolicyPackage([FromBody] OfflinePolicyPackageRequest request)
    {
        var package = new OfflinePolicyPackage
        {
            SecurityDeviceId = request.SecurityDeviceId,
            PackageVersion = string.IsNullOrWhiteSpace(request.PackageVersion) ? DateTime.UtcNow.ToString("yyyyMMddHHmmss") : request.PackageVersion.Trim(),
            PayloadJson = string.IsNullOrWhiteSpace(request.PayloadJson) ? "{}" : request.PayloadJson.Trim(),
            PayloadHash = request.PayloadHash?.Trim(),
            Status = string.IsNullOrWhiteSpace(request.Status) ? "Draft" : request.Status.Trim(),
            PublishedAtUtc = request.Status == "Published" ? DateTime.UtcNow : null
        };

        _context.OfflinePolicyPackages.Add(package);
        await _context.SaveChangesAsync();
        return Ok(package);
    }

    [HttpGet("connectors/status")]
    public IActionResult GetConnectorStatus()
    {
        var runtimeServices = _runtimeOrchestrator.GetServices()
            .Select(service => new
            {
                service.Name,
                service.DisplayName,
                service.Enabled,
                service.AutoStart,
                service.ManagedMode,
                service.Running,
                Status = !service.Enabled ? "disabled" : service.Running ? "ok" : service.AutoStart ? "degraded" : "manual"
            })
            .ToList();

        return Ok(new
        {
            ConnectorBoundary = "API wrapper only; no AI_Runtime or runtime files are modified.",
            RuntimeServices = runtimeServices,
            Standards = new[] { "OSDP-compatible abstraction", "ONVIF Profile A/C-compatible abstraction" }
        });
    }

    [HttpPost("simulator/virtual-controller")]
    [Authorize(Roles = "Admin,BaoVe")]
    public async Task<IActionResult> CreateVirtualController([FromBody] VirtualControllerRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            return BadRequest(new { message = "Name is required." });

        var device = new SecurityDevice
        {
            SiteId = request.SiteId,
            AccessPointId = request.AccessPointId,
            DeviceType = "VirtualController",
            Name = request.Name.Trim(),
            Vendor = "V-Shield Simulator",
            Model = "EnterpriseEdge-Sim",
            SerialNumber = $"SIM-{Guid.NewGuid():N}"[..16],
            FirmwareVersion = "sim-1.0",
            ConfigurationVersion = "initial",
            Status = "Ok",
            LastSeenAtUtc = DateTime.UtcNow
        };

        _context.SecurityDevices.Add(device);
        await _context.SaveChangesAsync();

        _context.AccessControllerDevices.Add(new AccessControllerDevice
        {
            SecurityDeviceId = device.SecurityDeviceId,
            Protocol = string.IsNullOrWhiteSpace(request.Protocol) ? "OSDP-Sim" : request.Protocol.Trim(),
            SupportsOfflineDecision = true,
            MaxCredentials = request.MaxCredentials <= 0 ? 50000 : request.MaxCredentials
        });
        _context.ReaderDevices.Add(new ReaderDevice
        {
            SecurityDeviceId = device.SecurityDeviceId,
            ReaderProtocol = "OSDP-Sim",
            CredentialFormats = string.IsNullOrWhiteSpace(request.Direction)
                ? "QR,Card,Pin"
                : $"QR,Card,Pin,{request.Direction.Trim()}"
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
            Message = "Virtual controller created."
        });

        await _context.SaveChangesAsync();
        return Ok(new { device.SecurityDeviceId, device.Name, device.Status });
    }

    [HttpPost("simulator/offline-scan")]
    public async Task<IActionResult> SimulateOfflineScan([FromBody] OfflineScanRequest request)
    {
        var device = await _context.SecurityDevices.FindAsync(request.SecurityDeviceId);
        if (device == null)
            return NotFound(new { message = "Device not found." });

        var package = await _context.OfflinePolicyPackages
            .Where(item => item.SecurityDeviceId == request.SecurityDeviceId && item.Status == "Published")
            .OrderByDescending(item => item.PublishedAtUtc)
            .FirstOrDefaultAsync();

        var now = request.EvaluatedAtUtc ?? DateTime.UtcNow;
        var allowed = PackageAllows(package?.PayloadJson, request.SubjectType, request.SubjectId, request.CredentialType);
        var decision = new AccessDecision
        {
            SubjectType = string.IsNullOrWhiteSpace(request.SubjectType) ? "Employee" : request.SubjectType.Trim(),
            SubjectId = request.SubjectId,
            SiteId = request.SiteId ?? device.SiteId,
            SecurityZoneId = request.SecurityZoneId,
            AccessPointId = request.AccessPointId ?? device.AccessPointId,
            CredentialType = string.IsNullOrWhiteSpace(request.CredentialType) ? "Any" : request.CredentialType.Trim(),
            Result = allowed ? AccessDecisionResults.Allow : AccessDecisionResults.Deny,
            Reason = allowed
                ? $"Offline simulator package {package?.PackageVersion} allowed credential."
                : "Offline simulator denied because no published package rule matched.",
            EvaluatedAtUtc = now,
            EvaluatedByUserId = GetCurrentUserId()
        };

        _context.AccessDecisions.Add(decision);
        _context.SecurityEvents.Add(new SecurityEvent
        {
            SourceType = "DeviceSimulator",
            SourceId = request.SecurityDeviceId.ToString(),
            EventType = allowed ? "OfflineAccessGranted" : "OfflineAccessDenied",
            Severity = allowed ? "Info" : "Medium",
            SiteId = decision.SiteId,
            SecurityZoneId = decision.SecurityZoneId,
            AccessPointId = decision.AccessPointId,
            SubjectType = decision.SubjectType,
            SubjectId = decision.SubjectId,
            Summary = decision.Reason,
            OccurredAtUtc = now
        });

        device.Status = "Ok";
        device.LastSeenAtUtc = now;
        await _context.SaveChangesAsync();
        return Ok(decision);
    }

    [HttpPost("simulator/fault")]
    public async Task<IActionResult> InjectSimulatorFault([FromBody] SimulatorFaultRequest request)
    {
        var device = await _context.SecurityDevices.FindAsync(request.SecurityDeviceId);
        if (device == null)
            return NotFound(new { message = "Device not found." });

        var status = string.IsNullOrWhiteSpace(request.Status) ? "Tamper" : request.Status.Trim();
        device.Status = status;
        device.LastSeenAtUtc = DateTime.UtcNow;

        _context.DeviceHealthSnapshots.Add(new DeviceHealthSnapshot
        {
            SecurityDeviceId = request.SecurityDeviceId,
            Status = status,
            Message = request.Message?.Trim() ?? "Simulator fault injected."
        });
        _context.Alarms.Add(new Alarm
        {
            AlarmType = status,
            Severity = request.Severity?.Trim() ?? "High",
            State = "New",
            Summary = $"Simulator fault for device {device.Name}: {status}.",
            SiteId = device.SiteId
        });

        await _context.SaveChangesAsync();
        return Ok(new { device.SecurityDeviceId, device.Status });
    }

    private static bool PackageAllows(string? payloadJson, string? subjectType, int? subjectId, string? credentialType)
    {
        if (string.IsNullOrWhiteSpace(payloadJson))
            return false;

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

                if (typeMatches && idMatches && credentialMatches)
                    return true;
            }
        }
        catch (JsonException)
        {
            return payloadJson.Contains("\"allowAll\":true", StringComparison.OrdinalIgnoreCase);
        }

        return false;
    }

    private int? GetCurrentUserId()
    {
        var userIdClaim = User.FindFirstValue(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub);
        return int.TryParse(userIdClaim, out var userId) ? userId : null;
    }

    public sealed record DeviceRequest(int? SiteId, int? AccessPointId, string? DeviceType, string Name, string? Vendor, string? Model, string? SerialNumber, string? FirmwareVersion, string? ConfigurationVersion);
    public sealed record ControllerDeviceRequest(string? Protocol, bool SupportsOfflineDecision, int MaxCredentials);
    public sealed record DeviceHealthRequest(string? Status, string? Message, int? LatencyMs);
    public sealed record DeviceConfigurationRequest(string? Version, string? ConfigurationJson);
    public sealed record ProvisioningRequest(int? SecurityDeviceId, string? DeviceType, string RequestedName);
    public sealed record ProvisioningApprovalRequest(string? ApprovalNote);
    public sealed record OfflinePolicyPackageRequest(int? SecurityDeviceId, string? PackageVersion, string? PayloadJson, string? PayloadHash, string? Status);
    public sealed record VirtualControllerRequest(string Name, int? SiteId, int? AccessPointId, string? Protocol, string? Direction, int MaxCredentials);
    public sealed record OfflineScanRequest(int SecurityDeviceId, string? SubjectType, int? SubjectId, int? SiteId, int? SecurityZoneId, int? AccessPointId, string? CredentialType, DateTime? EvaluatedAtUtc);
    public sealed record SimulatorFaultRequest(int SecurityDeviceId, string? Status, string? Severity, string? Message);
}
