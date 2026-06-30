namespace API.Services;

public class RouteRequest
{
    public double FromLat { get; set; }
    public double FromLng { get; set; }
    public double ToLat { get; set; }
    public double ToLng { get; set; }
    public int? BuildingId { get; set; }
    public long? TargetNodeId { get; set; }
}

public class RouteSegment
{
    public double DistanceMeters { get; set; }
    public double DurationSeconds { get; set; }
    public string Type { get; set; } = "Outdoor";
    public string? Instruction { get; set; }
}

public class RouteResult
{
    public string? OutdoorGeoJson { get; set; }
    public List<IndoorRouteStep>? IndoorSteps { get; set; }
    public double TotalDistanceMeters { get; set; }
    public double TotalDurationSeconds { get; set; }
    public string? TargetBuildingName { get; set; }
    public string? TargetFloorLabel { get; set; }
    public string? TargetLabel { get; set; }
}

public class IndoorRouteStep
{
    public long FromNodeId { get; set; }
    public long ToNodeId { get; set; }
    public string FromLabel { get; set; } = string.Empty;
    public string ToLabel { get; set; } = string.Empty;
    public string NodeType { get; set; } = "Corridor";
    public int? FloorId { get; set; }
    public double DistanceMeters { get; set; }
}

public interface IRoutingService
{
    Task<RouteResult> GetRouteAsync(RouteRequest request);
}
