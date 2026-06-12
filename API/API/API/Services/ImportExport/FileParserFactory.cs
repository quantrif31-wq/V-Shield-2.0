using API.DTOs;

namespace API.Services.ImportExport;

public class FileParserFactory
{
    private readonly Dictionary<string, IFileParser> _parsers;

    public FileParserFactory(IEnumerable<IFileParser> parsers)
    {
        _parsers = parsers.ToDictionary(p => p.Format, StringComparer.OrdinalIgnoreCase);
    }

    public IFileParser GetParser(string format)
    {
        var normalized = format.TrimStart('.').ToLowerInvariant();
        if (_parsers.TryGetValue(normalized, out var parser))
            return parser;
        throw new NotSupportedException($"Format '{format}' is not supported. Supported formats: {string.Join(", ", _parsers.Keys)}");
    }

    public string DetectFormat(string fileName, string? contentType)
    {
        var ext = Path.GetExtension(fileName)?.TrimStart('.').ToLowerInvariant();
        if (!string.IsNullOrEmpty(ext) && _parsers.ContainsKey(ext))
            return ext;

        if (!string.IsNullOrEmpty(contentType))
        {
            foreach (var fmt in ImportExportConstants.SupportedFormats)
            {
                if (string.Equals(fmt.MimeType, contentType, StringComparison.OrdinalIgnoreCase))
                    return fmt.Format;
            }
        }

        return ext ?? "csv";
    }

    public bool IsFormatSupported(string format)
    {
        var normalized = format.TrimStart('.').ToLowerInvariant();
        return _parsers.ContainsKey(normalized);
    }
}
