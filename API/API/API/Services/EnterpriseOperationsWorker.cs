using System.Security.Cryptography;
using System.Text;
using API.Data;
using API.Models;
using Microsoft.EntityFrameworkCore;

namespace API.Services;

public class EnterpriseOperationsWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<EnterpriseOperationsWorker> _logger;
    private readonly TimeSpan _interval;

    public EnterpriseOperationsWorker(
        IServiceScopeFactory scopeFactory,
        ILogger<EnterpriseOperationsWorker> logger,
        IConfiguration configuration)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _interval = TimeSpan.FromSeconds(Math.Max(5, configuration.GetValue("EnterpriseOperations:WorkerIntervalSeconds", 30)));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(_interval);
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await RunOnceAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Enterprise operations worker cycle failed.");
            }

            try
            {
                await timer.WaitForNextTickAsync(stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }

    public async Task RunOnceAsync(CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var socIntel = scope.ServiceProvider.GetRequiredService<ISocIntelligenceService>();
        var now = DateTime.UtcNow;

        await ProcessOutboxAsync(db, now, cancellationToken);
        await EscalateAlarmSlaAsync(db, socIntel, now, cancellationToken);
        await DetectVisitorOverstayAsync(db, now, cancellationToken);
        await MarkStaleDevicesAsync(db, now, cancellationToken);
        await AutoOffboardStaleAccountsAsync(db, _logger, now, cancellationToken);
        await ExpireStaleInterventionRequestsAsync(db, now, cancellationToken);

        await db.SaveChangesAsync(cancellationToken);
    }

    private static async Task ProcessOutboxAsync(ApplicationDbContext db, DateTime now, CancellationToken cancellationToken)
    {
        var events = await db.OutboxEvents
            .Where(item =>
                item.Channel == "Operations" &&
                item.Status == "Pending" &&
                (item.NextAttemptAtUtc == null || item.NextAttemptAtUtc <= now))
            .OrderBy(item => item.CreatedAtUtc)
            .Take(25)
            .ToListAsync(cancellationToken);

        if (events.Count == 0)
            return;

        var subscriptions = await db.WebhookSubscriptions
            .Where(item => item.IsActive)
            .ToListAsync(cancellationToken);

        foreach (var item in events)
        {
            var matchingSubscriptions = subscriptions
                .Where(subscription => MatchesSubscription(subscription, item.EventType))
                .ToList();

            foreach (var subscription in matchingSubscriptions)
            {
                var deliveryExists = await db.WebhookDeliveries.AnyAsync(delivery =>
                    delivery.WebhookSubscriptionId == subscription.WebhookSubscriptionId &&
                    delivery.OutboxEventId == item.OutboxEventId,
                    cancellationToken);

                if (deliveryExists)
                    continue;

                db.WebhookDeliveries.Add(new WebhookDelivery
                {
                    WebhookSubscriptionId = subscription.WebhookSubscriptionId,
                    OutboxEventId = item.OutboxEventId,
                    Status = "Pending",
                    Signature = ComputeSignature(subscription.SecretReference, $"{item.CorrelationId}|{item.EventType}|{item.PayloadJson}")
                });
            }

            item.RetryCount++;
            item.Status = matchingSubscriptions.Count == 0 ? "Dispatched" : "DispatchQueued";
            item.DispatchedAtUtc = now;
            item.NextAttemptAtUtc = null;
        }
    }

    private static async Task EscalateAlarmSlaAsync(ApplicationDbContext db, ISocIntelligenceService socIntel, DateTime now, CancellationToken cancellationToken)
    {
        var alarms = await db.Alarms
            .Where(item =>
                item.State != "Closed" &&
                item.State != "Escalated" &&
                item.CreatedAtUtc <= now.AddMinutes(-15) &&
                (item.Severity == "High" || item.Severity == "Critical"))
            .OrderBy(item => item.CreatedAtUtc)
            .ThenBy(item => item.AlarmId)
            .Take(50)
            .ToListAsync(cancellationToken);

        foreach (var alarm in alarms)
        {
            var riskScore = 50;
            try
            {
                var prediction = await socIntel.PredictEscalationRiskAsync(alarm.AlarmId);
                if (prediction != null)
                    riskScore = prediction.RiskScore;
            }
            catch
            {
            }

            if (riskScore >= 60)
            {
                alarm.State = "Escalated";
                db.AlarmComments.Add(new AlarmComment
                {
                    AlarmId = alarm.AlarmId,
                    Comment = $"[Chuyển cấp AI] Điểm rủi ro {riskScore}/100. Tự động chuyển cấp dựa trên phân tích dự đoán."
                });
            }
            else if (alarm.CreatedAtUtc <= now.AddMinutes(-60) && riskScore >= 40)
            {
                alarm.State = "Escalated";
                db.AlarmComments.Add(new AlarmComment
                {
                    AlarmId = alarm.AlarmId,
                    Comment = $"[Chuyển cấp AI] Điểm rủi ro {riskScore}/100. Quá ngưỡng 1 giờ + rủi ro trung bình."
                });
            }
        }
    }

    private static async Task DetectVisitorOverstayAsync(ApplicationDbContext db, DateTime now, CancellationToken cancellationToken)
    {
        var overstays = await db.Visits
            .Where(item =>
                item.Status == "CheckedIn" &&
                item.ExpectedOutUtc < now)
            .OrderBy(item => item.ExpectedOutUtc)
            .ThenBy(item => item.VisitId)
            .Take(50)
            .ToListAsync(cancellationToken);

        foreach (var visit in overstays)
        {
            visit.Status = "Overstay";
            var existingAlarm = await db.Alarms.AnyAsync(alarm =>
                alarm.AlarmType == "VisitorOverstay" &&
                alarm.Summary.Contains($"Visit {visit.VisitId}"),
                cancellationToken);

            if (!existingAlarm)
            {
                db.Alarms.Add(new Alarm
                {
                    AlarmType = "VisitorOverstay",
                    Severity = "Medium",
                    State = "New",
                    Summary = $"Chuyến thăm {visit.VisitId} của khách {visit.VisitorName} đã vượt khung giờ được duyệt.",
                    SiteId = visit.SiteId
                });
            }
        }
    }

    private static async Task MarkStaleDevicesAsync(ApplicationDbContext db, DateTime now, CancellationToken cancellationToken)
    {
        var staleDevices = await db.SecurityDevices
            .Where(item =>
                item.LastSeenAtUtc != null &&
                item.LastSeenAtUtc <= now.AddMinutes(-5) &&
                item.Status != "Offline" &&
                item.Status != "Disabled")
            .OrderBy(item => item.LastSeenAtUtc)
            .ThenBy(item => item.SecurityDeviceId)
            .Take(50)
            .ToListAsync(cancellationToken);

        foreach (var device in staleDevices)
        {
            device.Status = "Offline";
            db.DeviceHealthSnapshots.Add(new DeviceHealthSnapshot
            {
                SecurityDeviceId = device.SecurityDeviceId,
                Status = "Offline",
                Message = "Device heartbeat missed enterprise health threshold.",
                CapturedAtUtc = now
            });

            db.Alarms.Add(new Alarm
            {
                AlarmType = "DeviceOffline",
                Severity = "High",
                State = "New",
                Summary = $"Thiết bị an ninh {device.Name} đã offline.",
                SiteId = device.SiteId
            });
        }
    }

    private static async Task AutoOffboardStaleAccountsAsync(ApplicationDbContext db, ILogger logger, DateTime now, CancellationToken cancellationToken)
    {
        var suspendThreshold = now.AddDays(-30);
        var staleSyncThreshold = now.AddDays(-90);

        var suspendedEmployees = await db.Employees
            .Where(e =>
                e.LifecycleStatus == EmployeeLifecycleStates.Suspended &&
                e.LifecycleUpdatedAtUtc != null &&
                e.LifecycleUpdatedAtUtc <= suspendThreshold)
            .OrderBy(e => e.LifecycleUpdatedAtUtc)
            .ThenBy(e => e.EmployeeId)
            .Take(20)
            .ToListAsync(cancellationToken);

        foreach (var employee in suspendedEmployees)
        {
            employee.LifecycleStatus = EmployeeLifecycleStates.Terminated;
            employee.LifecycleUpdatedAtUtc = now;
            employee.Status = false;

            db.EmployeeLifecycleEvents.Add(new EmployeeLifecycleEvent
            {
                Employee = employee,
                PreviousState = EmployeeLifecycleStates.Suspended,
                NewState = EmployeeLifecycleStates.Terminated,
                Reason = "Auto-offboard: suspended for more than 30 days.",
                EffectiveAtUtc = now,
                ChangedByUserId = null
            });

            var user = await db.AppUsers.FirstOrDefaultAsync(u => u.EmployeeId == employee.EmployeeId, cancellationToken);
            if (user != null)
            {
                user.IsActive = false;
                user.TokenVersion++;
                var tokens = await db.UserRefreshTokens
                    .Where(t => t.UserId == user.UserId && t.RevokedAtUtc == null)
                    .ToListAsync(cancellationToken);
                foreach (var t in tokens)
                {
                    t.RevokedAtUtc = now;
                    t.RevocationReason = "Auto-offboard: suspended >30d";
                }
            }

            var rules = await db.AccessRules
                .Where(r => r.SubjectType == "Employee" && r.SubjectId == employee.EmployeeId && r.IsActive)
                .ToListAsync(cancellationToken);
            foreach (var r in rules)
            {
                r.IsActive = false;
                r.ValidToUtc = now;
            }

            logger.LogInformation("Auto-offboarded employee {EmployeeId} {Name} — suspended >30d", employee.EmployeeId, employee.FullName);
        }

        var staleMappings = await db.ExternalIdentityMappings
            .Include(m => m.Employee)
            .Where(m =>
                m.IsActive &&
                m.LastSyncedAtUtc != null &&
                m.LastSyncedAtUtc <= staleSyncThreshold &&
                m.Employee != null &&
                m.Employee.LifecycleStatus == EmployeeLifecycleStates.Active)
            .OrderBy(m => m.LastSyncedAtUtc)
            .ThenBy(m => m.ExternalIdentityMappingId)
            .Take(20)
            .ToListAsync(cancellationToken);

        foreach (var mapping in staleMappings)
        {
            if (mapping.Employee == null) continue;

            mapping.Employee.LifecycleStatus = EmployeeLifecycleStates.Suspended;
            mapping.Employee.LifecycleUpdatedAtUtc = now;
            mapping.Employee.Status = false;
            mapping.IsActive = false;

            db.EmployeeLifecycleEvents.Add(new EmployeeLifecycleEvent
            {
                Employee = mapping.Employee,
                PreviousState = EmployeeLifecycleStates.Active,
                NewState = EmployeeLifecycleStates.Suspended,
                Reason = "Auto-suspend: identity mapping not synced for 90+ days.",
                EffectiveAtUtc = now,
                ChangedByUserId = null
            });

            var user = await db.AppUsers.FirstOrDefaultAsync(u => u.EmployeeId == mapping.Employee.EmployeeId, cancellationToken);
            if (user != null)
            {
                user.IsActive = false;
                user.TokenVersion++;
            }

            logger.LogInformation("Auto-suspended employee {EmployeeId} {Name} — stale mapping >90d",
                mapping.Employee.EmployeeId, mapping.Employee.FullName);
        }
    }

    private static bool MatchesSubscription(WebhookSubscription subscription, string eventType)
    {
        if (subscription.EventTypes == "*")
            return true;

        return subscription.EventTypes
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Any(item => string.Equals(item, eventType, StringComparison.OrdinalIgnoreCase));
    }

    private static async Task ExpireStaleInterventionRequestsAsync(ApplicationDbContext db, DateTime now, CancellationToken cancellationToken)
    {
        var expired = await db.OperationalInterventionRequests
            .Where(r =>
                r.Status == "Pending" &&
                r.ExpiresAtUtc != null &&
                r.ExpiresAtUtc <= now)
            .OrderBy(r => r.ExpiresAtUtc)
            .ThenBy(r => r.OperationalInterventionRequestId)
            .Take(100)
            .ToListAsync(cancellationToken);

        foreach (var request in expired)
        {
            request.Status = "Expired";
        }
    }

    private static string ComputeSignature(string secretReference, string payload)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secretReference));
        var bytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}
