using System.Text.Json;
using API.Data;
using API.Models;
using Microsoft.EntityFrameworkCore;

namespace API.Services;

public class RoutingService : IRoutingService
{
    private readonly ApplicationDbContext _db;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<RoutingService> _logger;

    private const string OSRM_BASE = "https://router.project-osrm.org";

    public RoutingService(
        ApplicationDbContext db,
        IHttpClientFactory httpClientFactory,
        ILogger<RoutingService> logger)
    {
        _db = db;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task<RouteResult> GetRouteAsync(RouteRequest request)
    {
        var result = new RouteResult();
        var outdoorTask = GetOutdoorRouteAsync(request.FromLat, request.FromLng, request.ToLat, request.ToLng);

        IndoorRouteResult? indoor = null;
        if (request.BuildingId.HasValue)
        {
            indoor = await GetIndoorRouteAsync(request.BuildingId.Value, request.TargetNodeId);
        }

        var outdoor = await outdoorTask;

        if (outdoor != null)
        {
            result.OutdoorGeoJson = outdoor.GeoJson;
            result.TotalDistanceMeters += outdoor.Distance;
            result.TotalDurationSeconds += outdoor.Duration;
        }

        if (indoor != null)
        {
            result.IndoorSteps = indoor.Steps;
            result.TotalDistanceMeters += indoor.TotalDistance;
            result.TotalDurationSeconds += indoor.TotalDuration;
            result.TargetFloorLabel = indoor.TargetFloor;
            result.TargetLabel = indoor.TargetLabel;

            if (request.BuildingId.HasValue)
            {
                var building = await _db.Buildings.FindAsync(request.BuildingId.Value);
                result.TargetBuildingName = building?.Name;
            }
        }

        return result;
    }

    private async Task<OutdoorRouteResult?> GetOutdoorRouteAsync(double fromLat, double fromLng, double toLat, double toLng)
    {
        try
        {
            var client = _httpClientFactory.CreateClient("OSRM");
            var url = $"{OSRM_BASE}/route/v1/driving/{fromLng},{fromLat};{toLng},{toLat}?overview=full&geometries=geojson&steps=true";
            var response = await client.GetAsync(url);
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            if (root.GetProperty("code").GetString() != "Ok") return null;

            var route = root.GetProperty("routes")[0];
            var geometry = route.GetProperty("geometry");
            var geoJson = geometry.GetRawText();

            var distance = route.GetProperty("distance").GetDouble();
            var duration = route.GetProperty("duration").GetDouble();

            return new OutdoorRouteResult { GeoJson = geoJson, Distance = distance, Duration = duration };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "OSRM routing failed for {from},{to}", $"{fromLat},{fromLng}", $"{toLat},{toLng}");
            return null;
        }
    }

    private async Task<IndoorRouteResult?> GetIndoorRouteAsync(int buildingId, long? targetNodeId)
    {
        var nodes = await _db.IndoorPathNodes
            .Include(n => n.FacilityFloor)
            .Where(n => n.BuildingId == buildingId)
            .OrderBy(n => n.Id)
            .ToListAsync();

        if (nodes.Count == 0) return null;

        long? startNodeId = null;
        // Find entrance node as default start
        var entrance = nodes.FirstOrDefault(n => n.NodeType == "Entrance");
        if (entrance != null) startNodeId = entrance.Id;

        if (startNodeId == null) return null;

        var endId = targetNodeId ?? nodes.LastOrDefault()?.Id;
        if (endId == null || endId == startNodeId) return null;

        var path = AStarPathfinding(nodes, startNodeId.Value, endId.Value);
        if (path == null || path.Count < 2) return null;

        var steps = new List<IndoorRouteStep>();
        for (var i = 0; i < path.Count - 1; i++)
        {
            var from = path[i];
            var to = path[i + 1];
            var dx = (double)(to.X - from.X);
            var dy = (double)(to.Y - from.Y);
            var dz = (double)(to.Z - from.Z);
            var dist = Math.Sqrt(dx * dx + dy * dy + dz * dz);

            steps.Add(new IndoorRouteStep
            {
                FromNodeId = from.Id,
                ToNodeId = to.Id,
                FromLabel = from.Label,
                ToLabel = to.Label,
                NodeType = to.NodeType,
                FloorId = to.FacilityFloorId,
                DistanceMeters = Math.Round(dist, 1)
            });
        }

        var lastNode = path[^1];
        var targetFloor = lastNode.FacilityFloor != null ? lastNode.FacilityFloor.Name : null;

        return new IndoorRouteResult
        {
            Steps = steps,
            TotalDistance = steps.Sum(s => s.DistanceMeters),
            TotalDuration = steps.Sum(s => s.DistanceMeters / 1.2),
            TargetFloor = targetFloor,
            TargetLabel = lastNode.Label
        };
    }

