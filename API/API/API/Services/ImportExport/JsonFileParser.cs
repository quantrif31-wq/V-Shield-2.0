using System.Text.Json;

namespace API.Services.ImportExport;

public class JsonFileParser : IFileParser
{
    public string Format => "json";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = true,
    };

    public async Task<FileParseResult> ParseAsync(Stream stream, FileParseOptions options)
    {
        var result = new FileParseResult();

        using var reader = new StreamReader(stream, leaveOpen: true);
        var content = await reader.ReadToEndAsync();

        using var doc = JsonDocument.Parse(content);
        var root = doc.RootElement;

        List<JsonElement> items;
        if (root.ValueKind == JsonValueKind.Array)
        {
            items = root.EnumerateArray().ToList();
        }
        else if (root.ValueKind == JsonValueKind.Object && root.TryGetProperty("items", out var itemsProp))
        {
            items = itemsProp.EnumerateArray().ToList();
        }
        else
        {
            items = [root];
        }

        var headers = new HashSet<string>();
        foreach (var item in items)
        {
            if (item.ValueKind != JsonValueKind.Object) continue;
            foreach (var prop in item.EnumerateObject())
                headers.Add(prop.Name);
        }
        result.Headers = headers.ToList();

        foreach (var item in items.Take(options.MaxRows))
        {
            if (item.ValueKind != JsonValueKind.Object) continue;
            var row = new Dictionary<string, object?>();
            foreach (var prop in item.EnumerateObject())
            {
                row[prop.Name] = JsonElementToObject(prop.Value);
            }
            result.Rows.Add(row);
        }

        return result;
    }

    public Task<Stream> SerializeAsync(IReadOnlyList<Dictionary<string, object?>> data, FileSerializeOptions options)
    {
        var columns = options.Columns ?? (data.Count > 0 ? data[0].Keys.ToList() : []);

        var items = new List<Dictionary<string, object?>>();
        foreach (var row in data)
        {
            var item = new Dictionary<string, object?>();
            foreach (var col in columns)
            {
                if (row.TryGetValue(col, out var val))
                    item[col] = val;
            }
            items.Add(item);
        }

        var json = JsonSerializer.Serialize(items, JsonOptions);
        var bytes = System.Text.Encoding.UTF8.GetBytes(json);
        return Task.FromResult((Stream)new MemoryStream(bytes));
    }

    private static object? JsonElementToObject(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.String => element.GetString(),
            JsonValueKind.Number => element.TryGetInt64(out var l) ? l : element.GetDouble(),
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Null => null,
            JsonValueKind.Object => element.ToString(),
            JsonValueKind.Array => element.ToString(),
            _ => element.ToString(),
        };
    }
}
