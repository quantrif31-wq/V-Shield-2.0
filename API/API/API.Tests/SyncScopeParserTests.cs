using API.Services.Sync;
using Xunit;

namespace API.Tests;

public sealed class SyncScopeParserTests
{
    [Fact]
    public void ParseIds_NullOrEmpty_ReturnsEmpty()
    {
        Assert.Empty(SyncScopeParser.ParseIds(null));
        Assert.Empty(SyncScopeParser.ParseIds(""));
        Assert.Empty(SyncScopeParser.ParseIds("   "));
    }

    [Fact]
    public void ParseIds_SplitsAndTrims()
    {
        var result = SyncScopeParser.ParseIds("1, 2, 3");
        Assert.Equal(new[] { 1, 2, 3 }, result);
    }

    [Fact]
    public void ParseIds_SkipsInvalidAndDeduplicates()
    {
        var result = SyncScopeParser.ParseIds("1,abc,1,4,,5");
        Assert.Equal(new[] { 1, 4, 5 }, result);
    }

    [Fact]
    public void ParseIds_AllInvalid_ReturnsEmpty()
    {
        Assert.Empty(SyncScopeParser.ParseIds("abc, xyz, "));
    }
}