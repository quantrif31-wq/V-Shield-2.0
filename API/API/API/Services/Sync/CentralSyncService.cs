using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using API.Data;
using API.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace API.Services.Sync;

public class CentralSyncService
{
    private readonly ApplicationDbContext _db;
    private readonly SyncRuntimeOptions _options;
    private readonly ISyncExecutionContext _syncExecutionContext;
    private readonly SyncEventApplier _syncEventApplier;
    private readonly SyncRealtimeNotifier _syncRealtimeNotifier;

    public CentralSyncService(
        ApplicationDbContext db,
        IOptions<SyncRuntimeOptions> options,
        ISyncExecutionContext syncExecutionContext,
        SyncEventApplier syncEventApplier,
        SyncRealtimeNotifier syncRealtimeNotifier)
    {
        _db = db;
        _options = options.Value;
        _syncExecutionContext = syncExecutionContext;
        _syncEventApplier = syncEventApplier;
        _syncRealtimeNotifier = syncRealtimeNotifier;
    }

    public bool IsCentralMode => string.Equals(_options.Mode, SyncRuntimeModes.Central, StringComparison.OrdinalIgnoreCase);

    public async Task<SyncRegistrationResponse> RegisterNodeAsync(SyncRegistrationRequest request, string registrationKey, CancellationToken cancellationToken)
    {
        EnsureCentralMode();
        if (string.IsNullOrWhiteSpace(_options.RegistrationKey) || !string.Equals(_options.RegistrationKey, registrationKey, StringComparison.Ordinal))
        {
            throw new UnauthorizedAccessException("Invalid sync registration key.");
        }

        var node = await _db.SyncAreaNodes
            .Include(item => item.Assignments)
            .FirstOrDefaultAsync(item => item.AreaNodeId == request.AreaNodeId, cancellationToken);

        var secret = GenerateSecret();
        if (node == null)
        {
            node = new SyncAreaNode
            {
                AreaNodeId = request.AreaNodeId
            };
            _db.SyncAreaNodes.Add(node);
        }

        node.CompanyId = request.CompanyId;
        node.SiteId = request.SiteId;
        node.DisplayName = request.DisplayName;
        node.Version = request.Version;
        node.NodeSecretHash = HashSecret(secret);
        node.Mode = SyncRuntimeModes.AreaNode;
        node.Status = "Active";
        node.LastSeenAtUtc = DateTime.UtcNow;
        node.UpdatedAtUtc = DateTime.UtcNow;

        if (node.Assignments.Count > 0)
        {
            _db.SyncAreaAssignments.RemoveRange(node.Assignments);
        }

        var assignments = request.GateIds.Select(id => new SyncAreaAssignment { AreaNodeId = node.AreaNodeId, ScopeType = "Gate", ScopeId = id })
            .Concat(request.LaneIds.Select(id => new SyncAreaAssignment { AreaNodeId = node.AreaNodeId, ScopeType = "Lane", ScopeId = id }))
            .Concat(request.ZoneIds.Select(id => new SyncAreaAssignment { AreaNodeId = node.AreaNodeId, ScopeType = "SecurityZone", ScopeId = id }))
            .ToList();
        if (assignments.Count > 0)
        {
            _db.SyncAreaAssignments.AddRange(assignments);
        }

        var checkpoint = await _db.SyncOutboundCheckpoints.FirstOrDefaultAsync(item => item.AreaNodeId == node.AreaNodeId, cancellationToken);
        if (checkpoint == null)
        {
            _db.SyncOutboundCheckpoints.Add(new SyncOutboundCheckpoint { AreaNodeId = node.AreaNodeId });
        }

        await _db.SaveChangesAsync(cancellationToken);

        return new SyncRegistrationResponse(
            node.AreaNodeId,
            secret,
            node.CompanyId,
            node.SiteId,
            assignments.Select(item => new SyncScopeItemDto(item.ScopeType, item.ScopeId)).ToArray());
    }

