using System.Text;
using API.Services.ImportExport;
using Xunit;

namespace API.Tests;

public sealed class CsvFileParserTests
{
    private readonly CsvFileParser _parser = new();

    [Fact]
    public async Task ParseAsync_WithHeaders_ParsesRows()
    {
        var csv = "FullName,Status\nNguyen Van A,Active\nNguyen Van B,Inactive\n";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csv));

        var result = await _parser.ParseAsync(stream, new FileParseOptions());

        Assert.Equal(new[] { "FullName", "Status" }, result.Headers);
        Assert.Equal(2, result.Rows.Count);
        Assert.Equal("Nguyen Van A", result.Rows[0]["FullName"]);
        Assert.Equal("Inactive", result.Rows[1]["Status"]);
    }

    [Fact]
    public async Task ParseAsync_WithoutHeaders_GeneratesColumnNames()
    {
        var csv = "value1,value2\nvalue3,value4\n";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csv));

        var result = await _parser.ParseAsync(stream, new FileParseOptions { HasHeaders = false });

        Assert.Equal(new[] { "Column1", "Column2" }, result.Headers);
        Assert.Equal("value1", result.Rows[0]["Column1"]);
        Assert.Equal("value4", result.Rows[1]["Column2"]);
    }

    [Fact]
    public async Task ParseAsync_RespectsMaxRows()
    {
        var csv = "A,B\n1,2\n3,4\n5,6\n";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csv));

        var result = await _parser.ParseAsync(stream, new FileParseOptions { MaxRows = 2 });

        Assert.Equal(2, result.Rows.Count);
    }

    [Fact]
    public async Task ParseAsync_UsesCustomDelimiter()
    {
        var csv = "A;B\n1;2\n";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csv));

        var result = await _parser.ParseAsync(stream, new FileParseOptions { Delimiter = ";" });

        Assert.Equal(new[] { "A", "B" }, result.Headers);
        Assert.Equal("1", result.Rows[0]["A"]);
    }

    [Fact]
    public async Task SerializeAsync_WritesCsvWithHeaders()
    {
        var data = new List<Dictionary<string, object?>>
        {
            new() { ["Name"] = "A", ["Age"] = 30 },
            new() { ["Name"] = "B", ["Age"] = 25 },
        };

        using var stream = await _parser.SerializeAsync(data, new FileSerializeOptions());
        using var reader = new StreamReader(stream, Encoding.UTF8, leaveOpen: true);
        var text = await reader.ReadToEndAsync();

        Assert.Contains("Name", text);
        Assert.Contains("Age", text);
        Assert.Contains("A", text);
        Assert.Contains("30", text);
    }

    [Fact]
    public async Task SerializeAsync_WithoutHeaders_OmitsHeaderRow()
    {
        var data = new List<Dictionary<string, object?>>
        {
            new() { ["Name"] = "A" },
        };

        using var stream = await _parser.SerializeAsync(data, new FileSerializeOptions { IncludeHeaders = false });
        using var reader = new StreamReader(stream, Encoding.UTF8, leaveOpen: true);
        var text = await reader.ReadToEndAsync();

        Assert.DoesNotContain("Name", text);
        Assert.Contains("A", text);
    }
}

public sealed class FileParserFactoryTests
{
    [Fact]
    public void GetParser_ReturnsParserByFormat()
    {
        var factory = new FileParserFactory(new IFileParser[] { new CsvFileParser() });
        Assert.Equal("csv", factory.GetParser("csv").Format);
        Assert.Equal("csv", factory.GetParser(".csv").Format);
    }

    [Fact]
    public void GetParser_Throws_ForUnsupportedFormat()
    {
        var factory = new FileParserFactory(new IFileParser[] { new CsvFileParser() });
        Assert.Throws<NotSupportedException>(() => factory.GetParser("exe"));
    }

    [Fact]
    public void DetectFormat_ByExtension()
    {
        var factory = new FileParserFactory(new IFileParser[] { new CsvFileParser() });
        Assert.Equal("csv", factory.DetectFormat("data.csv", null));
    }

    [Fact]
    public void DetectFormat_ByContentType()
    {
        var factory = new FileParserFactory(new IFileParser[] { new CsvFileParser() });
        Assert.Equal("csv", factory.DetectFormat("data.unknown", "text/csv"));
    }

    [Fact]
    public void IsFormatSupported_ChecksNormalizedFormat()
    {
        var factory = new FileParserFactory(new IFileParser[] { new CsvFileParser() });
        Assert.True(factory.IsFormatSupported("CSV"));
        Assert.False(factory.IsFormatSupported("xlsx"));
    }

    [Fact]
    public void GetSupportedFormats_ListsParsers()
    {
        var factory = new FileParserFactory(new IFileParser[] { new CsvFileParser(), new JsonFileParser() });
        var formats = factory.GetSupportedFormats();
        Assert.Contains("csv", formats);
        Assert.Contains("json", formats);
    }
}