    private static List<IndoorPathNode>? AStarPathfinding(List<IndoorPathNode> nodes, long startId, long endId)
    {
        var nodeMap = nodes.ToDictionary(n => n.Id);
        if (!nodeMap.ContainsKey(startId) || !nodeMap.ContainsKey(endId)) return null;

        var start = nodeMap[startId];
        var end = nodeMap[endId];

        var openSet = new HashSet<long> { startId };
        var cameFrom = new Dictionary<long, long>();
        var gScore = new Dictionary<long, double> { [startId] = 0 };
        var fScore = new Dictionary<long, double> { [startId] = Heuristic(start, end) };

        while (openSet.Count > 0)
        {
            var currentId = openSet.OrderBy(id => fScore.GetValueOrDefault(id, double.MaxValue)).First();
            if (currentId == endId)
                return ReconstructPath(cameFrom, currentId, nodeMap);

            openSet.Remove(currentId);

            var current = nodeMap[currentId];
            var neighbors = ParseNeighbors(current.NeighborsJson);

            foreach (var (neighborId, weight) in neighbors)
            {
                if (!nodeMap.ContainsKey(neighborId)) continue;
                var tentativeG = gScore[currentId] + weight;

                if (tentativeG < gScore.GetValueOrDefault(neighborId, double.MaxValue))
                {
                    cameFrom[neighborId] = currentId;
                    gScore[neighborId] = tentativeG;
                    fScore[neighborId] = tentativeG + Heuristic(nodeMap[neighborId], end);
                    openSet.Add(neighborId);
                }
            }
        }

        return null;
    }

    private static double Heuristic(IndoorPathNode a, IndoorPathNode b)
    {
        var dx = (double)(a.X - b.X);
        var dy = (double)(a.Y - b.Y);
        var dz = (double)(a.Z - b.Z);
        return Math.Sqrt(dx * dx + dy * dy + dz * dz);
    }

    private static List<(long nodeId, double weight)> ParseNeighbors(string? neighborsJson)
    {
        if (string.IsNullOrWhiteSpace(neighborsJson)) return new();
        try
        {
            var arr = JsonSerializer.Deserialize<List<NeighborEntry>>(neighborsJson);
            return arr?.Select(a => (a.NodeId, a.Weight)).ToList() ?? new();
        }
        catch { return new(); }
    }

    private static List<IndoorPathNode> ReconstructPath(Dictionary<long, long> cameFrom, long currentId, Dictionary<long, IndoorPathNode> nodeMap)
    {
        var path = new List<IndoorPathNode> { nodeMap[currentId] };
        while (cameFrom.ContainsKey(currentId))
        {
            currentId = cameFrom[currentId];
            path.Add(nodeMap[currentId]);
        }
        path.Reverse();
        return path;
    }

    private class NeighborEntry { public long NodeId { get; set; } public double Weight { get; set; } }
    private class OutdoorRouteResult { public string? GeoJson { get; set; } public double Distance { get; set; } public double Duration { get; set; } }
    private class IndoorRouteResult { public List<IndoorRouteStep> Steps { get; set; } = new(); public double TotalDistance { get; set; } public double TotalDuration { get; set; } public string? TargetFloor { get; set; } public string? TargetLabel { get; set; } }
}
