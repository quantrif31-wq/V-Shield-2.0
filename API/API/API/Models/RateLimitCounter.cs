using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace API.Models;

[Table("RateLimitCounters")]
public class RateLimitCounter
{
    [Key]
    public long RateLimitCounterId { get; set; }

    [MaxLength(200)]
    public string CounterKey { get; set; } = string.Empty;

    public DateTime WindowStart { get; set; }

    public int Count { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
