using System.Text.Json;
using API.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.Extensions.Options;

namespace API.Services.Sync;

public sealed class SyncEntityEventFactory
{
    private static readonly HashSet<string> AreaNodeEntities =
    [
        nameof(AccessLog),
        nameof(Alarm),
        nameof(Visit),
        nameof(Camera),
        nameof(Lane),
        nameof(SecurityDevice),
        nameof(LaneEvent),
        nameof(DynamicQrScanLog),
        nameof(ChatConversation),
        nameof(ChatParticipant),
        nameof(ChatMessage),
        nameof(RemoteFaceEnrollmentJob)
    ];

    private static readonly HashSet<string> CentralEntities =
    [
        nameof(Employee),
        nameof(Vehicle),
        nameof(WatchlistEntry),
        nameof(AccessRule),
        nameof(Site),
        nameof(Gate),
        nameof(Lane),
        nameof(SecurityZone),
        nameof(AccessPoint),
        nameof(Camera),
        nameof(SecurityDevice),
        nameof(Visit),
        nameof(Alarm),
        nameof(ChatConversation),
        nameof(ChatParticipant),
        nameof(ChatMessage),
        nameof(RemoteFaceEnrollmentJob),
        nameof(EmployeeFaceModel)
    ];

    private readonly SyncRuntimeOptions _options;

    public SyncEntityEventFactory(IOptions<SyncRuntimeOptions> options)
    {
        _options = options.Value;
    }

    public bool IsEnabledForCurrentMode =>
        string.Equals(_options.Mode, SyncRuntimeModes.AreaNode, StringComparison.OrdinalIgnoreCase) ||
        string.Equals(_options.Mode, SyncRuntimeModes.Central, StringComparison.OrdinalIgnoreCase);

    public IReadOnlyList<PendingSyncEventCandidate> BuildCandidates(ChangeTracker changeTracker)
    {
        if (!IsEnabledForCurrentMode)
        {
            return [];
        }

        var candidates = new List<PendingSyncEventCandidate>();
        foreach (var entry in changeTracker.Entries().Where(entry =>
                     entry.State is EntityState.Added or EntityState.Modified or EntityState.Deleted &&
                     entry.Entity is not OutboxEvent &&
                     entry.Entity is not SystemAuditLog &&
                     entry.Entity is not SyncAreaNode &&
                     entry.Entity is not SyncAreaAssignment &&
                     entry.Entity is not SyncInboundEvent &&
                     entry.Entity is not SyncOutboundCheckpoint))
        {
            var entityName = entry.Metadata.ClrType.Name;
            if (!ShouldPublish(entityName))
            {
                continue;
            }

            var snapshotValues = entry.State == EntityState.Deleted ? entry.OriginalValues : entry.CurrentValues;
            var scope = ResolveScope(entry, snapshotValues);
            var payload = new
            {
                action = entry.State == EntityState.Deleted ? "Delete" : "Upsert",
                entityType = entityName,
                entity = BuildPropertyDictionary(snapshotValues),
                keys = BuildKeyDictionary(entry)
            };

            candidates.Add(new PendingSyncEventCandidate(
                entry,
                new OutboxEvent
                {
                    Channel = "Sync",
                    EventType = $"Sync.{entityName}.{(entry.State == EntityState.Deleted ? "Delete" : "Upsert")}",
                    AggregateType = entityName,
                    AggregateId = ResolveAggregateId(entry, snapshotValues),
                    PayloadJson = JsonSerializer.Serialize(payload),
                    Status = string.Equals(_options.Mode, SyncRuntimeModes.AreaNode, StringComparison.OrdinalIgnoreCase)
                        ? "PendingSync"
                        : "Published",
                    CompanyId = scope.CompanyId,
                    SiteId = scope.SiteId,
                    AreaNodeId = scope.AreaNodeId,
                    ScopeType = scope.ScopeType,
                    ScopeId = scope.ScopeId,
                    SourceSystem = string.Equals(_options.Mode, SyncRuntimeModes.Central, StringComparison.OrdinalIgnoreCase)
                        ? "Central"
                        : "AreaNode",
                    SchemaVersion = 1,
                    OccurredAtUtc = DateTime.UtcNow,
                    CreatedAtUtc = DateTime.UtcNow,
                    IsCanonical = string.Equals(_options.Mode, SyncRuntimeModes.Central, StringComparison.OrdinalIgnoreCase),
                    NextAttemptAtUtc = string.Equals(_options.Mode, SyncRuntimeModes.AreaNode, StringComparison.OrdinalIgnoreCase)
                        ? DateTime.UtcNow
                        : null
                }));
        }

        return candidates;
    }

