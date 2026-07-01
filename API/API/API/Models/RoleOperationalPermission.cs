using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace API.Models;

[Table("RoleOperationalPermissions")]
public class RoleOperationalPermission
{
    [Key]
    public int RoleOperationalPermissionId { get; set; }

    [MaxLength(32)]
    public string Role { get; set; } = string.Empty;

    [MaxLength(64)]
    public string TaskKey { get; set; } = string.Empty;

    public bool IsAllowed { get; set; }

    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;

    public int? UpdatedByUserId { get; set; }

    public AppUser? UpdatedByUser { get; set; }
}
