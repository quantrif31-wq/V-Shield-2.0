using System.Text.RegularExpressions;

namespace API.Services;

public static partial class LicensePlateHelper
{
    private static readonly Dictionary<char, char[]> ConfusableChars = new()
    {
        ['0'] = new[] { 'O', 'Q', 'D' },
        ['O'] = new[] { '0', 'Q', 'D' },
        ['1'] = new[] { 'I', 'L' },
        ['I'] = new[] { '1', 'L' },
        ['L'] = new[] { '1', 'I' },
        ['5'] = new[] { 'S' },
        ['S'] = new[] { '5' },
        ['8'] = new[] { 'B' },
        ['B'] = new[] { '8' },
        ['2'] = new[] { 'Z' },
        ['Z'] = new[] { '2' },
        ['6'] = new[] { 'G' },
        ['G'] = new[] { '6' },
    };

    public static string NormalizeForMatch(string? plate)
    {
        if (string.IsNullOrWhiteSpace(plate)) return string.Empty;
        return plate.Trim()
            .ToUpperInvariant()
            .Replace(" ", "")
            .Replace("-", "")
            .Replace(".", "")
            .Replace("_", "");
    }

    public static string NormalizeForStorage(string? plate)
    {
        if (string.IsNullOrWhiteSpace(plate)) return string.Empty;
        return plate.Trim().ToUpperInvariant();
    }

    public static int LevenshteinDistance(string a, string b)
    {
        var n = a.Length;
        var m = b.Length;
        var d = new int[n + 1, m + 1];

        for (var i = 0; i <= n; i++) d[i, 0] = i;
        for (var j = 0; j <= m; j++) d[0, j] = j;

        for (var i = 1; i <= n; i++)
        {
            for (var j = 1; j <= m; j++)
            {
                var cost = a[i - 1] == b[j - 1] ? 0 : 1;
                d[i, j] = Math.Min(
                    Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1),
                    d[i - 1, j - 1] + cost);
            }
        }

        return d[n, m];
    }

    public static double FuzzyMatchScore(string a, string b)
    {
        var normA = NormalizeForMatch(a);
        var normB = NormalizeForMatch(b);

        if (string.IsNullOrEmpty(normA) || string.IsNullOrEmpty(normB)) return 0;
        if (normA == normB) return 1.0;

        var distance = LevenshteinDistance(normA, normB);
        var maxLen = Math.Max(normA.Length, normB.Length);

        return Math.Max(0, 1.0 - (double)distance / maxLen);
    }

    public static List<string> GetConfusableVariants(string? plate)
    {
        var normalized = NormalizeForMatch(plate);
        if (string.IsNullOrEmpty(normalized)) return new();

        var variants = new HashSet<string> { normalized };
        var chars = normalized.ToCharArray();

        for (var i = 0; i < chars.Length; i++)
        {
            var original = chars[i];
            if (!ConfusableChars.TryGetValue(original, out var alternatives)) continue;

            foreach (var alt in alternatives)
            {
                chars[i] = alt;
                variants.Add(new string(chars));
            }

            chars[i] = original;
        }

        return variants.ToList();
    }

    public static bool IsVietnamesePlateFormat(string? plate)
    {
        if (string.IsNullOrWhiteSpace(plate)) return false;

        var cleaned = NormalizeForMatch(plate);
        if (cleaned.Length < 7 || cleaned.Length > 9) return false;

        return VietnamesePlateRegex().IsMatch(cleaned);
    }

    [GeneratedRegex(@"^\d{2}[A-Z]{1,2}\d{4,5}$")]
    private static partial Regex VietnamesePlateRegex();
}