    public async Task<SyncAreaNode> ValidateNodeAsync(string areaNodeId, string nodeSecret, CancellationToken cancellationToken)
    {
        EnsureCentralMode();
        var node = await _db.SyncAreaNodes.Include(item => item.Assignments)
            .FirstOrDefaultAsync(item => item.AreaNodeId == areaNodeId, cancellationToken);
        if (node == null || string.IsNullOrWhiteSpace(node.NodeSecretHash) || !string.Equals(node.NodeSecretHash, HashSecret(nodeSecret), StringComparison.Ordinal))
        {
            throw new UnauthorizedAccessException("Invalid sync node credentials.");
        }

        node.Status = "Active";
        node.LastSeenAtUtc = DateTime.UtcNow;
        node.UpdatedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
        return node;
    }

    public async Task<SyncBatchResponse> IngestUpstreamBatchAsync(SyncAreaNode node, SyncBatchRequest request, CancellationToken cancellationToken)
    {
        EnsureCentralMode();
        var results = new List<SyncEventAckDto>();

        foreach (var syncEvent in request.Events.Take(Math.Max(1, _options.BatchSize)))
        {
            if (!IsEventVisibleToNode(syncEvent.SiteId, syncEvent.ScopeType, syncEvent.ScopeId, node))
            {
                results.Add(new SyncEventAckDto(syncEvent.CorrelationId, false, null, "Event scope is outside assigned area."));
                continue;
            }

            var existing = await _db.SyncInboundEvents
                .FirstOrDefaultAsync(item => item.AreaNodeId == node.AreaNodeId && item.CorrelationId == syncEvent.CorrelationId, cancellationToken);
            if (existing != null)
            {
                if (string.Equals(existing.Status, "Pending", StringComparison.OrdinalIgnoreCase))
                {
                    await ApplyInboundEventAsync(existing, syncEvent, cancellationToken);
                }

                results.Add(new SyncEventAckDto(
                    syncEvent.CorrelationId,
                    string.Equals(existing.Status, "Applied", StringComparison.OrdinalIgnoreCase),
                    existing.AppliedAggregateId,
                    existing.FailureReason));
                continue;
            }

            var inbound = new SyncInboundEvent
            {
                AreaNodeId = node.AreaNodeId,
                CompanyId = syncEvent.CompanyId ?? node.CompanyId,
                SiteId = syncEvent.SiteId ?? node.SiteId,
                ScopeType = syncEvent.ScopeType,
                ScopeId = syncEvent.ScopeId,
                EventType = syncEvent.EventType,
                AggregateType = syncEvent.AggregateType,
                AggregateId = syncEvent.AggregateId,
                CorrelationId = syncEvent.CorrelationId,
                SourceSystem = syncEvent.SourceSystem,
                SchemaVersion = syncEvent.SchemaVersion,
                PayloadJson = syncEvent.PayloadJson,
                Status = "Pending",
                OccurredAtUtc = syncEvent.OccurredAtUtc,
                ReceivedAtUtc = DateTime.UtcNow
            };
            _db.SyncInboundEvents.Add(inbound);
            await _db.SaveChangesAsync(cancellationToken);

            await ApplyInboundEventAsync(inbound, syncEvent, cancellationToken);
            results.Add(new SyncEventAckDto(
                syncEvent.CorrelationId,
                string.Equals(inbound.Status, "Applied", StringComparison.OrdinalIgnoreCase),
                inbound.AppliedAggregateId,
                inbound.FailureReason));
        }

        return new SyncBatchResponse(results.Count(item => item.Accepted), results.Count(item => !item.Accepted), results);
    }

    public async Task<int> ProcessPendingInboundBatchAsync(CancellationToken cancellationToken)
    {
        EnsureCentralMode();
        var pending = await _db.SyncInboundEvents
            .Where(item => item.Status == "Pending")
            .OrderBy(item => item.SyncInboundEventId)
            .Take(Math.Max(1, _options.BatchSize))
            .ToListAsync(cancellationToken);

        foreach (var inbound in pending)
        {
            await ApplyInboundEventAsync(inbound, BuildSyncEvent(inbound), cancellationToken);
        }

        return pending.Count;
    }

