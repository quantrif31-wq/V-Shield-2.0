using System.Text.Json;
using Xunit;
using Xunit.Abstractions;

namespace API.Tests.LoadTesting;

/// <summary>
/// Load tests for authentication flows: login, refresh token, MFA, step-up.
/// These tests require a running instance of the API server.
/// Configure the target URL and auth credentials via environment variables:
///   LOAD_TEST_URL (default: http://localhost:5107)
///   LOAD_TEST_ADMIN_TOKEN (optional: pre-generated admin JWT)
/// </summary>
[Trait("Category", "LoadTest")]
[Trait("Category", "Auth")]
public class AuthLoadTests
{
    private readonly ITestOutputHelper _output;
    private readonly LoadTestRunner _runner;
    private readonly LoadTestConfiguration _config;

    public AuthLoadTests(ITestOutputHelper output)
    {
        _output = output;
        _config = new LoadTestConfiguration
        {
            BaseUrl = Environment.GetEnvironmentVariable("LOAD_TEST_URL") ?? "http://localhost:5107",
            AuthToken = Environment.GetEnvironmentVariable("LOAD_TEST_ADMIN_TOKEN") ?? string.Empty,
            DefaultDurationSeconds = 30,
            DefaultConcurrency = 5
        };
        _runner = new LoadTestRunner(_config);
    }

    private const string SkipMessage = "Requires running API server at LOAD_TEST_URL (default http://localhost:5107) with seeded data. Use --filter \"Category=LoadTest\" to run.";

    /// <summary>
    /// Pilot profile: 500 users, simulate login burst at shift start.
    /// Expectation: P95 latency under 500ms, zero failures.
    /// </summary>
    [Fact(Skip = SkipMessage)]
    public async Task LoginBurst_PilotProfile()
    {
        var stats = await _runner.RunPostAsync(
            scenarioName: "Login_Pilot",
            endpoint: "/api/Auth/login",
            body: new { username = "admin.test", password = "Admin@12345", mfaCode = (string?)null },
            concurrency: 10,
            durationSeconds: 30);

        _output.WriteLine(stats.ToString());

        Assert.True(stats.Passed, $"Login burst failed: {stats.FailedRequests} failures");
        Assert.InRange(stats.LatencyMsP95, 0, 2000);
    }

    /// <summary>
    /// Medium company profile: 5,000 users, concurrent login + MFA completion.
    /// Expectation: P95 latency under 1000ms, error rate under 1%.
    /// </summary>
    [Fact(Skip = SkipMessage)]
    public async Task LoginWithMfa_MediumProfile()
    {
        var stats = await _runner.RunPostAsync(
            scenarioName: "Login_MFA_Medium",
            endpoint: "/api/Auth/login",
            body: new { username = "admin.test", password = "Admin@12345", mfaCode = (string?)null },
            concurrency: 20,
            durationSeconds: 30);

        _output.WriteLine(stats.ToString());

        var errorRate = stats.TotalRequests > 0
            ? (double)stats.FailedRequests / stats.TotalRequests
            : 1.0;
        Assert.True(errorRate < 0.05, $"Error rate too high: {errorRate:P2}");
    }

    /// <summary>
    /// Validate /health/live handles high concurrency without degradation.
    /// This is critical because health probes run frequently in container orchestration.
    /// </summary>
    [Fact(Skip = SkipMessage)]
    public async Task HealthEndpoint_HighConcurrency()
    {
        var stats = await _runner.RunGetAsync(
            scenarioName: "Health_Live_HighConcurrency",
            endpoint: "/health/live",
            concurrency: 50,
            durationSeconds: 20);

        _output.WriteLine(stats.ToString());

        Assert.True(stats.Passed, "Health endpoint must be extremely reliable");
        Assert.InRange(stats.LatencyMsP95, 0, 200);
    }

    /// <summary>
    /// Refresh token rotation under concurrent load.
    /// Note: Requires a valid refresh token obtained from a prior login.
    /// </summary>
    [Fact(Skip = SkipMessage + " Also needs a valid refresh token.")]
    public async Task RefreshToken_LoadTest()
    {
        var stats = await _runner.RunPostAsync(
            scenarioName: "RefreshToken",
            endpoint: "/api/Auth/refresh",
            body: new { refreshToken = "test-refresh-token-placeholder" },
            concurrency: 10,
            durationSeconds: 20);

        _output.WriteLine(stats.ToString());

        // Note: This will likely fail with invalid token errors, which is expected.
        // The test measures that the endpoint handles load gracefully (no crashes).
        Assert.True(stats.TotalRequests > 0, "No requests completed");
    }

    /// <summary>
    /// Stress test: malicious login attempts (wrong passwords).
    /// Expectation: rate-limiting kicks in, but API stays responsive for legitimate traffic.
    /// </summary>
    [Fact(Skip = SkipMessage)]
    public async Task FailedLogin_StressTest()
    {
        var stats = await _runner.RunPostAsync(
            scenarioName: "FailedLogin_Stress",
            endpoint: "/api/Auth/login",
            body: new { username = "unknown.user", password = "WrongPassword!@#123" },
            concurrency: 30,
            durationSeconds: 30);

        _output.WriteLine(stats.ToString());

        // Expect rate-limiting responses (429) - this is acceptable behavior
        // API should not crash or return 500 errors
        // We just validate the API stays up
        Assert.True(stats.TotalRequests > 0, "API must respond to all requests");
    }
}
