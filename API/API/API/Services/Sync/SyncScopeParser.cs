namespace API.Services.Sync;

public static class SyncScopeParser
{
    public static IReadOnlyList<int> ParseIds(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return [];
        }

        return raw
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(value => int.TryParse(value, out var parsed) ? parsed : (int?)null)
            .Where(value => value.HasValue)
            .Select(value => value!.Value)
            .Distinct()
            .ToArray();
    }
}