    public async Task<SyncDownstreamFeedResponse> GetDownstreamFeedAsync(SyncAreaNode node, long afterSequence, CancellationToken cancellationToken)
    {
        EnsureCentralMode();
        var visible = new List<SyncOutboxEnvelopeDto>();
        var cursor = afterSequence;
        var maxScannedSequence = afterSequence;
        var pageSize = Math.Max(1, _options.BatchSize);
        var maxRounds = Math.Max(1, _options.DownstreamScanMultiplier);

        for (var round = 0; round < maxRounds && visible.Count < pageSize; round++)
        {
            var events = await _db.OutboxEvents
                .Where(item => item.Channel == "Sync" && item.Status == "Published" && item.OutboxEventId > cursor)
                .OrderBy(item => item.OutboxEventId)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            if (events.Count == 0)
            {
                break;
            }

            maxScannedSequence = Math.Max(maxScannedSequence, events[^1].OutboxEventId);
            cursor = events[^1].OutboxEventId;

            foreach (var item in events)
            {
                if (!IsEventVisibleToNode(item.SiteId, item.ScopeType, item.ScopeId, node))
                {
                    continue;
                }

                visible.Add(BuildEnvelope(item));
                if (visible.Count >= pageSize)
                {
                    break;
                }
            }
        }

        var checkpoint = await _db.SyncOutboundCheckpoints.FirstOrDefaultAsync(item => item.AreaNodeId == node.AreaNodeId, cancellationToken);
        if (checkpoint != null && maxScannedSequence > afterSequence)
        {
            checkpoint.LastDeliveredOutboxEventId = Math.Max(checkpoint.LastDeliveredOutboxEventId, maxScannedSequence);
            checkpoint.LastPulledAtUtc = DateTime.UtcNow;
            checkpoint.UpdatedAtUtc = DateTime.UtcNow;
            await _db.SaveChangesAsync(cancellationToken);
        }

        return new SyncDownstreamFeedResponse(afterSequence, maxScannedSequence, visible);
    }

