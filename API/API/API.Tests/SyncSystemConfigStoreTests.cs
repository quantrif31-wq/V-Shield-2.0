using API.Data;
using API.Services.Sync;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace API.Tests;

public sealed class SyncSystemConfigStoreTests
{
    private static ApplicationDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"cfg_{Guid.NewGuid():N}")
            .Options;
        return new ApplicationDbContext(options);
    }

    [Fact]
    public async Task NodeSecret_RoundTrips()
    {
        var db = CreateDb();
        var store = new SyncSystemConfigStore(db);

        Assert.Null(await store.GetNodeSecretAsync());

        await store.SetNodeSecretAsync("secret-abc");
        Assert.Equal("secret-abc", await store.GetNodeSecretAsync());
        Assert.Equal("secret-abc", db.SystemConfigs.Single(c => c.Key == "sync.node.secret").Value);
    }

    [Fact]
    public async Task NodeSecret_OverwritesExistingValue()
    {
        var db = CreateDb();
        var store = new SyncSystemConfigStore(db);

        await store.SetNodeSecretAsync("one");
        await store.SetNodeSecretAsync("two");

        Assert.Equal("two", await store.GetNodeSecretAsync());
        Assert.Single(db.SystemConfigs.Where(c => c.Key == "sync.node.secret"));
    }

    [Fact]
    public async Task LastPulledSequence_DefaultsToZero()
    {
        var db = CreateDb();
        var store = new SyncSystemConfigStore(db);
        Assert.Equal(0, await store.GetLastPulledSequenceAsync());
    }

    [Fact]
    public async Task LastPulledSequence_RoundTrips()
    {
        var db = CreateDb();
        var store = new SyncSystemConfigStore(db);

        await store.SetLastPulledSequenceAsync(12345);
        Assert.Equal(12345, await store.GetLastPulledSequenceAsync());
    }

    [Fact]
    public async Task BootstrapCompleted_RoundTripsTrueThenFalse()
    {
        var db = CreateDb();
        var store = new SyncSystemConfigStore(db);

        Assert.False(await store.GetBootstrapCompletedAsync());

        await store.SetBootstrapCompletedAsync(true);
        Assert.True(await store.GetBootstrapCompletedAsync());

        await store.SetBootstrapCompletedAsync(false);
        Assert.False(await store.GetBootstrapCompletedAsync());
    }
}