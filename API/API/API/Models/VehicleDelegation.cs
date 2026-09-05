using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace API.Models;

[Table("VehicleDelegations")]
public class VehicleDelegation
{
    [Key]
    public int VehicleDelegationId { get; set; }

    public int VehicleId { get; set; }

    public int FromEmployeeId { get; set; }

    public int ToEmployeeId { get; set; }

    // Người tạo yêu cầu. Null is retained for requests created before the
    // ownership-request workflow was introduced; those are owner proposals.
    public int? RequestedByEmployeeId { get; set; }

    [MaxLength(500)]
    public string? Reason { get; set; }

    [MaxLength(20)]
    public string Status { get; set; } = DelegationStatuses.Pending;

    public DateTime RequestedAtUtc { get; set; } = DateTime.UtcNow;

    public DateTime? RespondedAtUtc { get; set; }

    [ForeignKey(nameof(VehicleId))]
    public virtual Vehicle Vehicle { get; set; } = null!;

    [ForeignKey(nameof(FromEmployeeId))]
    public virtual Employee FromEmployee { get; set; } = null!;

    [ForeignKey(nameof(ToEmployeeId))]
    public virtual Employee ToEmployee { get; set; } = null!;
}

public static class DelegationStatuses
{
    public const string Pending = "Pending";
    public const string Approved = "Approved";
    public const string Rejected = "Rejected";
    public const string Revoked = "Revoked";
}
