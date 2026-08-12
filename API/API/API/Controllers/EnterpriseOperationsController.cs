using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using API.Data;
using API.Middleware;
using API.Models;
using API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace API.Controllers;

[ApiController]
[Route("api/enterprise/operations")]
[Authorize]
[RequireOperationalTask("monitoring")]
public class EnterpriseOperationsController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly RuntimeOrchestrator _runtimeOrchestrator;
    private readonly ISecurityConfigurationHealthService _configurationHealth;

    public EnterpriseOperationsController(
        ApplicationDbContext context,
        RuntimeOrchestrator runtimeOrchestrator,
        ISecurityConfigurationHealthService configurationHealth)
    {
        _context = context;
        _runtimeOrchestrator = runtimeOrchestrator;
        _configurationHealth = configurationHealth;
    }

    [HttpGet("overview")]
    public async Task<IActionResult> GetOverview()
    {
        return Ok(new
        {
            PendingOutboxEvents = await _context.OutboxEvents.CountAsync(item => item.Status == "Pending"),
            FailedOutboxEvents = await _context.OutboxEvents.CountAsync(item => item.Status == "Failed" || item.Status == "DeadLetter"),
            ActiveWebhookSubscriptions = await _context.WebhookSubscriptions.CountAsync(item => item.IsActive),
            PendingWebhookDeliveries = await _context.WebhookDeliveries.CountAsync(item => item.Status == "Pending"),
            DegradedDependencies = await _context.RuntimeDependencyHealths.CountAsync(item => item.Status != "Ok"),
            BackupRuns = await _context.BackupRuns.CountAsync(),
            RestoreDrills = await _context.RestoreDrills.CountAsync(),
            FailedOperationsChecks = await _context.SecurityOperationsChecks.CountAsync(item => item.Status == "Failed")
        });
    }

    [HttpGet("metrics/summary")]
    public async Task<IActionResult> GetMetricsSummary()
    {
        var latestBackup = await _context.BackupRuns
            .OrderByDescending(item => item.StartedAtUtc)
            .FirstOrDefaultAsync();
        var latestRestore = await _context.RestoreDrills
            .OrderByDescending(item => item.StartedAtUtc)
            .FirstOrDefaultAsync();

        return Ok(new
        {
            ApiLatencyBudgetMs = 500,
            AccessDecisionLatencyBudgetMs = 200,
            RuntimeDependencyHealthRecords = await _context.RuntimeDependencyHealths.CountAsync(),
            AlarmQueueAgeMinutes = await GetOldestAlarmAgeMinutesAsync(),
            DeviceOfflineCount = await _context.SecurityDevices.CountAsync(device => device.Status != "Active" && device.Status != "Ok"),
            FailedEvidenceExports = await _context.EvidenceExportRequests.CountAsync(item => item.Status == "Failed"),
            PendingOutboxEvents = await _context.OutboxEvents.CountAsync(item => item.Status == "Pending"),
            LatestBackupStatus = latestBackup?.Status ?? "None",
            LatestRestoreDrillStatus = latestRestore?.Status ?? "None",
            LatestMeasuredRpoMinutes = latestRestore?.MeasuredRpoMinutes,
            LatestMeasuredRtoMinutes = latestRestore?.MeasuredRtoMinutes
        });
    }

    [HttpGet("runtime-dependencies/status")]
    public async Task<IActionResult> GetRuntimeDependencyStatus()
    {
        var runtimeServices = _runtimeOrchestrator.GetServices()
            .Select(service => new
            {
                service.Name,
                service.DisplayName,
                service.Enabled,
                service.ManagedMode,
                service.Running,
                Status = !service.Enabled ? "disabled" : service.Running ? "ok" : service.AutoStart ? "degraded" : "manual"
            })
            .ToList();

        var latestHealth = await _context.RuntimeDependencyHealths
            .GroupBy(item => item.DependencyName)
            .Select(group => group.OrderByDescending(item => item.ObservedAtUtc).First())
            .ToListAsync();

        return Ok(new
        {
            Boundary = "Runtime is observed through API wrappers; no AI_Runtime or runtime files are modified.",
            RuntimeServices = runtimeServices,
            RecordedHealth = latestHealth
        });
    }

    [HttpGet("config-health")]
    [Authorize(Roles = "Admin")]
    public IActionResult GetConfigurationHealth()
    {
        var report = _configurationHealth.Evaluate();
        return Ok(new
        {
            report.EnvironmentName,
            report.IsProduction,
            report.Status,
            Findings = report.Findings.Select(finding => new
            {
                finding.Key,
                finding.Severity,
                finding.Status,
                finding.Message,
                finding.Remediation
            })
        });
    }

    [HttpPost("dependency-health")]
    public async Task<IActionResult> RecordDependencyHealth([FromBody] RuntimeDependencyHealthRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.DependencyName))
            return BadRequest(new { message = "Vui lòng nhập tên phụ thuộc." });

        var health = new RuntimeDependencyHealth
        {
            DependencyName = request.DependencyName.Trim(),
            DependencyType = string.IsNullOrWhiteSpace(request.DependencyType) ? "Runtime" : request.DependencyType.Trim(),
            Status = string.IsNullOrWhiteSpace(request.Status) ? "Unknown" : request.Status.Trim(),
            LatencyMs = request.LatencyMs,
            Message = request.Message?.Trim()
        };

        _context.RuntimeDependencyHealths.Add(health);
        await _context.SaveChangesAsync();
        return Ok(health);
    }

    [HttpPost("outbox-events")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> CreateOutboxEvent([FromBody] OutboxEventRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.EventType) || string.IsNullOrWhiteSpace(request.AggregateType))
            return BadRequest(new { message = "Vui lòng nhập EventType và AggregateType." });

        var item = new OutboxEvent
        {
            Channel = "Operations",
            EventType = request.EventType.Trim(),
            AggregateType = request.AggregateType.Trim(),
            AggregateId = request.AggregateId?.Trim(),
            PayloadJson = string.IsNullOrWhiteSpace(request.PayloadJson) ? "{}" : request.PayloadJson.Trim(),
            Status = "Pending",
            NextAttemptAtUtc = DateTime.UtcNow,
            CorrelationId = string.IsNullOrWhiteSpace(request.CorrelationId) ? Guid.NewGuid().ToString("N") : request.CorrelationId.Trim()
        };

        _context.OutboxEvents.Add(item);
        await _context.SaveChangesAsync();
        return Ok(item);
    }

    [HttpPatch("outbox-events/{outboxEventId:long}/dispatch")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> MarkOutboxDispatched(long outboxEventId, [FromBody] OutboxDispatchRequest request)
    {
        var item = await _context.OutboxEvents.FindAsync(outboxEventId);
        if (item == null)
            return NotFound(new { message = "Không tìm thấy sự kiện outbox." });

        item.Status = string.IsNullOrWhiteSpace(request.Status) ? "Dispatched" : request.Status.Trim();
        item.RetryCount = request.RetryCount;
        item.DispatchedAtUtc = item.Status == "Dispatched" ? DateTime.UtcNow : null;
        item.NextAttemptAtUtc = item.Status == "Dispatched" ? null : DateTime.UtcNow.AddMinutes(5);
        await _context.SaveChangesAsync();
        return Ok(item);
    }

    [HttpPost("webhook-subscriptions")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> CreateWebhookSubscription([FromBody] WebhookSubscriptionRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name) || string.IsNullOrWhiteSpace(request.TargetUrl))
            return BadRequest(new { message = "Vui lòng nhập tên và TargetUrl." });

        var subscription = new WebhookSubscription
        {
            Name = request.Name.Trim(),
            TargetUrl = request.TargetUrl.Trim(),
            SecretReference = string.IsNullOrWhiteSpace(request.SecretReference) ? "secret://vshield/webhook/default" : request.SecretReference.Trim(),
            EventTypes = string.IsNullOrWhiteSpace(request.EventTypes) ? "*" : request.EventTypes.Trim(),
            IsActive = request.IsActive
        };

        _context.WebhookSubscriptions.Add(subscription);
        await _context.SaveChangesAsync();
        return Ok(subscription);
    }

    [HttpPost("webhook-deliveries")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> CreateWebhookDelivery([FromBody] WebhookDeliveryRequest request)
    {
        var subscription = await _context.WebhookSubscriptions.FindAsync(request.WebhookSubscriptionId);
        if (subscription == null || !subscription.IsActive)
            return BadRequest(new { message = "Không tìm thấy đăng ký webhook đang hoạt động." });

        var outbox = request.OutboxEventId.HasValue
            ? await _context.OutboxEvents.FindAsync(request.OutboxEventId.Value)
            : null;

        var delivery = new WebhookDelivery
        {
            WebhookSubscriptionId = request.WebhookSubscriptionId,
            OutboxEventId = request.OutboxEventId,
            Status = "Pending",
            Signature = ComputeSignature(subscription.SecretReference, $"{request.WebhookSubscriptionId}|{request.OutboxEventId}|{outbox?.CorrelationId}")
        };

        _context.WebhookDeliveries.Add(delivery);
        await _context.SaveChangesAsync();
        return Ok(delivery);
    }

    [HttpPatch("webhook-deliveries/{webhookDeliveryId:long}/result")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> RecordWebhookDeliveryResult(long webhookDeliveryId, [FromBody] WebhookDeliveryResultRequest request)
    {
        var delivery = await _context.WebhookDeliveries.FindAsync(webhookDeliveryId);
        if (delivery == null)
            return NotFound(new { message = "Không tìm thấy lượt gửi webhook." });

        delivery.Status = string.IsNullOrWhiteSpace(request.Status) ? "Delivered" : request.Status.Trim();
        delivery.AttemptCount = request.AttemptCount <= 0 ? delivery.AttemptCount + 1 : request.AttemptCount;
        delivery.LastAttemptAtUtc = DateTime.UtcNow;
        delivery.ResponseStatusCode = request.ResponseStatusCode;
        delivery.ResponseBody = request.ResponseBody?.Trim();
        await _context.SaveChangesAsync();
        return Ok(delivery);
    }

    [HttpPost("siem-exports")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> CreateSiemExport([FromBody] SiemExportRequest request)
    {
        var outbox = new OutboxEvent
        {
            Channel = "Operations",
            EventType = "SiemExport",
            AggregateType = string.IsNullOrWhiteSpace(request.Source) ? "SecurityEvent" : request.Source.Trim(),
            AggregateId = request.CorrelationId?.Trim(),
            PayloadJson = string.IsNullOrWhiteSpace(request.PayloadJson) ? "{}" : request.PayloadJson.Trim(),
            Status = "Pending",
            NextAttemptAtUtc = DateTime.UtcNow,
            CorrelationId = string.IsNullOrWhiteSpace(request.CorrelationId) ? Guid.NewGuid().ToString("N") : request.CorrelationId.Trim()
        };

        _context.OutboxEvents.Add(outbox);
        await _context.SaveChangesAsync();
        return Ok(outbox);
    }

    [HttpPost("backup-runs")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> StartBackupRun([FromBody] BackupRunRequest request)
    {
        var backup = new BackupRun
        {
            Profile = string.IsNullOrWhiteSpace(request.Profile) ? "Production" : request.Profile.Trim(),
            Status = "Running",
            TargetRpoMinutes = request.TargetRpoMinutes <= 0 ? 15 : request.TargetRpoMinutes,
            TargetRtoMinutes = request.TargetRtoMinutes <= 0 ? 60 : request.TargetRtoMinutes,
            Notes = request.Notes?.Trim()
        };

        _context.BackupRuns.Add(backup);
        await _context.SaveChangesAsync();
        return Ok(backup);
    }

    [HttpPatch("backup-runs/{backupRunId:long}/complete")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> CompleteBackupRun(long backupRunId, [FromBody] BackupCompletionRequest request)
    {
        var backup = await _context.BackupRuns.FindAsync(backupRunId);
        if (backup == null)
            return NotFound(new { message = "Không tìm thấy lần sao lưu." });

        backup.Status = string.IsNullOrWhiteSpace(request.Status) ? "Completed" : request.Status.Trim();
        backup.CompletedAtUtc = DateTime.UtcNow;
        backup.BackupReference = request.BackupReference?.Trim();
        backup.SizeBytes = request.SizeBytes;
        backup.Verified = request.Verified;
        backup.Notes = request.Notes?.Trim();
        await _context.SaveChangesAsync();
        return Ok(backup);
    }

    [HttpPost("restore-drills")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> StartRestoreDrill([FromBody] RestoreDrillRequest request)
    {
        var drill = new RestoreDrill
        {
            BackupRunId = request.BackupRunId,
            Profile = string.IsNullOrWhiteSpace(request.Profile) ? "Production" : request.Profile.Trim(),
            Status = "Running"
        };

        _context.RestoreDrills.Add(drill);
        await _context.SaveChangesAsync();
        return Ok(drill);
    }

    [HttpPatch("restore-drills/{restoreDrillId:long}/complete")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> CompleteRestoreDrill(long restoreDrillId, [FromBody] RestoreDrillCompletionRequest request)
    {
        var drill = await _context.RestoreDrills.FindAsync(restoreDrillId);
        if (drill == null)
            return NotFound(new { message = "Không tìm thấy cuộc diễn tập khôi phục." });

        drill.Status = request.Passed ? "Passed" : "Failed";
        drill.CompletedAtUtc = DateTime.UtcNow;
        drill.MeasuredRpoMinutes = request.MeasuredRpoMinutes;
        drill.MeasuredRtoMinutes = request.MeasuredRtoMinutes;
        drill.Passed = request.Passed;
        drill.Findings = request.Findings?.Trim();
        await _context.SaveChangesAsync();
        return Ok(drill);
    }

    [HttpPost("security-checks")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> RecordSecurityOperationsCheck([FromBody] SecurityOperationsCheckRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.CheckType) || string.IsNullOrWhiteSpace(request.Name))
            return BadRequest(new { message = "Vui lòng nhập CheckType và tên." });

        var check = new SecurityOperationsCheck
        {
            CheckType = request.CheckType.Trim(),
            Name = request.Name.Trim(),
            Status = string.IsNullOrWhiteSpace(request.Status) ? "Passed" : request.Status.Trim(),
            Evidence = request.Evidence?.Trim()
        };

        _context.SecurityOperationsChecks.Add(check);
        await _context.SaveChangesAsync();
        return Ok(check);
    }

    private async Task<double> GetOldestAlarmAgeMinutesAsync()
    {
        var oldest = await _context.Alarms
            .Where(alarm => alarm.State != "Closed")
            .OrderBy(alarm => alarm.CreatedAtUtc)
            .Select(alarm => (DateTime?)alarm.CreatedAtUtc)
            .FirstOrDefaultAsync();

        return oldest.HasValue ? Math.Round((DateTime.UtcNow - oldest.Value).TotalMinutes, 2) : 0;
    }

    private int? GetCurrentUserId()
    {
        var userIdClaim = User.FindFirstValue(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub);
        return int.TryParse(userIdClaim, out var userId) ? userId : null;
    }

    private static string ComputeSignature(string secretReference, string payload)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secretReference));
        var bytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    public sealed record RuntimeDependencyHealthRequest(string DependencyName, string? DependencyType, string? Status, int? LatencyMs, string? Message);
    public sealed record OutboxEventRequest(string EventType, string AggregateType, string? AggregateId, string? PayloadJson, string? CorrelationId);
    public sealed record OutboxDispatchRequest(string? Status, int RetryCount);
    public sealed record WebhookSubscriptionRequest(string Name, string TargetUrl, string? SecretReference, string? EventTypes, bool IsActive);
    public sealed record WebhookDeliveryRequest(int WebhookSubscriptionId, long? OutboxEventId);
    public sealed record WebhookDeliveryResultRequest(string? Status, int AttemptCount, int? ResponseStatusCode, string? ResponseBody);
    public sealed record SiemExportRequest(string? Source, string? CorrelationId, string? PayloadJson);
    public sealed record BackupRunRequest(string? Profile, int TargetRpoMinutes, int TargetRtoMinutes, string? Notes);
    public sealed record BackupCompletionRequest(string? Status, string? BackupReference, long? SizeBytes, bool Verified, string? Notes);
    public sealed record RestoreDrillRequest(long? BackupRunId, string? Profile);
    public sealed record RestoreDrillCompletionRequest(int? MeasuredRpoMinutes, int? MeasuredRtoMinutes, bool Passed, string? Findings);
    public sealed record SecurityOperationsCheckRequest(string CheckType, string Name, string? Status, string? Evidence);
}
