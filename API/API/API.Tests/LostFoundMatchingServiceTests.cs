using API.Data;
using API.Models;
using API.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace API.Tests;

public sealed class LostFoundMatchingServiceTests
{
    private static ApplicationDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"match_{Guid.NewGuid():N}")
            .Options;
        return new ApplicationDbContext(options);
    }

    [Fact]
    public async Task GetSuggestions_MatchingWordsAndLocation_Suggests()
    {
        var db = CreateDb();
        db.LostItemReports.Add(new LostItemReport
        {
            LostItemReportId = 1,
            Status = "Pending",
            ItemDescription = "điện thoại iPhone màu đen",
            LastSeenLocation = "Căn tin",
            LostAtUtc = DateTime.UtcNow.AddHours(-2)
        });
        db.FoundItemReports.Add(new FoundItemReport
        {
            FoundItemReportId = 2,
            Status = "Unclaimed",
            ItemDescription = "điện thoại iPhone màu đen",
            FoundLocation = "Căn tin",
            FoundAtUtc = DateTime.UtcNow.AddHours(-1)
        });
        db.FoundItemReports.Add(new FoundItemReport
        {
            FoundItemReportId = 3,
            Status = "Unclaimed",
            ItemDescription = "máy tính",
            FoundLocation = "Phòng học",
            FoundAtUtc = DateTime.UtcNow.AddMinutes(-5)
        });
        await db.SaveChangesAsync();
        var svc = new LostFoundMatchingService(db);

        var result = await svc.GetSuggestionsAsync();

        var match = Assert.Single(result);
        Assert.Equal(1, match.LostItemReportId);
        Assert.Equal(2, match.FoundItemReportId);
        Assert.Equal("Suggested", match.Status);
        Assert.True(match.ConfidenceScore >= 0.3);
    }

    [Fact]
    public async Task GetSuggestions_WeakMatch_Excluded()
    {
        var db = CreateDb();
        db.LostItemReports.Add(new LostItemReport
        {
            LostItemReportId = 1,
            Status = "Pending",
            ItemDescription = "chìa khóa nhà",
            LastSeenLocation = "Sân",
            LostAtUtc = DateTime.UtcNow.AddDays(-10)
        });
        db.FoundItemReports.Add(new FoundItemReport
        {
            FoundItemReportId = 2,
            Status = "Unclaimed",
            ItemDescription = "bút bi xanh",
            FoundLocation = "Văn phòng",
            FoundAtUtc = DateTime.UtcNow.AddDays(-5)
        });
        await db.SaveChangesAsync();
        var svc = new LostFoundMatchingService(db);

        var result = await svc.GetSuggestionsAsync();

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetSuggestions_OnlyPendingAndUnclaimed()
    {
        var db = CreateDb();
        db.LostItemReports.Add(new LostItemReport
        {
            LostItemReportId = 1,
            Status = "MatchFound",
            ItemDescription = "điện thoại",
            LostAtUtc = DateTime.UtcNow
        });
        db.FoundItemReports.Add(new FoundItemReport
        {
            FoundItemReportId = 2,
            Status = "Returned",
            ItemDescription = "điện thoại",
            FoundAtUtc = DateTime.UtcNow
        });
        await db.SaveChangesAsync();
        var svc = new LostFoundMatchingService(db);

        var result = await svc.GetSuggestionsAsync();

        Assert.Empty(result);
    }

    [Fact]
    public async Task ConfirmMatch_MissingOrNotSuggested_ReturnsFalse()
    {
        var db = CreateDb();
        var svc = new LostFoundMatchingService(db);

        Assert.False(await svc.ConfirmMatchAsync(1, 1));

        db.ItemMatches.Add(new ItemMatch { ItemMatchId = 2, LostItemReportId = 1, FoundItemReportId = 2, Status = "Confirmed" });
        await db.SaveChangesAsync();
        Assert.False(await svc.ConfirmMatchAsync(2, 1));
    }

    [Fact]
    public async Task ConfirmMatch_Success_UpdatesItems()
    {
        var db = CreateDb();
        db.ItemMatches.Add(new ItemMatch { ItemMatchId = 1, LostItemReportId = 1, FoundItemReportId = 2, Status = "Suggested" });
        db.LostItemReports.Add(new LostItemReport { LostItemReportId = 1, Status = "Pending", ItemDescription = "x", LostAtUtc = DateTime.UtcNow });
        db.FoundItemReports.Add(new FoundItemReport { FoundItemReportId = 2, Status = "Unclaimed", ItemDescription = "x", FoundLocation = "y", FoundAtUtc = DateTime.UtcNow });
        await db.SaveChangesAsync();
        var svc = new LostFoundMatchingService(db);

        var ok = await svc.ConfirmMatchAsync(1, 7);

        Assert.True(ok);
        Assert.Equal("Confirmed", db.ItemMatches.Single().Status);
        Assert.Equal(7, db.ItemMatches.Single().MatchedByUserId);
        Assert.Equal("MatchFound", db.LostItemReports.Single().Status);
        Assert.Equal("MatchPending", db.FoundItemReports.Single().Status);
    }

    [Fact]
    public async Task RejectMatch_Missing_ReturnsFalse()
    {
        var db = CreateDb();
        var svc = new LostFoundMatchingService(db);
        Assert.False(await svc.RejectMatchAsync(1, 1));
    }

    [Fact]
    public async Task RejectMatch_Success()
    {
        var db = CreateDb();
        db.ItemMatches.Add(new ItemMatch { ItemMatchId = 1, LostItemReportId = 1, FoundItemReportId = 2, Status = "Suggested" });
        await db.SaveChangesAsync();
        var svc = new LostFoundMatchingService(db);

        var ok = await svc.RejectMatchAsync(1, 3);

        Assert.True(ok);
        Assert.Equal("Rejected", db.ItemMatches.Single().Status);
        Assert.Equal(3, db.ItemMatches.Single().MatchedByUserId);
    }
}