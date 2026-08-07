using System.Security.Cryptography;
using System.Text;

namespace API.Services;

public sealed class StaticVisitorQrService
{
    public const int DefaultTimeStepSeconds = 30;

    public string GenerateSecret(int length = 20)
    {
        var bytes = new byte[length];
        RandomNumberGenerator.Fill(bytes);
        return Base32Encode(bytes);
    }

    public long GetCurrentCounter(DateTime utcNow, int timeStepSeconds = DefaultTimeStepSeconds)
    {
        var unixTime = new DateTimeOffset(utcNow).ToUnixTimeSeconds();
        return unixTime / timeStepSeconds;
    }

    public string GenerateOtp(string base32Secret, long counter, int digits = 6)
    {
        var key = Base32Decode(base32Secret);
        var counterBytes = BitConverter.GetBytes(counter);
        if (BitConverter.IsLittleEndian)
        {
            Array.Reverse(counterBytes);
        }

        using var hmac = new HMACSHA1(key);
        var hash = hmac.ComputeHash(counterBytes);

        var offset = hash[^1] & 0x0F;

        int binaryCode =
            ((hash[offset] & 0x7F) << 24) |
            ((hash[offset + 1] & 0xFF) << 16) |
            ((hash[offset + 2] & 0xFF) << 8) |
            (hash[offset + 3] & 0xFF);

        int otp = binaryCode % (int)Math.Pow(10, digits);
        return otp.ToString().PadLeft(digits, '0');
    }

    public string BuildPayload(int visitorId, int registrationId, long counter, string otp)
    {
        return $"VIS:{visitorId}|REG:{registrationId}|TS:{counter}|OTP:{otp}";
    }

    public bool TryParsePayload(string payload, out VisitorQrParsedPayload? result, out string message)
    {
        result = null;
        message = "OK";

        try
        {
            if (string.IsNullOrWhiteSpace(payload))
            {
                message = "QR payload không được để trống.";
                return false;
            }

            var parts = payload.Split('|', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length != 4)
            {
                message = "QR payload không đúng định dạng.";
                return false;
            }

            var visPart = parts[0].Split(':', 2);
            var regPart = parts[1].Split(':', 2);
            var tsPart = parts[2].Split(':', 2);
            var otpPart = parts[3].Split(':', 2);

            if (visPart.Length != 2 || regPart.Length != 2 || tsPart.Length != 2 || otpPart.Length != 2)
            {
                message = "QR payload không đúng định dạng.";
                return false;
            }

            if (!visPart[0].Equals("VIS", StringComparison.OrdinalIgnoreCase))
            {
                message = "Thiếu VIS trong payload.";
                return false;
            }

            if (!regPart[0].Equals("REG", StringComparison.OrdinalIgnoreCase))
            {
                message = "Thiếu REG trong payload.";
                return false;
            }

            if (!tsPart[0].Equals("TS", StringComparison.OrdinalIgnoreCase))
            {
                message = "Thiếu TS trong payload.";
                return false;
            }

            if (!otpPart[0].Equals("OTP", StringComparison.OrdinalIgnoreCase))
            {
                message = "Thiếu OTP trong payload.";
                return false;
            }

            if (!int.TryParse(visPart[1], out var visitorId))
            {
                message = "VisitorId không hợp lệ.";
                return false;
            }

            if (!int.TryParse(regPart[1], out var registrationId))
            {
                message = "RegistrationId không hợp lệ.";
                return false;
            }

            if (!long.TryParse(tsPart[1], out var counter))
            {
                message = "TS không hợp lệ.";
                return false;
            }

            var otp = otpPart[1]?.Trim();
            if (string.IsNullOrWhiteSpace(otp))
            {
                message = "OTP không hợp lệ.";
                return false;
            }

            result = new VisitorQrParsedPayload(visitorId, registrationId, counter, otp);
            return true;
        }
        catch
        {
            message = "Không thể phân tích QR payload.";
            return false;
        }
    }

    public bool FixedTimeEquals(string left, string right)
    {
        if (left == null || right == null)
            return false;

        var leftBytes = Encoding.UTF8.GetBytes(left);
        var rightBytes = Encoding.UTF8.GetBytes(right);

        return leftBytes.Length == rightBytes.Length &&
               CryptographicOperations.FixedTimeEquals(leftBytes, rightBytes);
    }

    private static string Base32Encode(byte[] data)
    {
        const string alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";

        if (data == null || data.Length == 0)
            return string.Empty;

        var output = new StringBuilder();
        int buffer = data[0];
        int next = 1;
        int bitsLeft = 8;

        while (bitsLeft > 0 || next < data.Length)
        {
            if (bitsLeft < 5)
            {
                if (next < data.Length)
                {
                    buffer <<= 8;
                    buffer |= data[next++] & 0xFF;
                    bitsLeft += 8;
                }
                else
                {
                    int pad = 5 - bitsLeft;
                    buffer <<= pad;
                    bitsLeft += pad;
                }
            }

            int index = 0x1F & (buffer >> (bitsLeft - 5));
            bitsLeft -= 5;
            output.Append(alphabet[index]);
        }

        return output.ToString();
    }

    private static byte[] Base32Decode(string input)
    {
        const string alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";

        input = input.Trim().TrimEnd('=').ToUpperInvariant();

        var output = new List<byte>();
        int bitBuffer = 0;
        int bitsLeft = 0;

        foreach (char c in input)
        {
            int val = alphabet.IndexOf(c);
            if (val < 0)
                throw new FormatException("SecretKey Base32 không hợp lệ.");

            bitBuffer <<= 5;
            bitBuffer |= val & 0x1F;
            bitsLeft += 5;

            if (bitsLeft >= 8)
            {
                output.Add((byte)(bitBuffer >> (bitsLeft - 8)));
                bitsLeft -= 8;
            }
        }

        return output.ToArray();
    }
}

public sealed record VisitorQrParsedPayload(int VisitorId, int RegistrationId, long Counter, string Otp);
