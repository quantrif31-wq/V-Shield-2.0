using System.Text.RegularExpressions;

namespace API.Services.AI;

public interface IAiRedactionService
{
    /// <summary>
    /// Redact PII và secrets khỏi prompt trước khi gửi đến AI provider cloud.
    /// Trả về text đã redact và danh sách các field bị redact.
    /// </summary>
    (string RedactedText, List<string> RedactedFields) Redact(string text);

    /// <summary>
    /// Kiểm tra xem text có chứa thông tin nhạy cảm không.
    /// </summary>
    bool ContainsSensitiveData(string text);
}

public class AiRedactionService : IAiRedactionService
{
    private static readonly Regex[] Patterns =
    {
        new(@"\b\d{12,19}\b", RegexOptions.Compiled), // Số thẻ tín dụng tiềm năng
        new(@"\b\d{9}\b", RegexOptions.Compiled), // SSN tiềm năng
        new(@"[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Za-z]{2,}", RegexOptions.Compiled), // Email
        new(@"\b\d{3}[-.]?\d{3}[-.]?\d{4}\b", RegexOptions.Compiled), // SĐT Mỹ
        new(@"(?i)(password|secret|token|apikey|api_key|private_key)\s*[:=]\s*""?""[^\s""]+", RegexOptions.Compiled), // Secrets
    };

    private static readonly string[] SensitiveFieldPatterns =
    {
        "mfa_secret", "totp_secret", "password_hash", "refresh_token",
        "face_image", "video_frame", "plate_image",
        "secret_key", "api_key", "private_key", "access_token"
    };

    public (string RedactedText, List<string> RedactedFields) Redact(string text)
    {
        var redacted = text;
        var redactedFields = new List<string>();

        // Redact các pattern chung
        redacted = Patterns.Aggregate(redacted, (current, pattern) =>
            pattern.Replace(current, match =>
            {
                redactedFields.Add($"pattern:{pattern.ToString().Substring(0, Math.Min(20, pattern.ToString().Length))}");
                return "[REDACTED]";
            }));

        // Redact các field nhạy cảm trong JSON
        foreach (var field in SensitiveFieldPatterns)
        {
            var fieldPattern = new Regex(
                $@"""{field}""\s*:\s*""[^""]+""",
                RegexOptions.IgnoreCase | RegexOptions.Compiled);

            redacted = fieldPattern.Replace(redacted, match =>
            {
                redactedFields.Add($"field:{field}");
                return $@"""{field}"":""[REDACTED]""";
            });
        }

        return (redacted, redactedFields.Distinct().ToList());
    }

    public bool ContainsSensitiveData(string text)
    {
        if (Patterns.Any(p => p.IsMatch(text)))
            return true;

        return SensitiveFieldPatterns.Any(f =>
            text.Contains(f, StringComparison.OrdinalIgnoreCase));
    }
}
