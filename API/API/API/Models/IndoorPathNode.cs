using System.ComponentModel.DataAnnotations;

namespace API.Models;

public class IndoorPathNode
{
    public long Id { get; set; }

    public int BuildingId { get; set; }

    public int? FacilityFloorId { get; set; }

    [MaxLength(160)]
    public string Label { get; set; } = string.Empty;

    [MaxLength(40)]
    public string NodeType { get; set; } = "Corridor";

    public decimal X { get; set; }

    public decimal Y { get; set; }

    public decimal Z { get; set; }

    public bool IsEmergencyExit { get; set; }

    public bool IsAccessible { get; set; } = true;

    [MaxLength(8000)]
    public string? NeighborsJson { get; set; }

    public Building? Building { get; set; }

    public FacilityFloor? FacilityFloor { get; set; }
}
