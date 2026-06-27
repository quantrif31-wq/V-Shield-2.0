using API.Data;
using API.Models;
using Microsoft.EntityFrameworkCore;

namespace API.Services;

public class LostFoundMatchingService
{
    private readonly ApplicationDbContext _context;

    public LostFoundMatchingService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<ItemMatch>> GetSuggestionsAsync(int maxSuggestions = 10)
    {
        var pendingLost = await _context.LostItemReports
            .Where(l => l.Status == "Pending")
            .ToListAsync();

        var unclaimedFound = await _context.FoundItemReports
            .Where(f => f.Status == "Unclaimed")
            .ToListAsync();

        var suggestions = new List<ItemMatch>();

        foreach (var lost in pendingLost)
        {
            foreach (var found in unclaimedFound)
            {
                var score = ComputeMatchScore(lost, found);
                if (score >= 0.3)
                {
                    suggestions.Add(new ItemMatch
                    {
                        LostItemReportId = lost.LostItemReportId,
                        FoundItemReportId = found.FoundItemReportId,
                        ConfidenceScore = score,
                        Status = "Suggested",
                        Note = $"Auto-suggested match (confidence: {score:P0})"
                    });
                }
            }
        }

        return suggestions.OrderByDescending(s => s.ConfidenceScore).Take(maxSuggestions).ToList();
    }

    private static double ComputeMatchScore(LostItemReport lost, FoundItemReport found)
    {
        var score = 0.0;

        var lostWords = lost.ItemDescription.ToLower().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var foundWords = found.ItemDescription.ToLower().Split(' ', StringSplitOptions.RemoveEmptyEntries);

        var common = lostWords.Intersect(foundWords).Count();
        if (common > 0)
            score += (double)common / Math.Max(lostWords.Length, foundWords.Length) * 0.5;

        var timeDiff = Math.Abs((lost.LostAtUtc - found.FoundAtUtc).TotalHours);
        if (timeDiff <= 24)
            score += 0.2;
        else if (timeDiff <= 72)
            score += 0.1;

        if (!string.IsNullOrWhiteSpace(lost.LastSeenLocation) && !string.IsNullOrWhiteSpace(found.FoundLocation))
        {
            if (lost.LastSeenLocation.Contains(found.FoundLocation, StringComparison.OrdinalIgnoreCase) ||
                found.FoundLocation.Contains(lost.LastSeenLocation, StringComparison.OrdinalIgnoreCase))
                score += 0.3;
        }

        return Math.Min(score, 1.0);
    }

    public async Task<bool> ConfirmMatchAsync(long matchId, int userId)
    {
        var match = await _context.ItemMatches.FindAsync(matchId);
        if (match == null || match.Status != "Suggested")
            return false;

        match.Status = "Confirmed";
        match.MatchedByUserId = userId;
        match.MatchedAtUtc = DateTime.UtcNow;

        var lost = await _context.LostItemReports.FindAsync(match.LostItemReportId);
        if (lost != null) lost.Status = "MatchFound";

        var found = await _context.FoundItemReports.FindAsync(match.FoundItemReportId);
        if (found != null) found.Status = "MatchPending";

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> RejectMatchAsync(long matchId, int userId)
    {
        var match = await _context.ItemMatches.FindAsync(matchId);
        if (match == null || match.Status != "Suggested")
            return false;

        match.Status = "Rejected";
        match.MatchedByUserId = userId;
        await _context.SaveChangesAsync();
        return true;
    }
}
