using API.Services.ImportExport;
using API.Services.ImportExport.AI;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace API.Tests;

public sealed class FileAnalyzerTests
{
    private static FileAnalyzer CreateAnalyzer()
    {
        var parser = new Mock<IFileParser>();
        parser.SetupGet(p => p.Format).Returns("csv");
        parser.Setup(p => p.ParseAsync(It.IsAny<Stream>(), It.IsAny<FileParseOptions>()))
            .ReturnsAsync(new FileParseResult { Headers = ["A"], Rows = [new Dictionary<string, object?> { ["A"] = "1" }] });
        var factory = new FileParserFactory(new[] { parser.Object });
        return new FileAnalyzer(factory);
    }

    [Fact]
    public async Task AnalyzeAsync_ReadableCsv_ReturnsProceed()
    {
        var analyzer = CreateAnalyzer();
        using var stream = new MemoryStream("a\n1"u8.ToArray());
        var result = await analyzer.AnalyzeAsync(stream, "data.csv", null);

        Assert.True(result.IsReadable);
        Assert.Equal("csv", result.DetectedFormat);
        Assert.Equal("proceed", result.SuggestedAction);
        Assert.NotNull(result.ParsedData);
    }

    [Fact]
    public async Task AnalyzeAsync_OcrImage_SuggestsOcr()
    {
        var analyzer = CreateAnalyzer();
        using var stream = new MemoryStream();
        var result = await analyzer.AnalyzeAsync(stream, "scan.png", "image/png");

        Assert.False(result.IsReadable);
        Assert.Equal("ocr", result.SuggestedAction);
        Assert.Contains("OCR", result.Message);
    }

    [Fact]
    public async Task AnalyzeAsync_Doc_SuggestsOcr()
    {
        var analyzer = CreateAnalyzer();
        using var stream = new MemoryStream();
        var result = await analyzer.AnalyzeAsync(stream, "report.docx", null);

        Assert.False(result.IsReadable);
        Assert.Equal("ocr", result.SuggestedAction);
    }

    [Fact]
    public async Task AnalyzeAsync_Unknown_SuggestsAiAssist()
    {
        var analyzer = CreateAnalyzer();
        using var stream = new MemoryStream();
        var result = await analyzer.AnalyzeAsync(stream, "weird.bin", null);

        Assert.False(result.IsReadable);
        Assert.Equal("ai_assist", result.SuggestedAction);
    }

    [Theory]
    [InlineData("png", true)]
    [InlineData("pdf", true)]
    [InlineData("docx", true)]
    [InlineData("csv", false)]
    public void IsAISuggestedFormat_Classifies(string format, bool expected)
    {
        Assert.Equal(expected, CreateAnalyzer().IsAISuggestedFormat(format));
    }

    [Theory]
    [InlineData("png", true)]
    [InlineData("jpg", true)]
    [InlineData("docx", false)]
    [InlineData("csv", false)]
    public void NeedsOcr_Classifies(string format, bool expected)
    {
        Assert.Equal(expected, CreateAnalyzer().NeedsOcr(format));
    }
}

public sealed class OcrServiceTests
{
    private static OcrService CreateService() => new(NullLogger<OcrService>.Instance);

    [Theory]
    [InlineData("scan.pdf", true)]
    [InlineData("image.PNG", true)]
    [InlineData("photo.jpg", true)]
    [InlineData("sheet.tiff", true)]
    [InlineData("doc.docx", true)]
    [InlineData("data.csv", false)]
    [InlineData("data.txt", false)]
    [InlineData(null, false)]
    public void CanHandle_ByExtension(string? fileName, bool expected)
    {
        Assert.Equal(expected, CreateService().CanHandle(fileName!));
    }
}