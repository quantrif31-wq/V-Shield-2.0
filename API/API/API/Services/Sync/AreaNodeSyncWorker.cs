using System.Net;
using System.Net.Http.Json;
using System.Reflection;
using System.Text.Json;
using API.Data;
using API.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace API.Services.Sync;

public class AreaNodeSyncWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<AreaNodeSyncWorker> _logger;
    private readonly SyncRuntimeOptions _options;

    public AreaNodeSyncWorker(
        IServiceScopeFactory scopeFactory,
        IHttpClientFactory httpClientFactory,
        ILogger<AreaNodeSyncWorker> logger,
        IOptions<SyncRuntimeOptions> options)
    {
        _scopeFactory = scopeFactory;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        _options = options.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!string.Equals(_options.Mode, SyncRuntimeModes.AreaNode, StringComparison.OrdinalIgnoreCase) ||
            string.IsNullOrWhiteSpace(_options.CentralBaseUrl) ||
            string.IsNullOrWhiteSpace(_options.LocalAreaNodeId))
        {
            return;
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await RunOnceAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Area node sync cycle failed.");
            }

            await Task.Delay(TimeSpan.FromSeconds(Math.Max(1, Math.Min(_options.PushIntervalSeconds, _options.PullIntervalSeconds))), stoppingToken);
        }
    }

    private async Task RunOnceAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var syncSystemConfigStore = scope.ServiceProvider.GetRequiredService<SyncSystemConfigStore>();
        var syncExecutionContext = scope.ServiceProvider.GetRequiredService<ISyncExecutionContext>();
        var syncEventApplier = scope.ServiceProvider.GetRequiredService<SyncEventApplier>();
        var realtimeNotifier = scope.ServiceProvider.GetRequiredService<SyncRealtimeNotifier>();

        var nodeSecret = await syncSystemConfigStore.GetNodeSecretAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(nodeSecret))
        {
            nodeSecret = await RegisterNodeAsync(syncSystemConfigStore, cancellationToken);
        }

        try
        {
            await RunSyncCycleAsync(
                db,
                syncSystemConfigStore,
                syncExecutionContext,
                syncEventApplier,
                realtimeNotifier,
                nodeSecret!,
                cancellationToken);
        }
        catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.Unauthorized)
        {
            _logger.LogInformation("Sync node credentials expired or were reset upstream. Re-registering area node {AreaNodeId}.", _options.LocalAreaNodeId);
            await ResetLocalSessionAsync(syncSystemConfigStore, cancellationToken);
            nodeSecret = await RegisterNodeAsync(syncSystemConfigStore, cancellationToken);
            await RunSyncCycleAsync(
                db,
                syncSystemConfigStore,
                syncExecutionContext,
                syncEventApplier,
                realtimeNotifier,
                nodeSecret,
                cancellationToken);
        }
    }

    private async Task RunSyncCycleAsync(
        ApplicationDbContext db,
        SyncSystemConfigStore syncSystemConfigStore,
        ISyncExecutionContext syncExecutionContext,
        SyncEventApplier syncEventApplier,
        SyncRealtimeNotifier realtimeNotifier,
        string nodeSecret,
        CancellationToken cancellationToken)
    {
        var bootstrapCompleted = await syncSystemConfigStore.GetBootstrapCompletedAsync(cancellationToken);
        if (!bootstrapCompleted)
        {
            await BootstrapAsync(syncSystemConfigStore, syncExecutionContext, syncEventApplier, realtimeNotifier, nodeSecret, cancellationToken);
        }

        await PushPendingEventsAsync(db, nodeSecret, cancellationToken);
        await PullDownstreamEventsAsync(syncSystemConfigStore, syncExecutionContext, syncEventApplier, realtimeNotifier, nodeSecret, cancellationToken);
    }

    private static async Task ResetLocalSessionAsync(SyncSystemConfigStore store, CancellationToken cancellationToken)
    {
        await store.SetNodeSecretAsync(string.Empty, cancellationToken);
        await store.SetBootstrapCompletedAsync(false, cancellationToken);
        await store.SetLastPulledSequenceAsync(0, cancellationToken);
    }

    private async Task<string> RegisterNodeAsync(SyncSystemConfigStore store, CancellationToken cancellationToken)
    {
        var request = new SyncRegistrationRequest(
            _options.LocalAreaNodeId!,
            _options.CompanyId,
            _options.SiteId,
            string.IsNullOrWhiteSpace(_options.DisplayName) ? _options.LocalAreaNodeId! : _options.DisplayName!,
            _options.Version,
            SyncScopeParser.ParseIds(_options.AssignedGateIds),
            SyncScopeParser.ParseIds(_options.AssignedLaneIds),
            SyncScopeParser.ParseIds(_options.AssignedZoneIds));

        using var requestMessage = new HttpRequestMessage(HttpMethod.Post, $"{_options.CentralBaseUrl!.TrimEnd('/')}/api/sync/nodes/register")
        {
            Content = JsonContent.Create(request)
        };
        requestMessage.Headers.Add("X-VShield-Registration-Key", _options.RegistrationKey ?? string.Empty);

        var client = _httpClientFactory.CreateClient();
        using var response = await client.SendAsync(requestMessage, cancellationToken);
        response.EnsureSuccessStatusCode();
        var payload = await response.Content.ReadFromJsonAsync<SyncRegistrationResponse>(cancellationToken: cancellationToken)
            ?? throw new InvalidOperationException("Invalid sync registration response.");
        await store.SetNodeSecretAsync(payload.NodeSecret, cancellationToken);
        return payload.NodeSecret;
    }

    private async Task BootstrapAsync(
        SyncSystemConfigStore store,
        ISyncExecutionContext syncExecutionContext,
        SyncEventApplier syncEventApplier,
        SyncRealtimeNotifier realtimeNotifier,
        string nodeSecret,
        CancellationToken cancellationToken)
    {
        using var request = CreateNodeRequest(HttpMethod.Get, "/api/sync/bootstrap/master-data", nodeSecret);
        var client = _httpClientFactory.CreateClient();
        using var response = await client.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();
        var payload = await response.Content.ReadFromJsonAsync<SyncBootstrapResponse>(cancellationToken: cancellationToken)
            ?? throw new InvalidOperationException("Invalid sync bootstrap response.");

        syncExecutionContext.SuppressOutboxPublishing = true;
        try
        {
            foreach (var envelope in payload.Events)
            {
                string? appliedId = null;
                try
                {
                    appliedId = await syncEventApplier.ApplyAsync(envelope.Event, cancellationToken);
                    await realtimeNotifier.PublishAsync(envelope.Event.AggregateType, appliedId ?? envelope.Event.AggregateId, "Upsert", "Central", cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Skipping bootstrap sync event {AggregateType}/{AggregateId}.", envelope.Event.AggregateType, envelope.Event.AggregateId);
                }
            }
        }
        finally
        {
            syncExecutionContext.SuppressOutboxPublishing = false;
        }

        await store.SetLastPulledSequenceAsync(payload.RecommendedSequence, cancellationToken);
        await store.SetBootstrapCompletedAsync(true, cancellationToken);
    }

    private async Task PushPendingEventsAsync(ApplicationDbContext db, string nodeSecret, CancellationToken cancellationToken)
    {
        await EnsureChatDependencyEventsAsync(db, cancellationToken);

        var pending = await db.OutboxEvents
            .Where(item => item.Channel == "Sync" && item.Status == "PendingSync")
            .Take(Math.Max(1, _options.BatchSize))
            .ToListAsync(cancellationToken);

        if (pending.Count == 0)
        {
            return;
        }

        pending = pending
            .OrderBy(item => GetSyncPriority(item.AggregateType))
            .ThenBy(item => item.OutboxEventId)
            .ToList();

        var request = new SyncBatchRequest(
            Guid.NewGuid().ToString("N"),
            _options.LocalAreaNodeId!,
            _options.CompanyId,
            _options.SiteId,
            DateTime.UtcNow,
            pending.Select(item => new SyncEventDto(
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
                item.OccurredAtUtc)).ToArray());

        using var requestMessage = CreateNodeRequest(HttpMethod.Post, "/api/sync/upstream/events", nodeSecret, request);
        var client = _httpClientFactory.CreateClient();
        using var response = await client.SendAsync(requestMessage, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("Sync upstream batch failed with status code {StatusCode}", response.StatusCode);
            return;
        }

        var payload = await response.Content.ReadFromJsonAsync<SyncBatchResponse>(cancellationToken: cancellationToken);
        var results = payload?.Results.ToDictionary(item => item.CorrelationId, StringComparer.OrdinalIgnoreCase)
            ?? new Dictionary<string, SyncEventAckDto>(StringComparer.OrdinalIgnoreCase);

        foreach (var item in pending)
        {
            if (!results.TryGetValue(item.CorrelationId, out var ack))
            {
                continue;
            }

            if (ack.Accepted)
            {
                item.Status = "Synced";
                item.DispatchedAtUtc = DateTime.UtcNow;
                item.NextAttemptAtUtc = null;
            }
            else
            {
                item.Status = "PendingSync";
                item.RetryCount++;
                item.NextAttemptAtUtc = DateTime.UtcNow.AddSeconds(Math.Min(60, Math.Max(5, item.RetryCount * 5)));
            }
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    private async Task EnsureChatDependencyEventsAsync(ApplicationDbContext db, CancellationToken cancellationToken)
    {
        var pendingChatMessages = await db.OutboxEvents
            .Where(item => item.Channel == "Sync" &&
                           item.Status == "PendingSync" &&
                           item.AggregateType == nameof(ChatMessage))
            .OrderBy(item => item.OutboxEventId)
            .ToListAsync(cancellationToken);

        if (pendingChatMessages.Count == 0)
        {
            return;
        }

        var added = false;
        foreach (var pendingMessage in pendingChatMessages)
        {
            using var payload = JsonDocument.Parse(pendingMessage.PayloadJson);
            if (!payload.RootElement.TryGetProperty("entity", out var entity))
            {
                continue;
            }

            var conversationId = entity.TryGetProperty(nameof(ChatMessage.ConversationId), out var conversationProperty)
                ? conversationProperty.GetInt32()
                : 0;
            if (conversationId <= 0)
            {
                continue;
            }

            var conversation = await db.ChatConversations
                .AsNoTracking()
                .FirstOrDefaultAsync(item => item.ConversationId == conversationId, cancellationToken);
            if (conversation == null)
            {
                continue;
            }

            if (!await HasSyncEnvelopeAsync(db, nameof(ChatConversation), conversation.ConversationId.ToString(), cancellationToken))
            {
                db.OutboxEvents.Add(BuildSyncOutboxEvent(nameof(ChatConversation), conversation.ConversationId.ToString(), conversation));
                added = true;
            }

            var participants = await db.ChatParticipants
                .AsNoTracking()
                .Where(item => item.ConversationId == conversationId)
                .ToListAsync(cancellationToken);

            foreach (var participant in participants)
            {
                if (await HasSyncEnvelopeAsync(db, nameof(ChatParticipant), participant.ParticipantId.ToString(), cancellationToken))
                {
                    continue;
                }

                db.OutboxEvents.Add(BuildSyncOutboxEvent(nameof(ChatParticipant), participant.ParticipantId.ToString(), participant));
                added = true;
            }
        }

        if (added)
        {
            await db.SaveChangesAsync(cancellationToken);
        }
    }

    private Task<bool> HasSyncEnvelopeAsync(ApplicationDbContext db, string aggregateType, string aggregateId, CancellationToken cancellationToken)
    {
        return db.OutboxEvents.AnyAsync(item =>
            item.Channel == "Sync" &&
            item.AggregateType == aggregateType &&
            item.AggregateId == aggregateId &&
            (item.Status == "PendingSync" || item.Status == "Synced"), cancellationToken);
    }

    private OutboxEvent BuildSyncOutboxEvent(string entityType, string aggregateId, object entity)
    {
        var payload = JsonSerializer.Serialize(new
        {
            action = "Upsert",
            entityType,
            entity = BuildEntityDictionary(entity),
            keys = BuildKeyDictionary(entity)
        });

        return new OutboxEvent
        {
            Channel = "Sync",
            EventType = $"Sync.{entityType}.Upsert",
            AggregateType = entityType,
            AggregateId = aggregateId,
            PayloadJson = payload,
            Status = "PendingSync",
            CompanyId = _options.CompanyId,
            SiteId = _options.SiteId,
            AreaNodeId = _options.LocalAreaNodeId,
            SourceSystem = "AreaNode",
            SchemaVersion = 1,
            OccurredAtUtc = DateTime.UtcNow,
            CreatedAtUtc = DateTime.UtcNow,
            NextAttemptAtUtc = DateTime.UtcNow
        };
    }

    private static Dictionary<string, object?> BuildEntityDictionary(object entity)
    {
        return entity.GetType()
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(property => property.CanRead && IsScalarProperty(property.PropertyType))
            .ToDictionary(property => property.Name, property => property.GetValue(entity), StringComparer.OrdinalIgnoreCase);
    }

    private static Dictionary<string, object?> BuildKeyDictionary(object entity)
    {
        var keyProperty = entity.GetType()
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .FirstOrDefault(property => property.Name.EndsWith("Id", StringComparison.OrdinalIgnoreCase));

        if (keyProperty == null)
        {
            return new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        }

        return new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
        {
            [keyProperty.Name] = keyProperty.GetValue(entity)
        };
    }

    private static bool IsScalarProperty(Type propertyType)
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

    private static int GetSyncPriority(string aggregateType) => aggregateType switch
    {
        nameof(ChatConversation) => 0,
        nameof(ChatParticipant) => 1,
        nameof(ChatMessage) => 2,
        _ => 10
    };

    private async Task PullDownstreamEventsAsync(
        SyncSystemConfigStore store,
        ISyncExecutionContext syncExecutionContext,
        SyncEventApplier syncEventApplier,
        SyncRealtimeNotifier realtimeNotifier,
        string nodeSecret,
        CancellationToken cancellationToken)
    {
        var lastSequence = await store.GetLastPulledSequenceAsync(cancellationToken);
        using var request = CreateNodeRequest(HttpMethod.Get, $"/api/sync/downstream/events?afterSequence={lastSequence}", nodeSecret);
        var client = _httpClientFactory.CreateClient();
        using var response = await client.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();
        var payload = await response.Content.ReadFromJsonAsync<SyncDownstreamFeedResponse>(cancellationToken: cancellationToken)
            ?? throw new InvalidOperationException("Invalid sync downstream response.");

        var maxAppliedSequence = Math.Max(lastSequence, payload.ToSequence);
        syncExecutionContext.SuppressOutboxPublishing = true;
        try
        {
            foreach (var envelope in payload.Events)
            {
                if (ShouldSkipEcho(envelope.Event))
                {
                    maxAppliedSequence = Math.Max(maxAppliedSequence, envelope.OutboxEventId);
                    continue;
                }

                string? appliedId = null;
                try
                {
                    appliedId = await syncEventApplier.ApplyAsync(envelope.Event, cancellationToken);
                    await realtimeNotifier.PublishAsync(envelope.Event.AggregateType, appliedId ?? envelope.Event.AggregateId, "Upsert", envelope.Event.SourceSystem, cancellationToken);
                }
                catch (Exception ex)
                {
                    // 1 event lỗi không được chặn sequence, tránh pull lại vô hạn.
                    _logger.LogWarning(ex, "Skipping downstream sync event {AggregateType}/{AggregateId}.", envelope.Event.AggregateType, envelope.Event.AggregateId);
                }
                maxAppliedSequence = Math.Max(maxAppliedSequence, envelope.OutboxEventId);
            }
        }
        finally
        {
            syncExecutionContext.SuppressOutboxPublishing = false;
        }

        if (maxAppliedSequence > lastSequence)
        {
            await store.SetLastPulledSequenceAsync(maxAppliedSequence, cancellationToken);
            using var ackRequest = CreateNodeRequest(HttpMethod.Post, "/api/sync/downstream/ack", nodeSecret, new SyncAckRequest(maxAppliedSequence));
            using var ackResponse = await client.SendAsync(ackRequest, cancellationToken);
            ackResponse.EnsureSuccessStatusCode();
        }
    }

    private bool ShouldSkipEcho(SyncEventDto syncEvent)
    {
        return string.Equals(syncEvent.AreaNodeId, _options.LocalAreaNodeId, StringComparison.OrdinalIgnoreCase) &&
               syncEvent.AggregateType is "AccessLog" or "LaneEvent" or "DynamicQrScanLog";
    }

    private HttpRequestMessage CreateNodeRequest<TBody>(HttpMethod method, string relativePath, string nodeSecret, TBody body)
    {
        var request = CreateNodeRequest(method, relativePath, nodeSecret);
        request.Content = JsonContent.Create(body);
        return request;
    }

    private HttpRequestMessage CreateNodeRequest(HttpMethod method, string relativePath, string nodeSecret)
    {
        var request = new HttpRequestMessage(method, $"{_options.CentralBaseUrl!.TrimEnd('/')}{relativePath}");
        request.Headers.Add("X-VShield-Node-Id", _options.LocalAreaNodeId!);
        request.Headers.Add("X-VShield-Node-Secret", nodeSecret);
        return request;
    }
}
