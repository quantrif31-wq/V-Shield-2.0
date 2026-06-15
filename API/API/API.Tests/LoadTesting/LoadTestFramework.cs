using System.Diagnostics;
using System.Net.Http;
using System.Text;
using System.Text.Json;

namespace API.Tests.LoadTesting;

/// <summary>
/// Configuration for load test runs.
/// </summary>
public class LoadTestConfiguration
{
    public string BaseUrl { get; set; } = "http://localhost:5107";
    public string AuthToken { get; set; } = string.Empty;
    public int DefaultDurationSeconds { get; set; } = 60;
    public int DefaultWarmUpSeconds { get; set; } = 3;
    public int DefaultConcurrency { get; set; } = 10;
}

/// <summary>
/// Statistics collected from a load test scenario.
/// </summary>
public class LoadTestStatistics
{
    public string ScenarioName { get; set; } = string.Empty;
    public long TotalRequests { get; set; }
    public long SuccessfulRequests { get; set; }
    public long FailedRequests { get; set; }
    public double RequestsPerSecond { get; set; }
    public double LatencyMsMean { get; set; }
    public double LatencyMsP50 { get; set; }
    public double LatencyMsP95 { get; set; }
    public double LatencyMsP99 { get; set; }
    public double LatencyMsMin { get; set; }
    public double LatencyMsMax { get; set; }
    public bool Passed => FailedRequests == 0;
    public List<string> Errors { get; set; } = new();

    public override string ToString()
    {
        return $"[{ScenarioName}] " +
               $"OK={SuccessfulRequests} Fail={FailedRequests} " +
               $"RPS={RequestsPerSecond:F1} " +
               $"P50={LatencyMsP50:F0}ms P95={LatencyMsP95:F0}ms P99={LatencyMsP99:F0}ms " +
               $"Min={LatencyMsMin:F0}ms Max={LatencyMsMax:F0}ms " +
               (Passed ? " ✅ PASS" : " ❌ FAIL");
    }
}

/// <summary>
/// Lightweight HTTP load testing framework. No external dependencies required.
/// Runs concurrent HTTP requests and collects latency/throughput statistics.
/// </summary>
public class LoadTestRunner
{
    private readonly LoadTestConfiguration _config;

    public LoadTestRunner(LoadTestConfiguration config)
    {
        _config = config;
    }

    /// <summary>
    /// Run a load test scenario against a GET endpoint.
    /// </summary>
    public async Task<LoadTestStatistics> RunGetAsync(
        string scenarioName,
        string endpoint,
        int? concurrency = null,
        int? durationSeconds = null)
    {
        return await RunScenarioAsync(scenarioName, async client =>
        {
            var request = CreateRequest(HttpMethod.Get, endpoint);
            return await client.SendAsync(request);
        }, concurrency, durationSeconds);
    }

    /// <summary>
    /// Run a load test scenario against a POST endpoint.
    /// </summary>
    public async Task<LoadTestStatistics> RunPostAsync(
        string scenarioName,
        string endpoint,
        object body,
        int? concurrency = null,
        int? durationSeconds = null)
    {
        return await RunScenarioAsync(scenarioName, async client =>
        {
            var request = CreateRequest(HttpMethod.Post, endpoint, body);
            return await client.SendAsync(request);
        }, concurrency, durationSeconds);
    }

    /// <summary>
    /// Run a load test scenario against a PATCH endpoint.
    /// </summary>
    public async Task<LoadTestStatistics> RunPatchAsync(
        string scenarioName,
        string endpoint,
        object body,
        int? concurrency = null,
        int? durationSeconds = null)
    {
        return await RunScenarioAsync(scenarioName, async client =>
        {
            var request = CreateRequest(HttpMethod.Patch, endpoint, body);
            return await client.SendAsync(request);
        }, concurrency, durationSeconds);
    }

