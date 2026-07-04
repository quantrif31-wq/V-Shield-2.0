using API.Data;
using API.Models;
using Microsoft.EntityFrameworkCore;

namespace API.Services.Sync;

public class SyncSystemConfigStore
{
    private readonly ApplicationDbContext _db;

    public SyncSystemConfigStore(ApplicationDbContext db)
    {
        _db = db;
    }

    public Task<string?> GetNodeSecretAsync(CancellationToken cancellationToken = default) =>
        GetAsync("sync.node.secret", cancellationToken);

    public Task SetNodeSecretAsync(string value, CancellationToken cancellationToken = default) =>
        SetAsync("sync.node.secret", value, cancellationToken);

    public async Task<long> GetLastPulledSequenceAsync(CancellationToken cancellationToken = default)
    {
        var value = await GetAsync("sync.last-pulled-sequence", cancellationToken);
        return long.TryParse(value, out var parsed) ? parsed : 0;
    }

    public Task SetLastPulledSequenceAsync(long value, CancellationToken cancellationToken = default) =>
        SetAsync("sync.last-pulled-sequence", value.ToString(), cancellationToken);

    public async Task<bool> GetBootstrapCompletedAsync(CancellationToken cancellationToken = default)
    {
        var value = await GetAsync("sync.bootstrap.completed", cancellationToken);
        return string.Equals(value, "true", StringComparison.OrdinalIgnoreCase);
    }

    public Task SetBootstrapCompletedAsync(bool value, CancellationToken cancellationToken = default) =>
        SetAsync("sync.bootstrap.completed", value ? "true" : "false", cancellationToken);

    private async Task<string?> GetAsync(string key, CancellationToken cancellationToken)
    {
        return await _db.SystemConfigs
            .Where(item => item.Key == key)
            .Select(item => item.Value)
            .FirstOrDefaultAsync(cancellationToken);
    }

    private async Task SetAsync(string key, string value, CancellationToken cancellationToken)
    {
        var entry = await _db.SystemConfigs.FindAsync([key], cancellationToken);
        if (entry == null)
        {
            entry = new SystemConfig { Key = key };
            _db.SystemConfigs.Add(entry);
        }

        entry.Value = value;
        entry.UpdatedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
    }
}
