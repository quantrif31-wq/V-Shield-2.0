using System.ComponentModel.DataAnnotations;

namespace API.Models;

public static class EmployeeFaceCredentialBindingStatuses
{
    public const string Pending = "Pending";
    public const string Active = "Active";
    public const string Revoked = "Revoked";

    public static readonly IReadOnlySet<string> Supported =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            Pending, Active, Revoked
        };
}

public sealed class EmployeeFaceCredentialBinding
{
    public long Id { get; set; }
    public int EmployeeId { get; set; }
    public long AccessCredentialId { get; set; }
    [MaxLength(20)] public string Status { get; set; } = EmployeeFaceCredentialBindingStatuses.Pending;
    public DateTime? ActivatedAtUtc { get; set; }
    public DateTime? RevokedAtUtc { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public int? CreatedByUserId { get; set; }
    public int? RevokedByUserId { get; set; }
    [MaxLength(500)] public string? Reason { get; set; }
    [Timestamp] public byte[] RowVersion { get; set; } = [];

    public Employee? Employee { get; set; }
    public AccessCredential? AccessCredential { get; set; }
    public AppUser? CreatedByUser { get; set; }
    public AppUser? RevokedByUser { get; set; }
}