    /// <summary>
    /// Core load test runner. Spawns concurrent workers that continuously send requests
    /// for the specified duration and collects statistics.
    /// </summary>
    public async Task<LoadTestStatistics> RunScenarioAsync(
        string scenarioName,
        Func<HttpClient, Task<HttpResponseMessage>> scenario,
        int? concurrency = null,
        int? durationSeconds = null)
    {
        var actualConcurrency = concurrency ?? _config.DefaultConcurrency;
        var actualDuration = TimeSpan.FromSeconds(durationSeconds ?? _config.DefaultDurationSeconds);
        var errors = new List<string>();
        var errorLock = new object();
        var latencies = new List<double>();
        var latencyLock = new object();
        var successCount = 0L;
        var failCount = 0L;

        // Warm-up phase
        var warmUpDuration = _config.DefaultWarmUpSeconds;
        Console.WriteLine($"  Warm-up: {warmUpDuration}s...");
        var warmupCts = new CancellationTokenSource(TimeSpan.FromSeconds(warmUpDuration));
        using (var warmupClient = CreateClient())
        {
            var warmupTask = Task.Run(async () =>
            {
                while (!warmupCts.Token.IsCancellationRequested)
                {
                    try
                    {
                        await scenario(warmupClient);
                    }
                    catch
                    {
                        // ignore warm-up errors
                    }
                }
            }, warmupCts.Token);
            await warmupTask;
        }

        // Main test phase
        Console.WriteLine($"  Running: {actualConcurrency} concurrent users for {actualDuration.TotalSeconds}s...");
        var cts = new CancellationTokenSource(actualDuration);
        var workerTasks = Enumerable.Range(0, actualConcurrency)
            .Select(_ => RunWorker(scenario, cts.Token, latencies, latencyLock, errorLock, errors,
                () => Interlocked.Increment(ref successCount),
                () => Interlocked.Increment(ref failCount)))
            .ToArray();

        await Task.WhenAll(workerTasks);

        // Calculate statistics
        var totalRequests = successCount + failCount;

        var stats = new LoadTestStatistics
        {
            ScenarioName = scenarioName,
            TotalRequests = totalRequests,
            SuccessfulRequests = successCount,
            FailedRequests = failCount,
            RequestsPerSecond = actualDuration.TotalSeconds > 0
                ? totalRequests / actualDuration.TotalSeconds
                : 0,
            Errors = errors.Take(20).ToList()
        };

        if (latencies.Count > 0)
        {
            var sorted = latencies.OrderBy(l => l).ToList();
            stats.LatencyMsMean = sorted.Average();
            stats.LatencyMsMin = sorted.First();
            stats.LatencyMsMax = sorted.Last();
            stats.LatencyMsP50 = sorted[(int)(sorted.Count * 0.50)];
            stats.LatencyMsP95 = sorted[(int)(sorted.Count * 0.95)];
            stats.LatencyMsP99 = sorted[(int)(sorted.Count * 0.99)];
        }

        return stats;
    }

    private async Task RunWorker(
        Func<HttpClient, Task<HttpResponseMessage>> scenario,
        CancellationToken ct,
        List<double> latencies,
        object latencyLock,
        object errorLock,
        List<string> errors,
        Action onSuccess,
        Action onFailure)
    {
        using var client = CreateClient();

        while (!ct.IsCancellationRequested)
        {
            try
            {
                var sw = Stopwatch.StartNew();
                var response = await scenario(client);
                sw.Stop();

                // Record latency (thread-safe)
                lock (latencyLock)
                {
                    latencies.Add(sw.Elapsed.TotalMilliseconds);
                }

                // Check success/failure outside the lock
                if (response.IsSuccessStatusCode)
                {
                    onSuccess();
                }
                else
                {
                    var errorMsg = $"HTTP {(int)response.StatusCode}";
                    onFailure();
                    lock (errorLock)
                    {
                        errors.Add(errorMsg);
                    }
                }
            }
            catch (Exception ex)
            {
                onFailure();
                lock (errorLock)
                {
                    errors.Add(ex.Message);
                }
            }
        }
    }

    private HttpRequestMessage CreateRequest(HttpMethod method, string endpoint, object? body = null)
    {
        var request = new HttpRequestMessage(method, $"{_config.BaseUrl}{endpoint}");

        if (!string.IsNullOrEmpty(_config.AuthToken))
        {
            request.Headers.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _config.AuthToken);
        }

        if (body != null)
        {
            var json = JsonSerializer.Serialize(body);
            request.Content = new StringContent(json, Encoding.UTF8, "application/json");
        }

        return request;
    }

    private HttpClient CreateClient()
    {
        var client = new HttpClient
        {
            BaseAddress = new Uri(_config.BaseUrl)
        };
        client.DefaultRequestHeaders.Add("X-Correlation-Id", Guid.NewGuid().ToString());
        return client;
    }
}
