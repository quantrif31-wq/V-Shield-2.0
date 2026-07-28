using System.ComponentModel.DataAnnotations;

namespace API.Models;

public static class AccessCredentialTypes
{
    public const string DynamicQr = "DynamicQr";
    public const string Card = "Card";
    public const string FaceBiometric = "FaceBiometric";

    public static readonly IReadOnlySet<string> Supported =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            DynamicQr, Card, FaceBiometric
        };

    public static string? Normalize(string? value) =>
        Supported.FirstOrDefault(x => x.Equals(value?.Trim(), StringComparison.OrdinalIgnoreCase));
}

public static class AccessCredentialStatuses
{
    public const string Pending = "Pending";
    public const string Active = "Active";
    public const string Inactive = "Inactive";
    public const string Revoked = "Revoked";

    public static readonly IReadOnlySet<string> Supported =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            Pending, Active, Inactive, Revoked
        };
}

public static class EffectiveCredentialStatuses
{
    public const string Pending = "Pending";
    public const string NotYetEffective = "NotYetEffective";
    public const string Active = "Active";
    public const string Inactive = "Inactive";
    public const string Expired = "Expired";
    public const string Revoked = "Revoked";
    public const string Invalid = "Invalid";
}

public sealed class AccessCredential
{
    public long Id { get; set; }
    public int EmployeeId { get; set; }
    [MaxLength(40)] public string CredentialType { get; set; } = string.Empty;
    [MaxLength(20)] public string Status { get; set; } = AccessCredentialStatuses.Pending;
    public DateTime? EffectiveFromUtc { get; set; }
    public DateTime? ExpiresAtUtc { get; set; }
    public DateTime? RevokedAtUtc { get; set; }
    [MaxLength(64)] public string? IdentifierHash { get; set; }
    [MaxLength(40)] public string? IdentifierHashVersion { get; set; }
    [MaxLength(80)] public string? MaskedIdentifier { get; set; }
    public int? EmployeeDynamicQrId { get; set; }
    public int? CreatedByUserId { get; set; }
    public int? RevokedByUserId { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAtUtc { get; set; }
    [MaxLength(500)] public string? Description { get; set; }
    [MaxLength(500)] public string? RevocationReason { get; set; }
    [Timestamp] public byte[] RowVersion { get; set; } = [];

    public Employee? Employee { get; set; }
    public EmployeeDynamicQr? EmployeeDynamicQr { get; set; }
    public AppUser? CreatedByUser { get; set; }
    public AppUser? RevokedByUser { get; set; }
}
