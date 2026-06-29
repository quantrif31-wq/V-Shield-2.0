using Xunit;

namespace API.Tests.LoadTesting;

internal static class LoadTestEnvironment
{
    public const string EnableLoadTestsVariable = "ENABLE_LOAD_TESTS";
    public const string BaseUrlVariable = "LOAD_TEST_URL";
    public const string AdminTokenVariable = "LOAD_TEST_ADMIN_TOKEN";
    public const string DurationSecondsVariable = "LOAD_TEST_DURATION_SECONDS";
    public const string ConcurrencyVariable = "LOAD_TEST_CONCURRENCY";
    public const string WarmUpSecondsVariable = "LOAD_TEST_WARMUP_SECONDS";
    public const string RefreshTokenVariable = "LOAD_TEST_REFRESH_TOKEN";

    public static bool IsEnabled() =>
        string.Equals(
            Environment.GetEnvironmentVariable(EnableLoadTestsVariable),
            "true",
            StringComparison.OrdinalIgnoreCase);

    public static LoadTestConfiguration CreateConfiguration(
        int defaultDurationSeconds,
        int defaultConcurrency)
    {
        return new LoadTestConfiguration
        {
            BaseUrl = Environment.GetEnvironmentVariable(BaseUrlVariable) ?? "http://localhost:5107",
            AuthToken = Environment.GetEnvironmentVariable(AdminTokenVariable) ?? string.Empty,
            DefaultDurationSeconds = GetInt(DurationSecondsVariable, defaultDurationSeconds),
            DefaultConcurrency = GetInt(ConcurrencyVariable, defaultConcurrency),
            DefaultWarmUpSeconds = GetInt(WarmUpSecondsVariable, 3)
        };
    }

    private static int GetInt(string variableName, int fallback)
    {
        var rawValue = Environment.GetEnvironmentVariable(variableName);
        return int.TryParse(rawValue, out var parsed) && parsed > 0
            ? parsed
            : fallback;
    }
}

[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public sealed class LoadTestFactAttribute : FactAttribute
{
    public LoadTestFactAttribute(string? additionalRequirement = null, params string[] requiredEnvironmentVariables)
    {
        var reasons = new List<string>();

        if (!LoadTestEnvironment.IsEnabled())
        {
            reasons.Add(
                $"Set {LoadTestEnvironment.EnableLoadTestsVariable}=true to opt into external load tests.");
        }

        var missingVariables = requiredEnvironmentVariables
            .Where(variableName => string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(variableName)))
            .ToArray();

        if (missingVariables.Length > 0)
        {
            reasons.Add($"Missing environment variables: {string.Join(", ", missingVariables)}.");
        }

        if (reasons.Count > 0)
        {
            if (!string.IsNullOrWhiteSpace(additionalRequirement))
            {
                reasons.Add(additionalRequirement);
            }

            Skip = string.Join(" ", reasons);
        }
    }
}