    private bool ShouldPublish(string entityName)
    {
        if (string.Equals(_options.Mode, SyncRuntimeModes.AreaNode, StringComparison.OrdinalIgnoreCase))
        {
            return AreaNodeEntities.Contains(entityName);
        }

        if (string.Equals(_options.Mode, SyncRuntimeModes.Central, StringComparison.OrdinalIgnoreCase))
        {
            return CentralEntities.Contains(entityName);
        }

        return false;
    }

    private SyncScopeResolution ResolveScope(EntityEntry entry, PropertyValues values)
    {
        var scope = new SyncScopeResolution
        {
            CompanyId = _options.CompanyId,
            SiteId = _options.SiteId,
            AreaNodeId = string.Equals(_options.Mode, SyncRuntimeModes.AreaNode, StringComparison.OrdinalIgnoreCase)
                ? _options.LocalAreaNodeId
                : null
        };

        var siteId = TryGetInt(values, "SiteId") ?? TryGetInt(values, "PrimarySiteId");
        if (siteId.HasValue)
        {
            scope.SiteId = siteId;
            scope.ScopeType = "Site";
            scope.ScopeId = siteId;
        }

        var gateId = TryGetInt(values, "GateId");
        if (gateId.HasValue)
        {
            scope.ScopeType = "Gate";
            scope.ScopeId = gateId;
        }

        var laneId = TryGetInt(values, "LaneId");
        if (laneId.HasValue)
        {
            scope.ScopeType = "Lane";
            scope.ScopeId = laneId;
        }

        var zoneId = TryGetInt(values, "SecurityZoneId");
        if (zoneId.HasValue)
        {
            scope.ScopeType = "SecurityZone";
            scope.ScopeId = zoneId;
        }

        return scope;
    }

    private static string? ResolveAggregateId(EntityEntry entry, PropertyValues values)
    {
        var key = entry.Metadata.FindPrimaryKey();
        if (key?.Properties.Count == 1)
        {
            var prop = key.Properties[0];
            var value = entry.Property(prop.Name).CurrentValue ?? entry.Property(prop.Name).OriginalValue;
            return value?.ToString();
        }

        return TryGetString(values, $"{entry.Metadata.ClrType.Name}Id");
    }

    private static Dictionary<string, object?> BuildPropertyDictionary(PropertyValues values)
    {
        var dict = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        foreach (var prop in values.Properties)
        {
            dict[prop.Name] = values[prop.Name];
        }

        return dict;
    }

    private static Dictionary<string, object?> BuildKeyDictionary(EntityEntry entry)
    {
        var result = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        var key = entry.Metadata.FindPrimaryKey();
        if (key == null)
        {
            return result;
        }

        foreach (var prop in key.Properties)
        {
            result[prop.Name] = entry.Property(prop.Name).CurrentValue ?? entry.Property(prop.Name).OriginalValue;
        }

        return result;
    }

    private static int? TryGetInt(PropertyValues values, string propertyName)
    {
        var prop = values.Properties.FirstOrDefault(item => string.Equals(item.Name, propertyName, StringComparison.OrdinalIgnoreCase));
        if (prop == null)
        {
            return null;
        }

        var value = values[prop.Name];
        if (value is int intValue)
        {
            return intValue;
        }

        if (value is long longValue && longValue >= int.MinValue && longValue <= int.MaxValue)
        {
            return (int)longValue;
        }

        return int.TryParse(value?.ToString(), out var parsed) ? parsed : null;
    }

    private static string? TryGetString(PropertyValues values, string propertyName)
    {
        var prop = values.Properties.FirstOrDefault(item => string.Equals(item.Name, propertyName, StringComparison.OrdinalIgnoreCase));
        return prop == null ? null : values[prop.Name]?.ToString();
    }
}

public sealed record PendingSyncEventCandidate(EntityEntry Entry, OutboxEvent OutboxEvent);

public sealed class SyncScopeResolution
{
    public int? CompanyId { get; set; }
    public int? SiteId { get; set; }
    public string? AreaNodeId { get; set; }
    public string? ScopeType { get; set; }
    public int? ScopeId { get; set; }
}
