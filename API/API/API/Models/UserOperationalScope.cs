using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace API.Models;

[Table("UserOperationalScopes")]
public class UserOperationalScope
{
    [Key]
    public int UserOperationalScopeId { get; set; }

    public int UserId { get; set; }

    [MaxLength(64)]
    public string TaskKey { get; set; } = string.Empty;

    public int? SiteId { get; set; }

    public int? GateId { get; set; }

    public int? LaneId { get; set; }

    public int? SecurityZoneId { get; set; }

    public bool CanView { get; set; } = true;

    public bool CanManage { get; set; } = true;

    public DateTime ValidFromUtc { get; set; } = DateTime.UtcNow;

    public DateTime? ValidToUtc { get; set; }

    [MaxLength(500)]
    public string? Note { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public int? CreatedByUserId { get; set; }

    public AppUser? User { get; set; }
    public AppUser? CreatedByUser { get; set; }
    public Site? Site { get; set; }
    public Gate? Gate { get; set; }
    public Lane? Lane { get; set; }
    public SecurityZone? SecurityZone { get; set; }
}
