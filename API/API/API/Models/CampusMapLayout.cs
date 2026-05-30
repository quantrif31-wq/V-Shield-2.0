using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace API.Models;

[Table("CampusMapLayouts")]
public class CampusMapLayout
{
    [Key]
    public int Id { get; set; }

    public int GateId { get; set; }

    [Column(TypeName = "decimal(10,2)")]
    public decimal X { get; set; }

    [Column(TypeName = "decimal(10,2)")]
    public decimal Y { get; set; }

    [Column(TypeName = "decimal(10,2)")]
    public decimal W { get; set; }

    [Column(TypeName = "decimal(10,2)")]
    public decimal H { get; set; }

    public int ZIndex { get; set; } = 1;

    [StringLength(30)]
    public string? Color { get; set; }

    [StringLength(80)]
    public string? Icon { get; set; }

    public bool IsVisible { get; set; } = true;

    public bool IsLocked { get; set; } = false;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public int? UpdatedBy { get; set; }

    [ForeignKey(nameof(GateId))]
    public virtual Gate Gate { get; set; } = null!;

    [ForeignKey(nameof(UpdatedBy))]
    public virtual AppUser? UpdatedByUser { get; set; }
}
