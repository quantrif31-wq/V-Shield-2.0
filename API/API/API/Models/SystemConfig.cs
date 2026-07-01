using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace API.Models;

[Table("SystemConfigs")]
public class SystemConfig
{
    [Key]
    [MaxLength(100)]
    public string Key { get; set; } = string.Empty;

    [MaxLength(500)]
    public string Value { get; set; } = string.Empty;

    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}