    public async Task RecordAckAsync(SyncAreaNode node, long lastAcknowledgedOutboxEventId, CancellationToken cancellationToken)
    {
        EnsureCentralMode();
        var checkpoint = await _db.SyncOutboundCheckpoints.FirstOrDefaultAsync(item => item.AreaNodeId == node.AreaNodeId, cancellationToken);
        if (checkpoint == null)
        {
            checkpoint = new SyncOutboundCheckpoint { AreaNodeId = node.AreaNodeId };
            _db.SyncOutboundCheckpoints.Add(checkpoint);
        }

        checkpoint.LastAcknowledgedOutboxEventId = Math.Max(checkpoint.LastAcknowledgedOutboxEventId, lastAcknowledgedOutboxEventId);
        checkpoint.LastAcknowledgedAtUtc = DateTime.UtcNow;
        checkpoint.UpdatedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<SyncBootstrapResponse> BuildBootstrapAsync(SyncAreaNode node, CancellationToken cancellationToken)
    {
        EnsureCentralMode();
        var maxSequence = await _db.OutboxEvents
            .Where(item => item.Channel == "Sync")
            .Select(item => (long?)item.OutboxEventId)
            .MaxAsync(cancellationToken) ?? 0;

        var results = new List<SyncOutboxEnvelopeDto>();
        var isGlobalNode = IsGlobalNode(node);
        var siteId = node.SiteId;
        if (isGlobalNode)
        {
            var sites = await _db.Sites.ToListAsync(cancellationToken);
            results.AddRange(sites.Select(item => BuildSyntheticEnvelope(item, item.SiteId.ToString(), "Site", item.SiteId, node)));

            var gates = await _db.Gates.ToListAsync(cancellationToken);
            results.AddRange(gates.Select(item => BuildSyntheticEnvelope(item, item.GateId.ToString(), "Gate", item.GateId, node)));

            var lanes = await _db.Lanes.ToListAsync(cancellationToken);
            results.AddRange(lanes.Select(item => BuildSyntheticEnvelope(item, item.LaneId.ToString(), "Lane", item.LaneId, node)));

            var zones = await _db.SecurityZones.ToListAsync(cancellationToken);
            results.AddRange(zones.Select(item => BuildSyntheticEnvelope(item, item.SecurityZoneId.ToString(), "SecurityZone", item.SecurityZoneId, node)));

            var accessPoints = await _db.AccessPoints.ToListAsync(cancellationToken);
            results.AddRange(accessPoints.Select(item => BuildSyntheticEnvelope(item, item.AccessPointId.ToString(), "AccessPoint", item.AccessPointId, node)));

            var employees = await _db.Employees.ToListAsync(cancellationToken);
            results.AddRange(employees.Select(item => BuildSyntheticEnvelope(item, item.EmployeeId.ToString(), item.PrimarySiteId.HasValue ? "Site" : null, item.PrimarySiteId, node)));

            var vehicles = await _db.Vehicles.ToListAsync(cancellationToken);
            results.AddRange(vehicles.Select(item => BuildSyntheticEnvelope(item, item.VehicleId.ToString(), item.SiteId.HasValue ? "Site" : null, item.SiteId, node)));

            var accessRules = await _db.AccessRules.ToListAsync(cancellationToken);
            results.AddRange(accessRules.Select(item => BuildSyntheticEnvelope(
                item,
                item.AccessRuleId.ToString(),
                item.SecurityZoneId.HasValue ? "SecurityZone" : item.SiteId.HasValue ? "Site" : null,
                item.SecurityZoneId ?? item.SiteId,
                node)));

            var cameras = await _db.Cameras.ToListAsync(cancellationToken);
            results.AddRange(cameras.Select(item => BuildSyntheticEnvelope(
                item,
                item.CameraId.ToString(),
                item.GateId.HasValue ? "Gate" : null,
                item.GateId,
                node)));

            var devices = await _db.SecurityDevices.ToListAsync(cancellationToken);
            results.AddRange(devices.Select(item => BuildSyntheticEnvelope(item, item.SecurityDeviceId.ToString(), item.SiteId.HasValue ? "Site" : null, item.SiteId, node)));
        }
        else if (siteId.HasValue)
        {
            var site = await _db.Sites.FirstOrDefaultAsync(item => item.SiteId == siteId.Value, cancellationToken);
            if (site != null) results.Add(BuildSyntheticEnvelope(site, site.SiteId.ToString(), "Site", site.SiteId, node));

            var gateIds = await _db.Lanes
                .Where(item => item.SiteId == siteId.Value && item.GateId.HasValue)
                .Select(item => item.GateId!.Value)
                .Distinct()
                .ToListAsync(cancellationToken);

            var gates = gateIds.Count == 0
                ? new List<Gate>()
                : await _db.Gates.Where(item => gateIds.Contains(item.GateId)).ToListAsync(cancellationToken);
            results.AddRange(gates.Select(item => BuildSyntheticEnvelope(item, item.GateId.ToString(), "Gate", item.GateId, node)));

            var lanes = await _db.Lanes.Where(item => item.SiteId == siteId.Value).ToListAsync(cancellationToken);
            results.AddRange(lanes.Select(item => BuildSyntheticEnvelope(item, item.LaneId.ToString(), "Lane", item.LaneId, node)));

            var zones = await _db.SecurityZones.Where(item => item.SiteId == siteId.Value).ToListAsync(cancellationToken);
            results.AddRange(zones.Select(item => BuildSyntheticEnvelope(item, item.SecurityZoneId.ToString(), "SecurityZone", item.SecurityZoneId, node)));

            var accessPoints = await _db.AccessPoints.Where(item => item.SiteId == siteId.Value).ToListAsync(cancellationToken);
            results.AddRange(accessPoints.Select(item => BuildSyntheticEnvelope(item, item.AccessPointId.ToString(), "AccessPoint", item.AccessPointId, node)));

            var employees = await _db.Employees.Where(item => item.PrimarySiteId == siteId.Value).ToListAsync(cancellationToken);
            results.AddRange(employees.Select(item => BuildSyntheticEnvelope(item, item.EmployeeId.ToString(), "Site", siteId.Value, node)));

            var vehicles = await _db.Vehicles.Where(item => item.SiteId == siteId.Value).ToListAsync(cancellationToken);
            results.AddRange(vehicles.Select(item => BuildSyntheticEnvelope(item, item.VehicleId.ToString(), "Site", siteId.Value, node)));

            var accessRules = await _db.AccessRules.Where(item => item.SiteId == siteId.Value || item.SiteId == null).ToListAsync(cancellationToken);
            results.AddRange(accessRules.Select(item => BuildSyntheticEnvelope(item, item.AccessRuleId.ToString(), item.SecurityZoneId.HasValue ? "SecurityZone" : "Site", item.SecurityZoneId ?? siteId.Value, node)));

            var cameras = gateIds.Count == 0
                ? new List<Camera>()
                : await _db.Cameras.Where(item => item.GateId.HasValue && gateIds.Contains(item.GateId.Value)).ToListAsync(cancellationToken);
            results.AddRange(cameras.Select(item => BuildSyntheticEnvelope(item, item.CameraId.ToString(), item.GateId.HasValue ? "Gate" : "Site", item.GateId ?? siteId.Value, node)));

            var devices = await _db.SecurityDevices.Where(item => item.SiteId == siteId.Value).ToListAsync(cancellationToken);
            results.AddRange(devices.Select(item => BuildSyntheticEnvelope(item, item.SecurityDeviceId.ToString(), "Site", siteId.Value, node)));
        }

        var watchlistEntries = await _db.WatchlistEntries.Where(item => item.IsActive).ToListAsync(cancellationToken);
        results.AddRange(watchlistEntries.Select(item => BuildSyntheticEnvelope(item, item.WatchlistEntryId.ToString(), null, null, node)));

        var chatConversations = await _db.ChatConversations.ToListAsync(cancellationToken);
        results.AddRange(chatConversations.Select(item => BuildSyntheticEnvelope(item, item.ConversationId.ToString(), null, null, node)));

        var chatParticipants = await _db.ChatParticipants.ToListAsync(cancellationToken);
        results.AddRange(chatParticipants.Select(item => BuildSyntheticEnvelope(item, item.ParticipantId.ToString(), null, null, node)));

        var chatMessages = await _db.ChatMessages.ToListAsync(cancellationToken);
        results.AddRange(chatMessages.Select(item => BuildSyntheticEnvelope(item, item.MessageId.ToString(), null, null, node)));

        return new SyncBootstrapResponse(maxSequence, results);
    }

    private SyncOutboxEnvelopeDto BuildEnvelope(OutboxEvent item)
    {
        return new SyncOutboxEnvelopeDto(item.OutboxEventId, new SyncEventDto(
            item.EventType,
            item.AggregateType,
            item.AggregateId,
            item.CorrelationId,
            item.PayloadJson,
            item.CompanyId,
            item.SiteId,
            item.AreaNodeId,
            item.ScopeType,
            item.ScopeId,
            item.SourceSystem,
            item.SchemaVersion,
            item.OccurredAtUtc));
    }

    private SyncOutboxEnvelopeDto BuildSyntheticEnvelope<T>(T entity, string aggregateId, string? scopeType, int? scopeId, SyncAreaNode node)
    {
        var payload = JsonSerializer.Serialize(new
        {
            action = "Upsert",
            entityType = typeof(T).Name,
            entity = BuildEntityDictionary(entity)
        });

        return new SyncOutboxEnvelopeDto(0, new SyncEventDto(
            $"Sync.{typeof(T).Name}.Upsert",
            typeof(T).Name,
            aggregateId,
            Guid.NewGuid().ToString("N"),
            payload,
            node.CompanyId,
            node.SiteId,
            node.AreaNodeId,
            scopeType,
            scopeId,
            "Central",
            1,
            DateTime.UtcNow));
    }

    private static Dictionary<string, object?> BuildEntityDictionary<T>(T entity)
    {
        return typeof(T)
            .GetProperties()
            .Where(property => property.CanRead && IsScalarType(property.PropertyType))
            .ToDictionary(property => property.Name, property => property.GetValue(entity), StringComparer.OrdinalIgnoreCase);
    }

    private bool IsEventVisibleToNode(int? siteId, string? scopeType, int? scopeId, SyncAreaNode node)
    {
        if (IsGlobalNode(node))
        {
            return true;
        }

        if (node.SiteId.HasValue && siteId.HasValue && node.SiteId.Value != siteId.Value)
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(scopeType) || !scopeId.HasValue)
        {
            return true;
        }

        if (string.Equals(scopeType, "Site", StringComparison.OrdinalIgnoreCase))
        {
            return !node.SiteId.HasValue || node.SiteId.Value == scopeId.Value;
        }

        return node.Assignments.Any(item =>
            string.Equals(item.ScopeType, scopeType, StringComparison.OrdinalIgnoreCase) &&
            item.ScopeId == scopeId.Value);
    }

