using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.DataProtection;

namespace API.Services;

public sealed class TotpService
{
    private const int SecretByteLength = 20;
    private const int CodeDigits = 6;
    private static readonly TimeSpan Step = TimeSpan.FromSeconds(30);
    private readonly IDataProtector _protector;

    public TotpService(IDataProtectionProvider dataProtectionProvider)
    {
        _protector = dataProtectionProvider.CreateProtector("VShield.Auth.TotpSecret.v1");
    }

    public string GenerateSecret()
    {
        var bytes = RandomNumberGenerator.GetBytes(SecretByteLength);
        return Base32Encode(bytes);
    }

    public string ProtectSecret(string secret) => _protector.Protect(secret);

    public string UnprotectSecret(string protectedSecret) => _protector.Unprotect(protectedSecret);

    public bool VerifyCode(string protectedSecret, string? code)
    {
        if (string.IsNullOrWhiteSpace(code))
            return false;

        var normalizedCode = new string(code.Where(char.IsDigit).ToArray());
        if (normalizedCode.Length != CodeDigits)
            return false;

        var secret = UnprotectSecret(protectedSecret);
        var secretBytes = Base32Decode(secret);
        var now = DateTimeOffset.UtcNow;

        for (var offset = -1; offset <= 1; offset++)
        {
            var timestamp = now.AddSeconds(offset * Step.TotalSeconds);
            var expected = GenerateCode(secretBytes, timestamp);
            if (CryptographicOperations.FixedTimeEquals(
                    Encoding.ASCII.GetBytes(expected),
                    Encoding.ASCII.GetBytes(normalizedCode)))
            {
                return true;
            }
        }

        return false;
    }

    public string BuildOtpAuthUri(string issuer, string username, string secret)
    {
        var encodedIssuer = Uri.EscapeDataString(issuer);
        var encodedUser = Uri.EscapeDataString(username);
        return $"otpauth://totp/{encodedIssuer}:{encodedUser}?secret={secret}&issuer={encodedIssuer}&digits={CodeDigits}&period={(int)Step.TotalSeconds}";
    }

    private static string GenerateCode(byte[] secret, DateTimeOffset timestamp)
    {
        var counter = (long)Math.Floor(timestamp.ToUnixTimeSeconds() / Step.TotalSeconds);
        var counterBytes = BitConverter.GetBytes(counter);
        if (BitConverter.IsLittleEndian)
            Array.Reverse(counterBytes);

        using var hmac = new HMACSHA1(secret);
        var hash = hmac.ComputeHash(counterBytes);
        var offset = hash[^1] & 0x0F;
        var binary =
            ((hash[offset] & 0x7F) << 24) |
            ((hash[offset + 1] & 0xFF) << 16) |
            ((hash[offset + 2] & 0xFF) << 8) |
            (hash[offset + 3] & 0xFF);

        var otp = binary % (int)Math.Pow(10, CodeDigits);
        return otp.ToString(new string('0', CodeDigits), CultureInfo.InvariantCulture);
    }

    private static string Base32Encode(byte[] data)
    {
        const string alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";
        var output = new StringBuilder((int)Math.Ceiling(data.Length / 5d) * 8);
        var buffer = 0;
        var bitsLeft = 0;

        foreach (var value in data)
        {
            buffer = (buffer << 8) | value;
            bitsLeft += 8;

            while (bitsLeft >= 5)
            {
                output.Append(alphabet[(buffer >> (bitsLeft - 5)) & 31]);
                bitsLeft -= 5;
            }
        }

        if (bitsLeft > 0)
            output.Append(alphabet[(buffer << (5 - bitsLeft)) & 31]);

        return output.ToString();
    }

    private static byte[] Base32Decode(string input)
    {
        const string alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";
        var sanitized = input.Trim().Replace(" ", string.Empty).TrimEnd('=').ToUpperInvariant();
        var output = new List<byte>();
        var buffer = 0;
        var bitsLeft = 0;

        foreach (var c in sanitized)
        {
            var value = alphabet.IndexOf(c);
            if (value < 0)
                throw new FormatException("Invalid base32 TOTP secret.");

            buffer = (buffer << 5) | value;
            bitsLeft += 5;

            if (bitsLeft >= 8)
            {
                output.Add((byte)((buffer >> (bitsLeft - 8)) & 255));
                bitsLeft -= 8;
            }
        }

        return output.ToArray();
    }
}
