namespace API.Services.Sync;

public record SyncRegistrationRequest(
    string AreaNodeId,
    int? CompanyId,
    int? SiteId,
    string DisplayName,
    string? Version,
    IReadOnlyList<int> GateIds,
    IReadOnlyList<int> LaneIds,
    IReadOnlyList<int> ZoneIds);

public record SyncRegistrationResponse(
    string AreaNodeId,
    string NodeSecret,
    int? CompanyId,
    int? SiteId,
    IReadOnlyList<SyncScopeItemDto> Assignments);

public record SyncScopeItemDto(string ScopeType, int ScopeId);

public record SyncEventDto(
    string EventType,
    string AggregateType,
    string? AggregateId,
    string CorrelationId,
    string PayloadJson,
    int? CompanyId,
    int? SiteId,
    string? AreaNodeId,
    string? ScopeType,
    int? ScopeId,
    string SourceSystem,
    int SchemaVersion,
    DateTime OccurredAtUtc);

public record SyncBatchRequest(
    string BatchId,
    string AreaNodeId,
    int? CompanyId,
    int? SiteId,
    DateTime SentAtUtc,
    IReadOnlyList<SyncEventDto> Events);

public record SyncEventAckDto(string CorrelationId, bool Accepted, string? AppliedAggregateId, string? Error);

public record SyncBatchResponse(
    int AcceptedCount,
    int RejectedCount,
    IReadOnlyList<SyncEventAckDto> Results);

public record SyncDownstreamFeedResponse(
    long FromSequence,
    long ToSequence,
    IReadOnlyList<SyncOutboxEnvelopeDto> Events);

public record SyncOutboxEnvelopeDto(
    long OutboxEventId,
    SyncEventDto Event);

public record SyncAckRequest(long LastAcknowledgedOutboxEventId);

public record SyncBootstrapResponse(
    long RecommendedSequence,
    IReadOnlyList<SyncOutboxEnvelopeDto> Events);
