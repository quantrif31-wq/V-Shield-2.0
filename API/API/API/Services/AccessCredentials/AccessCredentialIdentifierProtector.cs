using System.Security.Cryptography;
using System.Text;
using API.Models;

namespace API.Services.AccessCredentials;

public sealed class AccessCredentialOptions
{
    public const string SectionName = "AccessCredentials";
    public string? IdentifierHmacKey { get; set; }
}

public interface IAccessCredentialIdentifierProtector
{
    (string Hash, string Mask) Protect(string credentialType, string identifier);
}

public sealed class AccessCredentialIdentifierProtector(AccessCredentialOptions options)
    : IAccessCredentialIdentifierProtector
{
    public (string Hash, string Mask) Protect(string credentialType, string identifier)
    {
        if (string.IsNullOrWhiteSpace(options.IdentifierHmacKey) ||
            options.IdentifierHmacKey.Length < 32)
            throw new InvalidOperationException("Access credential identifier protection is not configured.");
        var normalizedType = AccessCredentialTypes.Normalize(credentialType)
            ?? throw new ArgumentException("Unsupported credential type.");
        var normalized = identifier.Trim().ToUpperInvariant();
        if (normalized.Length == 0) throw new ArgumentException("Identifier is required.");
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(options.IdentifierHmacKey));
        var hash = Convert.ToHexString(hmac.ComputeHash(
            Encoding.UTF8.GetBytes($"{normalizedType}\n{normalized}"))).ToLowerInvariant();
        var visible = normalized.Length <= 4 ? normalized[^1..] : normalized[^4..];
        return (hash, $"{new string('*', Math.Max(3, normalized.Length - visible.Length))}{visible}");
    }
}
