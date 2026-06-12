namespace API.Services.ImportExport;

public interface IFileParser
{
    string Format { get; }
    Task<FileParseResult> ParseAsync(Stream stream, FileParseOptions options);
    Task<Stream> SerializeAsync(IReadOnlyList<Dictionary<string, object?>> data, FileSerializeOptions options);
}

public class FileParseResult
{
    public List<string> Headers { get; set; } = [];
    public List<Dictionary<string, object?>> Rows { get; set; } = [];
    public int TotalRows => Rows.Count;
    public List<FileParseWarning> Warnings { get; set; } = [];
}

public class FileParseWarning
{
    public int Row { get; set; }
    public string? Column { get; set; }
    public string Message { get; set; } = null!;
}

public class FileParseOptions
{
    public bool HasHeaders { get; set; } = true;
    public string? Delimiter { get; set; }
    public string? Encoding { get; set; }
    public int? SheetIndex { get; set; }
    public string? SheetName { get; set; }
    public int MaxRows { get; set; } = 100_000;
}

public class FileSerializeOptions
{
    public bool IncludeHeaders { get; set; } = true;
    public List<string>? Columns { get; set; }
    public string? SheetName { get; set; }
}
