using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace API.Models;

[Table("Campus3DObjects")]
public class Campus3DObject
{
    [Key]
    public int Id { get; set; }

    public int SiteId { get; set; }

    [MaxLength(40)]
    public string ObjectType { get; set; } = "Building";

    [MaxLength(160)]
    public string Label { get; set; } = string.Empty;

    [Column(TypeName = "decimal(10,2)")]
    public decimal PositionX { get; set; }

    [Column(TypeName = "decimal(10,2)")]
    public decimal PositionZ { get; set; }

    [Column(TypeName = "decimal(10,2)")]
    public decimal PositionY { get; set; }

    [Column(TypeName = "decimal(10,2)")]
    public decimal Width { get; set; }

    [Column(TypeName = "decimal(10,2)")]
    public decimal Length { get; set; }

    [Column(TypeName = "decimal(10,2)")]
    public decimal Height { get; set; }

    public int? Floors { get; set; }

    [Column(TypeName = "decimal(6,2)")]
    public decimal Rotation { get; set; }

    [MaxLength(30)]
    public string? Color { get; set; }

    public string? PropertiesJson { get; set; }

    public bool IsActive { get; set; } = true;

    [ForeignKey(nameof(SiteId))]
    public Site? Site { get; set; }
}
