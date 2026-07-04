namespace API.Services.Sync;

public class SyncRuntimeOptions
{
    public const string SectionName = "Sync";

    public string Mode { get; set; } = SyncRuntimeModes.Standalone;
    public string? CentralBaseUrl { get; set; }
    public string? RegistrationKey { get; set; }
    public string? LocalAreaNodeId { get; set; }
    public int? CompanyId { get; set; }
    public int? SiteId { get; set; }
    public string? DisplayName { get; set; }
    public string? Version { get; set; }
    public string? AssignedGateIds { get; set; }
    public string? AssignedLaneIds { get; set; }
    public string? AssignedZoneIds { get; set; }
    public int PushIntervalSeconds { get; set; } = 2;
    public int PullIntervalSeconds { get; set; } = 3;
    public int BatchSize { get; set; } = 50;
    public int DownstreamScanMultiplier { get; set; } = 10;
}

public static class SyncRuntimeModes
{
    public const string Standalone = "Standalone";
    public const string Central = "Central";
    public const string AreaNode = "AreaNode";
}
