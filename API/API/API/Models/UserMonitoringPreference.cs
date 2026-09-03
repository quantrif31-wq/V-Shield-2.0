using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace API.Models;

[Table("UserMonitoringPreferences")]
public sealed class UserMonitoringPreference
{
    [Key]
    public int UserId { get; set; }

    [Required]
    [MaxLength(256)]
    public string SelectedCameraIdsJson { get; set; } = "[]";

    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;

    [ForeignKey(nameof(UserId))]
    public AppUser? User { get; set; }
}
