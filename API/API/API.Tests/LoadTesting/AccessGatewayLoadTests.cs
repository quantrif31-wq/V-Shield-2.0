using Xunit;
using Xunit.Abstractions;

namespace API.Tests.LoadTesting;

/// <summary>
/// Load tests for access gateway operations: gate transit scan, QR access, barrier commands.
/// These tests hit the core access-control surfaces that gate operators use continuously.
/// </summary>
[Trait("Category", "LoadTest")]
[Trait("Category", "AccessGateway")]
public class AccessGatewayLoadTests
{
    private readonly ITestOutputHelper _output;
    private readonly LoadTestRunner _runner;
    private readonly LoadTestConfiguration _config;

    public AccessGatewayLoadTests(ITestOutputHelper output)
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

    private const string SkipMessage = "Requires running API server at LOAD_TEST_URL (default http://localhost:5107) with seeded gate/QR data. Use --filter \"Category=LoadTest\" to run.";

    /// <summary>
    /// Pilot profile: 10 gates x 1 scan/sec = 10 RPS sustained for 60s.
    /// Simulates normal workday traffic at a small site.
    /// </summary>
    [Fact(Skip = SkipMessage)]
    public async Task GateTransitScan_PilotProfile()
    {
        var stats = await _runner.RunPostAsync(
            scenarioName: "GateTransit_Pilot",
            endpoint: "/api/gate-transit/scan",
            body: new
            {
                gateId = 1,
                direction = "IN",
                scanType = "QR",
                credentialData = "test-qr-data",
                plateText = (string?)null,
                temperature = (decimal?)null
            },
            concurrency: 10,
            durationSeconds: 30);

        _output.WriteLine(stats.ToString());

        Assert.True(stats.TotalRequests > 0, "Must complete requests");
        var errorRate = stats.TotalRequests > 0
            ? (double)stats.FailedRequests / stats.TotalRequests
            : 1.0;
        Assert.True(errorRate < 0.10, $"Error rate too high: {errorRate:P2}");
    }

    /// <summary>
    /// Medium company profile: 50 gates x 2 scans/sec = 100 RPS peak.
    /// Simulates shift change rush hour.
    /// </summary>
    [Fact(Skip = SkipMessage)]
    public async Task GateTransitScan_MediumBurst()
    {
        var stats = await _runner.RunPostAsync(
            scenarioName: "GateTransit_MediumBurst",
            endpoint: "/api/gate-transit/scan",
            body: new
            {
                gateId = 1,
                direction = "IN",
                scanType = "QR",
                credentialData = "test-qr-data-medium",
                plateText = (string?)null
            },
            concurrency: 30,
            durationSeconds: 30);

        _output.WriteLine(stats.ToString());

        Assert.True(stats.TotalRequests > 0, "Must complete requests");
    }

    /// <summary>
    /// QR access verification under sustained load.
    /// </summary>
    [Fact(Skip = SkipMessage)]
    public async Task QrAccessVerify_LoadTest()
    {
        var stats = await _runner.RunPostAsync(
            scenarioName: "QrAccess_Verify",
            endpoint: "/api/QrAccess/verify-camera-auth",
            body: new
            {
                qrData = "test-qr-verify",
                gateId = 1,
            },
            concurrency: 15,
            durationSeconds: 30);

        _output.WriteLine(stats.ToString());

        Assert.True(stats.TotalRequests > 0, "Must complete requests");
        var errorRate = stats.TotalRequests > 0
            ? (double)stats.FailedRequests / stats.TotalRequests
            : 1.0;
        Assert.True(errorRate < 0.15, $"Error rate too high: {errorRate:P2} - likely expected with invalid QR data");
    }

    /// <summary>
    /// Barrier command audit under load - simulates gate operators issuing open/close commands.
    /// </summary>
    [Fact(Skip = SkipMessage)]
    public async Task BarrierCommand_LoadTest()
    {
        var stats = await _runner.RunPostAsync(
            scenarioName: "Barrier_Command",
            endpoint: "/api/enterprise/visitor-vehicle/barriers",
            body: new
            {
                laneId = (int?)null,
                name = "LoadTest Barrier",
                state = "Closed"
            },
            concurrency: 5,
            durationSeconds: 20);

        _output.WriteLine(stats.ToString());

        Assert.True(stats.TotalRequests > 0, "Must complete requests");
    }
}
