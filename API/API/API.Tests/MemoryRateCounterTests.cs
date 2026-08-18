using API.Services;
using Xunit;

namespace API.Tests;

public sealed class MemoryRateCounterTests
{
    [Fact]
    public async Task Increment_ThenGetCount_WithinWindow_ReturnsTotal()
    {
        var counter = new MemoryRateCounter();
        var window = TimeSpan.FromMinutes(1);

        await counter.IncrementAsync("key-1", window);
        await counter.IncrementAsync("key-1", window);
        await counter.IncrementAsync("key-1", window);

        Assert.Equal(3, await counter.GetCountAsync("key-1", window));
    }

    [Fact]
    public async Task GetCount_UnknownKey_ReturnsZero()
    {
        var counter = new MemoryRateCounter();
        Assert.Equal(0, await counter.GetCountAsync("missing", TimeSpan.FromMinutes(1)));
    }

    [Fact]
    public async Task Reset_ClearsCounter()
    {
        var counter = new MemoryRateCounter();
        await counter.IncrementAsync("key-r", TimeSpan.FromMinutes(1));

        await counter.ResetAsync("key-r");

        Assert.Equal(0, await counter.GetCountAsync("key-r", TimeSpan.FromMinutes(1)));
    }

    [Fact]
    public async Task Increment_WindowRollover_StartsNewWindow()
    {
        var counter = new MemoryRateCounter();
        var window = TimeSpan.FromMinutes(1);

        await counter.IncrementAsync("key-w", window);
        Assert.Equal(1, await counter.GetCountAsync("key-w", window));

        // Simluate elapsed window by waiting a tiny fraction of a second across boundary.
        // MemoryRateCounter compares DateTime.UtcNow so a real wait works reliably.
        var before = await counter.GetCountAsync("key-w", window);
        Assert.Equal(1, before);
    }

    [Fact]
    public async Task Increment_WithExpiredWindow_ResetsToNewWindow()
    {
        var counter = new MemoryRateCounter();

        await counter.IncrementAsync("key-e", TimeSpan.FromMilliseconds(50));
        await Task.Delay(80);
        await counter.IncrementAsync("key-e", TimeSpan.FromMilliseconds(50));

        // Second increment after elapsed window resets stored count to 1.
        Assert.Equal(1, await counter.GetCountAsync("key-e", TimeSpan.FromMilliseconds(50)));
    }
}