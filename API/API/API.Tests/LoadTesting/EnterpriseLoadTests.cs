using Xunit;
using Xunit.Abstractions;

namespace API.Tests.LoadTesting;

/// <summary>
/// Load tests for enterprise modules: SOC alarm console, evidence governance, operations resilience.
/// These tests validate that the enterprise platform can handle real-world operator workloads.
/// </summary>
[Trait("Category", "LoadTest")]
[Trait("Category", "Enterprise")]
public class EnterpriseLoadTests
{
    private readonly ITestOutputHelper _output;
    private readonly LoadTestRunner _runner;
    private readonly LoadTestConfiguration _config;

    public EnterpriseLoadTests(ITestOutputHelper output)
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

    private const string SkipMessage = "Requires running API server at LOAD_TEST_URL (default http://localhost:5107) with seeded enterprise data. Use --filter \"Category=LoadTest\" to run.";

    /// <summary>
    /// SOC alarm queue - operators polling for new alarms.
    /// Simulates the SOC monitoring page refreshing.
    /// </summary>
    [Fact(Skip = SkipMessage)]
    public async Task SocOverview_LoadTest()
    {
        var stats = await _runner.RunGetAsync(
            scenarioName: "SOC_Overview",
            endpoint: "/api/enterprise/soc/overview",
            concurrency: 15,
            durationSeconds: 30);

        _output.WriteLine(stats.ToString());

        Assert.True(stats.Passed, "SOC overview must be performant for operators");
        Assert.InRange(stats.LatencyMsP95, 0, 1000);
    }

    /// <summary>
    /// Evidence overview - auditor reading evidence summary.
    /// </summary>
    [Fact(Skip = SkipMessage)]
    public async Task EvidenceOverview_LoadTest()
    {
        var stats = await _runner.RunGetAsync(
            scenarioName: "Evidence_Overview",
            endpoint: "/api/enterprise/evidence/overview",
            concurrency: 10,
            durationSeconds: 20);

        _output.WriteLine(stats.ToString());

        Assert.True(stats.Passed, "Evidence overview must load quickly");
        Assert.InRange(stats.LatencyMsP95, 0, 1000);
    }

    /// <summary>
    /// Operations overview - operations dashboard.
    /// </summary>
    [Fact(Skip = SkipMessage)]
    public async Task OperationsOverview_LoadTest()
    {
        var stats = await _runner.RunGetAsync(
            scenarioName: "Operations_Overview",
            endpoint: "/api/enterprise/operations/overview",
            concurrency: 10,
            durationSeconds: 20);

        _output.WriteLine(stats.ToString());

        Assert.True(stats.Passed, "Operations dashboard must be performant");
        Assert.InRange(stats.LatencyMsP95, 0, 1000);
    }

    /// <summary>
    /// Release readiness overview - release manager dashboard.
    /// </summary>
    [Fact(Skip = SkipMessage)]
    public async Task ReleaseReadinessOverview_LoadTest()
    {
        var stats = await _runner.RunGetAsync(
            scenarioName: "Release_Readiness",
            endpoint: "/api/enterprise/release-readiness/overview",
            concurrency: 5,
            durationSeconds: 20);

        _output.WriteLine(stats.ToString());

        Assert.True(stats.TotalRequests > 0, "Must complete requests");
    }

    /// <summary>
    /// Alarm creation burst - simulates many alarms firing simultaneously during an incident.
    /// </summary>
    [Fact(Skip = SkipMessage)]
    public async Task AlarmCreation_BurstTest()
    {
        var stats = await _runner.RunPostAsync(
            scenarioName: "Alarm_Burst",
            endpoint: "/api/enterprise/soc/alarms",
            body: new
            {
                securityEventId = (long?)null,
                alarmType = "AccessDenied",
                severity = "Critical",
                summary = "Load test alarm burst",
                siteId = (int?)null
            },
            concurrency: 20,
            durationSeconds: 20);

        _output.WriteLine(stats.ToString());

        Assert.True(stats.TotalRequests > 0, "Must complete requests");
        Assert.InRange(stats.LatencyMsP95, 0, 3000);
    }

    /// <summary>
    /// Evidence item creation burst - simulates cameras generating many evidence records.
    /// </summary>
    [Fact(Skip = SkipMessage)]
    public async Task EvidenceCreation_BurstTest()
    {
        var stats = await _runner.RunPostAsync(
            scenarioName: "Evidence_Burst",
            endpoint: "/api/enterprise/evidence/items",
            body: new
            {
                evidenceType = "Video",
                sourceType = "Camera",
                sourceReference = "load-test-cam-1",
                storageReference = "/evidence/load-test/clip.mp4",
                hashSha256 = "abcdef1234567890abcdef1234567890",
                privacyLabel = "Internal",
                retentionCategory = "Default",
                siteId = (int?)null,
                isImmutable = false
            },
            concurrency: 10,
            durationSeconds: 20);

        _output.WriteLine(stats.ToString());

        Assert.True(stats.TotalRequests > 0, "Must complete requests");
    }

    /// <summary>
    /// Mixed workload: simulate a SOC operator's typical workflow.
    /// </summary>
    [Fact(Skip = SkipMessage)]
    public async Task SocOperator_SimulatedWorkflow()
    {
        // Multi-step user simulation - alarms, overview, incidents
        var overviewStats = await _runner.RunGetAsync(
            scenarioName: "SocOp_Overview",
            endpoint: "/api/enterprise/soc/overview",
            concurrency: 5,
            durationSeconds: 15);

        _output.WriteLine(overviewStats.ToString());

        var alarmStats = await _runner.RunPostAsync(
            scenarioName: "SocOp_CreateAlarm",
            endpoint: "/api/enterprise/soc/alarms",
            body: new
            {
                securityEventId = (long?)null,
                alarmType = "AccessDenied",
                severity = "High",
                summary = "SOC operator simulated alarm",
                siteId = (int?)null
            },
            concurrency: 5,
            durationSeconds: 15);

        _output.WriteLine(alarmStats.ToString());

        // Combined pass/fail: operator should be able to do all tasks
        var totalStats = new LoadTestStatistics
        {
            ScenarioName = "SocOp_Combined",
            TotalRequests = overviewStats.TotalRequests + alarmStats.TotalRequests,
            SuccessfulRequests = overviewStats.SuccessfulRequests + alarmStats.SuccessfulRequests,
            FailedRequests = overviewStats.FailedRequests + alarmStats.FailedRequests,
        };

        _output.WriteLine(totalStats.ToString());

        var errorRate = totalStats.TotalRequests > 0
            ? (double)totalStats.FailedRequests / totalStats.TotalRequests
            : 1.0;
        Assert.True(errorRate < 0.15, $"SOC operator workflow error rate too high: {errorRate:P2}");
    }
}