    private static bool IsGlobalNode(SyncAreaNode node) => node.Assignments.Count == 0;

    private void EnsureCentralMode()
    {
        if (!IsCentralMode)
        {
            throw new InvalidOperationException("Sync central endpoints are only available in Central mode.");
        }
    }

    private static bool IsScalarType(Type propertyType)
    {
        var underlyingType = Nullable.GetUnderlyingType(propertyType) ?? propertyType;
        return underlyingType.IsPrimitive ||
               underlyingType.IsEnum ||
               underlyingType == typeof(string) ||
               underlyingType == typeof(decimal) ||
               underlyingType == typeof(DateTime) ||
               underlyingType == typeof(Guid) ||
               underlyingType == typeof(bool);
    }

    private static string GenerateSecret() => Convert.ToHexString(RandomNumberGenerator.GetBytes(32)).ToLowerInvariant();

    private static string HashSecret(string value)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(value));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    private async Task ApplyInboundEventAsync(SyncInboundEvent inbound, SyncEventDto syncEvent, CancellationToken cancellationToken)
    {
        try
        {
            _syncExecutionContext.SuppressOutboxPublishing = true;
            var appliedAggregateId = await _syncEventApplier.ApplyAsync(syncEvent, cancellationToken);
            inbound.AppliedAggregateId = appliedAggregateId;
            inbound.Status = "Applied";
            inbound.FailureReason = null;

            var existingCanonical = await _db.OutboxEvents.AnyAsync(item =>
                item.Channel == "Sync" &&
                item.CorrelationId == syncEvent.CorrelationId &&
                item.SourceSystem == "Central", cancellationToken);

            if (!existingCanonical)
            {
                _db.OutboxEvents.Add(new OutboxEvent
                {
                    Channel = "Sync",
                    EventType = syncEvent.EventType,
                    AggregateType = syncEvent.AggregateType,
                    AggregateId = appliedAggregateId ?? syncEvent.AggregateId,
                    PayloadJson = syncEvent.PayloadJson,
                    Status = "Published",
                    CompanyId = syncEvent.CompanyId,
                    SiteId = syncEvent.SiteId,
                    AreaNodeId = inbound.AreaNodeId,
                    ScopeType = syncEvent.ScopeType,
                    ScopeId = syncEvent.ScopeId,
                    SourceSystem = "Central",
                    SchemaVersion = syncEvent.SchemaVersion,
                    OccurredAtUtc = syncEvent.OccurredAtUtc,
                    CreatedAtUtc = DateTime.UtcNow,
                    IsCanonical = true,
                    CorrelationId = syncEvent.CorrelationId
                });
            }

            await _db.SaveChangesAsync(cancellationToken);
            await _syncRealtimeNotifier.PublishAsync(
                syncEvent.AggregateType,
                appliedAggregateId ?? syncEvent.AggregateId,
                GetAction(syncEvent.PayloadJson),
                "Central",
                cancellationToken);
        }
        catch (Exception ex)
        {
            // Applying an event can leave a failed Added/Modified aggregate in
            // the shared DbContext. Detach it before recording the rejection;
            // otherwise the rejection itself fails and returns HTTP 409, which
            // blocks every later offline event in the node's outbox.
            _db.ChangeTracker.Clear();
            _db.SyncInboundEvents.Attach(inbound);
            inbound.Status = "Rejected";
            inbound.FailureReason = ex.Message.Length > 240 ? ex.Message[..240] : ex.Message;
            await _db.SaveChangesAsync(cancellationToken);
        }
        finally
        {
            _syncExecutionContext.SuppressOutboxPublishing = false;
        }
    }

    private static SyncEventDto BuildSyncEvent(SyncInboundEvent inbound)
    {
        return new SyncEventDto(
            inbound.EventType,
            inbound.AggregateType,
            inbound.AggregateId,
            inbound.CorrelationId,
            inbound.PayloadJson,
            inbound.CompanyId,
            inbound.SiteId,
            inbound.AreaNodeId,
            inbound.ScopeType,
            inbound.ScopeId,
            inbound.SourceSystem,
            inbound.SchemaVersion,
            inbound.OccurredAtUtc);
    }

    private static string GetAction(string payloadJson)
    {
        using var doc = JsonDocument.Parse(payloadJson);
        return doc.RootElement.TryGetProperty("action", out var action) ? action.GetString() ?? "Upsert" : "Upsert";
    }
}
