using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace API.Models;

[Table("Notifications")]
public class Notification
{
    [Key]
    public long Id { get; set; }

    public int RecipientUserId { get; set; }

    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [MaxLength(2000)]
    public string? Body { get; set; }

    [MaxLength(50)]
    public string Category { get; set; } = "System";

    [MaxLength(20)]
    public string? Severity { get; set; }

    [MaxLength(50)]
    public string? ReferenceType { get; set; }

    public string? ReferenceId { get; set; }

    [MaxLength(500)]
    public string? ActionUrl { get; set; }

    public decimal? Latitude { get; set; }

    public decimal? Longitude { get; set; }

    [MaxLength(200)]
    public string? LocationLabel { get; set; }

    public bool IsRead { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? ReadAt { get; set; }

    [ForeignKey(nameof(RecipientUserId))]
    public AppUser RecipientUser { get; set; } = null!;
}

[Table("NotificationRules")]
public class NotificationRule
{
    [Key]
    public int Id { get; set; }

    [MaxLength(100)]
    public string EventType { get; set; } = string.Empty;

    [MaxLength(40)]
    public string? SeverityMin { get; set; }

    public int? RecipientUserId { get; set; }

    [MaxLength(40)]
    public string? RecipientRole { get; set; }

    public bool NotifyWeb { get; set; } = true;

    public bool NotifyMobile { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [ForeignKey(nameof(RecipientUserId))]
    public AppUser? RecipientUser { get; set; }
}
