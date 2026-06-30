using System.Collections.Concurrent;
using Microsoft.Data.SqlClient;

namespace API.Services;

public interface IDistributedRateCounter
{
    Task<int> GetCountAsync(string counterKey, TimeSpan window);
    Task<int> IncrementAsync(string counterKey, TimeSpan window);
    Task ResetAsync(string counterKey);
}

public class MemoryRateCounter : IDistributedRateCounter
{
    private readonly ConcurrentDictionary<string, (int Count, DateTime WindowStart)> _store = new();

    public Task<int> GetCountAsync(string counterKey, TimeSpan window)
    {
        CleanStale(window);
        return Task.FromResult(_store.TryGetValue(counterKey, out var entry) ? entry.Count : 0);
    }

    public Task<int> IncrementAsync(string counterKey, TimeSpan window)
    {
        var now = DateTime.UtcNow;
        var entry = _store.AddOrUpdate(counterKey,
            _ => (1, now),
            (_, existing) =>
            {
                if (now - existing.WindowStart > window)
                    return (1, now);
                return (existing.Count + 1, existing.WindowStart);
            });
        return Task.FromResult(entry.Count);
    }

    public Task ResetAsync(string counterKey)
    {
        _store.TryRemove(counterKey, out _);
        return Task.CompletedTask;
    }

    private void CleanStale(TimeSpan window)
    {
        var cutoff = DateTime.UtcNow - window;
        foreach (var key in _store.Keys)
        {
            if (_store.TryGetValue(key, out var entry) && entry.WindowStart < cutoff)
                _store.TryRemove(key, out _);
        }
    }
}

public class SqlServerRateCounter : IDistributedRateCounter, IDisposable
{
    private readonly string _connectionString;
    private readonly ILogger<SqlServerRateCounter> _logger;
    private readonly Timer _cleanupTimer;

    public SqlServerRateCounter(IConfiguration configuration, ILogger<SqlServerRateCounter> logger)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("DefaultConnection is required for SqlServerRateCounter");
        _logger = logger;
        _cleanupTimer = new Timer(async _ => await CleanupStaleAsync(), null, TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(5));
    }

    public async Task<int> GetCountAsync(string counterKey, TimeSpan window)
    {
        try
        {
            using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();
            using var cmd = new SqlCommand(
                @"SELECT ISNULL(SUM(Count), 0) FROM RateLimitCounters
                  WHERE CounterKey = @key AND WindowStart >= @cutoff",
                conn);
            cmd.Parameters.AddWithValue("@key", counterKey);
            cmd.Parameters.AddWithValue("@cutoff", DateTime.UtcNow - window);
            return (int)(await cmd.ExecuteScalarAsync() ?? 0);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Rate counter SQL query failed, falling back to 0");
            return 0;
        }
    }

    public async Task<int> IncrementAsync(string counterKey, TimeSpan window)
    {
        try
        {
            using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();
            var now = DateTime.UtcNow;
            using var cmd = new SqlCommand(
                @"MERGE RateLimitCounters AS target
                  USING (SELECT @key AS CounterKey, @now AS WindowStart) AS source
                  ON target.CounterKey = source.CounterKey
                     AND target.WindowStart = source.WindowStart
                  WHEN MATCHED THEN UPDATE SET Count = target.Count + 1
                  WHEN NOT MATCHED THEN INSERT (CounterKey, WindowStart, Count)
                      VALUES (@key, @now, 1)
                  OUTPUT INSERTED.Count;",
                conn);
            cmd.Parameters.AddWithValue("@key", counterKey);
            cmd.Parameters.AddWithValue("@now", now);
            var result = await cmd.ExecuteScalarAsync();
            return result != null ? Convert.ToInt32(result) : 1;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Rate counter SQL increment failed, falling back to 1");
            return 1;
        }
    }

    public async Task ResetAsync(string counterKey)
    {
        try
        {
            using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();
            using var cmd = new SqlCommand("DELETE FROM RateLimitCounters WHERE CounterKey = @key", conn);
            cmd.Parameters.AddWithValue("@key", counterKey);
            await cmd.ExecuteNonQueryAsync();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Rate counter SQL reset failed");
        }
    }

    private async Task CleanupStaleAsync()
    {
        try
        {
            using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();
            using var cmd = new SqlCommand(
                "DELETE FROM RateLimitCounters WHERE WindowStart < @cutoff",
                conn);
            cmd.Parameters.AddWithValue("@cutoff", DateTime.UtcNow.AddHours(-1));
            await cmd.ExecuteNonQueryAsync();
        }
        catch
        {
        }
    }

    public void Dispose() => _cleanupTimer.Dispose();
}
