using System.ComponentModel.DataAnnotations;

namespace API.Models;

public class OutboxEvent
{
    public long OutboxEventId { get; set; }
    [MaxLength(120)] public string EventType { get; set; } = string.Empty;
    [MaxLength(120)] public string AggregateType { get; set; } = string.Empty;
    [MaxLength(120)] public string? AggregateId { get; set; }
    [MaxLength(4000)] public string PayloadJson { get; set; } = "{}";
    [MaxLength(40)] public string Status { get; set; } = "Pending";
    public int RetryCount { get; set; }
    public DateTime? NextAttemptAtUtc { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? DispatchedAtUtc { get; set; }
    [MaxLength(80)] public string CorrelationId { get; set; } = Guid.NewGuid().ToString("N");
}

public class WebhookSubscription
{
    public int WebhookSubscriptionId { get; set; }
    [MaxLength(160)] public string Name { get; set; } = string.Empty;
    [MaxLength(500)] public string TargetUrl { get; set; } = string.Empty;
    [MaxLength(240)] public string SecretReference { get; set; } = string.Empty;
    [MaxLength(1000)] public string EventTypes { get; set; } = "*";
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}

public class WebhookDelivery
{
    public long WebhookDeliveryId { get; set; }
    public int WebhookSubscriptionId { get; set; }
    public long? OutboxEventId { get; set; }
    [MaxLength(40)] public string Status { get; set; } = "Pending";
    public int AttemptCount { get; set; }
    public DateTime? LastAttemptAtUtc { get; set; }
    public int? ResponseStatusCode { get; set; }
    [MaxLength(1000)] public string? ResponseBody { get; set; }
    [MaxLength(240)] public string Signature { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public WebhookSubscription? Subscription { get; set; }
    public OutboxEvent? OutboxEvent { get; set; }
}

public class RuntimeDependencyHealth
{
    public long RuntimeDependencyHealthId { get; set; }
    [MaxLength(120)] public string DependencyName { get; set; } = string.Empty;
    [MaxLength(80)] public string DependencyType { get; set; } = "Runtime";
    [MaxLength(40)] public string Status { get; set; } = "Unknown";
    public int? LatencyMs { get; set; }
    [MaxLength(1000)] public string? Message { get; set; }
    public DateTime ObservedAtUtc { get; set; } = DateTime.UtcNow;
}

public class BackupRun
{
    public long BackupRunId { get; set; }
    [MaxLength(120)] public string Profile { get; set; } = "Production";
    [MaxLength(40)] public string Status { get; set; } = "Running";
    public DateTime StartedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAtUtc { get; set; }
    [MaxLength(500)] public string? BackupReference { get; set; }
    public long? SizeBytes { get; set; }
    public int TargetRpoMinutes { get; set; }
    public int TargetRtoMinutes { get; set; }
    public bool Verified { get; set; }
    [MaxLength(1000)] public string? Notes { get; set; }
}

public class RestoreDrill
{
    public long RestoreDrillId { get; set; }
    public long? BackupRunId { get; set; }
    [MaxLength(120)] public string Profile { get; set; } = "Production";
    [MaxLength(40)] public string Status { get; set; } = "Running";
    public DateTime StartedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAtUtc { get; set; }
    public int? MeasuredRpoMinutes { get; set; }
    public int? MeasuredRtoMinutes { get; set; }
    public bool Passed { get; set; }
    [MaxLength(2000)] public string? Findings { get; set; }
    public BackupRun? BackupRun { get; set; }
}

public class SecurityOperationsCheck
{
    public long SecurityOperationsCheckId { get; set; }
    [MaxLength(120)] public string CheckType { get; set; } = string.Empty;
    [MaxLength(160)] public string Name { get; set; } = string.Empty;
    [MaxLength(40)] public string Status { get; set; } = "Pending";
    [MaxLength(1000)] public string? Evidence { get; set; }
    public DateTime CheckedAtUtc { get; set; } = DateTime.UtcNow;
}
