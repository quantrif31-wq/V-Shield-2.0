using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace API.Models;

[Table("UserRefreshTokens")]
public class UserRefreshToken
{
    [Key]
    public long Id { get; set; }

    public int UserId { get; set; }

    [Required]
    [MaxLength(128)]
    public string TokenHash { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string JwtId { get; set; } = string.Empty;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public DateTime ExpiresAtUtc { get; set; }

    public DateTime? RevokedAtUtc { get; set; }

    [MaxLength(128)]
    public string? ReplacedByTokenHash { get; set; }

    [MaxLength(200)]
    public string? RevocationReason { get; set; }

    [MaxLength(80)]
    public string? CreatedByIp { get; set; }

    public bool IsActive => RevokedAtUtc == null && DateTime.UtcNow < ExpiresAtUtc;

    [ForeignKey(nameof(UserId))]
    public virtual AppUser User { get; set; } = null!;
}
