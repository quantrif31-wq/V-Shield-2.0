using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace API.Models;

[Table("SystemAuditLogs")]
public class SystemAuditLog
{
    [Key]
    public long Id { get; set; }
    public DateTime TimestampUtc { get; set; } = DateTime.UtcNow;
    public int? UserId { get; set; }
    [MaxLength(100)]
    public string? Username { get; set; }
    [MaxLength(50)]
    public string? HttpMethod { get; set; }
    [MaxLength(300)]
    public string? Path { get; set; }
    [MaxLength(20)]
    public string ActionType { get; set; } = string.Empty; // CREATE|UPDATE|DELETE|REQUEST
    [MaxLength(120)]
    public string? EntityName { get; set; }
    [MaxLength(120)]
    public string? EntityId { get; set; }
    public string? OldValuesJson { get; set; }
    public string? NewValuesJson { get; set; }
    public bool IsSuccess { get; set; }
    [MaxLength(1000)]
    public string? FailureReason { get; set; }
    public int? StatusCode { get; set; }
}

