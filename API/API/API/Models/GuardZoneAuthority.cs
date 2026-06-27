using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace API.Models;

[Table("GuardZoneAuthority")]
public partial class GuardZoneAuthority
{
    [Key]
    public int GuardZoneAuthorityId { get; set; }

    public int UserId { get; set; }

    public int SecurityZoneId { get; set; }

    [MaxLength(40)]
    public string AuthorityLevel { get; set; } = "Normal";

    public bool CanOverride { get; set; } = true;

    public bool CanManage { get; set; }

    public DateTime ValidFrom { get; set; }

    public DateTime ValidTo { get; set; }

    public int? GrantedByUserId { get; set; }

    [MaxLength(500)]
    public string? Note { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public AppUser? User { get; set; }
    public SecurityZone? SecurityZone { get; set; }
    public AppUser? GrantedByUser { get; set; }
}
