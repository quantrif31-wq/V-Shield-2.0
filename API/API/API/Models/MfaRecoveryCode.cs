using System.ComponentModel.DataAnnotations;

namespace API.Models;

public class MfaRecoveryCode
{
    public long MfaRecoveryCodeId { get; set; }
    public int UserId { get; set; }
    [MaxLength(128)] public string CodeHash { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime ExpiresAtUtc { get; set; } = DateTime.UtcNow.AddDays(180);
    public DateTime? UsedAtUtc { get; set; }
    public int? CreatedByUserId { get; set; }
    public AppUser? User { get; set; }
}
