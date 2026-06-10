using System.Security.Claims;
using API.Data;
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

    public EnterpriseDeviceController(ApplicationDbContext context, RuntimeOrchestrator runtimeOrchestrator)
    {
        _context = context;
        _runtimeOrchestrator = runtimeOrchestrator;
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
    [Authorize(Roles = "Admin")]
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
    [Authorize(Roles = "Admin")]
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
    [Authorize(Roles = "Admin")]
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
    [Authorize(Roles = "Admin")]
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
    [Authorize(Roles = "Admin")]
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
    [Authorize(Roles = "Admin")]
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
}

