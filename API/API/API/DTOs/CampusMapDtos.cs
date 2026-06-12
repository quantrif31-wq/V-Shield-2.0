using System.ComponentModel.DataAnnotations;

namespace API.DTOs;

public class CampusMapLayoutBatchUpsertRequest
{
    [Required]
    [MinLength(1)]
    public List<CampusMapLayoutUpsertItemRequest> Items { get; set; } = new();
}

public class CampusMapLayoutUpsertItemRequest
{
    [Required]
    public int GateId { get; set; }

    [Required]
    public decimal? X { get; set; }

    [Required]
    public decimal? Y { get; set; }

    [Required]
    public decimal? W { get; set; }

    [Required]
    public decimal? H { get; set; }

    public int? ZIndex { get; set; }

    [MaxLength(30)]
    public string? Color { get; set; }

    [MaxLength(80)]
    public string? Icon { get; set; }

    public bool? IsVisible { get; set; }

    public bool? IsLocked { get; set; }
}

public class CampusMapLayoutPatchRequest
{
    public decimal? X { get; set; }
    public decimal? Y { get; set; }
    public decimal? W { get; set; }
    public decimal? H { get; set; }
    public int? ZIndex { get; set; }

    [MaxLength(30)]
    public string? Color { get; set; }

    [MaxLength(80)]
    public string? Icon { get; set; }

    public bool? IsVisible { get; set; }
    public bool? IsLocked { get; set; }
}
