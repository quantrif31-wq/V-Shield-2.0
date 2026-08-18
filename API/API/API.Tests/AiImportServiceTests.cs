using API.DTOs;
using API.Services.ImportExport;
using API.Services.ImportExport.AI;
using API.Services.ImportExport.Validation;
using Moq;
using Xunit;

namespace API.Tests;

public static class FakeImportHandlers
{
    public static List<TemplateFieldInfo> EmployeeTemplate() =>
    [
        new() { FieldName = "FullName", DisplayName = "Họ tên", DataType = "string", IsRequired = true },
        new() { FieldName = "Status", DisplayName = "Trạng thái", DataType = "bool" }
    ];

    public sealed class EmployeeHandler : IEntityImportHandler
    {
        public string EntityType => "employee";
        public string DisplayName => "Nhân viên";
        public List<TemplateFieldInfo> GetTemplateFields() => EmployeeTemplate();
        public Task<List<ImportErrorDetail>> ValidateRowAsync(Dictionary<string, object?> row, int rowIndex, ImportValidationContext context) => Task.FromResult(new List<ImportErrorDetail>());
        public Task<object?> CreateEntityAsync(Dictionary<string, object?> row, ImportValidationContext context) => Task.FromResult<object?>(null);
        public Task<object?> UpdateEntityAsync(Dictionary<string, object?> row, object existingEntity, ImportValidationContext context) => Task.FromResult<object?>(null);
        public Task<Dictionary<string, object?>> EntityToDictionaryAsync(object entity) => Task.FromResult(new Dictionary<string, object?>());
        public Task<List<Dictionary<string, object?>>> ExportDataAsync(ExportRequest request) => Task.FromResult(new List<Dictionary<string, object?>>());
        public Func<object, bool>? GetExportFilter(ExportRequest request) => null;
    }
}

public sealed class AiImportServiceTests
{
    private readonly Mock<IFileAnalyzer> _fileAnalyzer = new();
    private readonly Mock<IOcrService> _ocrService = new();
    private readonly Mock<IAiNormalizationService> _aiService = new();
    private readonly Mock<IImportExportService> _importExport = new();
    private readonly StructureValidator _validator = new();
    private readonly SynonymDetector _synonymDetector = new(new SynonymRegistry());

    private AiImportService CreateService()
    {
        var factory = new FileParserFactory(new IFileParser[] { new CsvFileParser() });
        return new AiImportService(
            _fileAnalyzer.Object,
            _ocrService.Object,
            _aiService.Object,
            _importExport.Object,
            factory,
            new IEntityImportHandler[] { new FakeImportHandlers.EmployeeHandler() },
            _synonymDetector,
            _validator);
    }

    private static FileParseResult Parsed(params string[] headers) => new()
    {
        Headers = headers.ToList(),
        Rows = [headers.ToDictionary(h => h, h => (object?)"Nguyen Van A")]
    };

    [Fact]
    public void CreateSession_StoresSession()
    {
        var service = CreateService();
        var session = service.CreateSession("employee", "a.csv", "csv", [1, 2, 3]);

        Assert.Equal("employee", session.EntityType);
        Assert.Equal("a.csv", session.FileName);
        Assert.Equal([1, 2, 3], session.OriginalFileContent);
    }

    [Fact]
    public async Task AnalyzeFileAsync_ForwardsToAnalyzer()
    {
        var service = CreateService();
        var expected = new FileAnalysisResult { IsReadable = true, DetectedFormat = "csv", SuggestedAction = "proceed" };
        _fileAnalyzer.Setup(a => a.AnalyzeAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<string?>()))
            .ReturnsAsync(expected);

        using var stream = new MemoryStream();
        var result = await service.AnalyzeFileAsync("employee", stream, "a.csv", "text/csv");

        Assert.Same(expected, result);
    }

    [Fact]
    public async Task ProcessOcrAsync_UnknownEntity_Throws()
    {
        var service = CreateService();
        using var stream = new MemoryStream();
        await Assert.ThrowsAsync<NotSupportedException>(() => service.ProcessOcrAsync("nope", stream, "a.pdf"));
    }

    [Fact]
    public async Task ProcessOcrAsync_OcrFailed_ReturnsFailedStatus()
    {
        var service = CreateService();
        _ocrService.Setup(o => o.ExtractTextAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new OcrResult { Success = false, ErrorMessage = "no tesseract" });

        using var stream = new MemoryStream();
        var result = await service.ProcessOcrAsync("employee", stream, "a.pdf");

        Assert.Equal("failed", result.Status);
        Assert.Equal("no tesseract", result.ErrorMessage);
    }

    [Fact]
    public async Task ProcessOcrAsync_NoIssues_ReturnsReady()
    {
        var service = CreateService();
        _ocrService.Setup(o => o.ExtractTextAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new OcrResult { Success = true, RawText = "text" });
        _aiService.Setup(a => a.ParseOcrTextAsync(It.IsAny<string>(), It.IsAny<IEntityImportHandler>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Parsed("FullName", "Status"));

        using var stream = new MemoryStream();
        var result = await service.ProcessOcrAsync("employee", stream, "a.pdf");

        Assert.Equal("ready", result.Status);
    }

    [Fact]
    public async Task ProcessOcrAsync_SynonymIssue_ReturnsNeedsNormalization()
    {
        var service = CreateService();
        _ocrService.Setup(o => o.ExtractTextAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new OcrResult { Success = true, RawText = "text" });
        _aiService.Setup(a => a.ParseOcrTextAsync(It.IsAny<string>(), It.IsAny<IEntityImportHandler>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Parsed("ho va ten"));

        using var stream = new MemoryStream();
        var result = await service.ProcessOcrAsync("employee", stream, "a.pdf");

        Assert.Equal("needs_normalization", result.Status);
        Assert.NotEmpty(result.Changes);
    }

    [Fact]
    public async Task NormalizeAndPreviewAsync_SessionNotFound_Throws()
    {
        var service = CreateService();
        await Assert.ThrowsAsync<KeyNotFoundException>(() => service.NormalizeAndPreviewAsync("employee", Guid.NewGuid()));
    }

    [Fact]
    public async Task NormalizeAndPreviewAsync_NoIssues_ReturnsReadyWithoutAi()
    {
        var service = CreateService();
        var session = service.CreateSession("employee", "a.csv", "csv", null);
        session.ParsedData = Parsed("FullName");

        var preview = await service.NormalizeAndPreviewAsync("employee", session.SessionId);

        Assert.True(preview.ReadyForImport);
        Assert.Equal(0, preview.ChangeCount);
    }

    [Fact]
    public async Task NormalizeAndPreviewAsync_IssuesWithAi_UsesAiNormalizer()
    {
        var service = CreateService();
        _aiService.Setup(a => a.IsAvailable()).Returns(true);
        _aiService.Setup(a => a.NormalizeAsync(It.IsAny<FileParseResult>(), It.IsAny<IEntityImportHandler>(), It.IsAny<List<SynonymIssue>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AiProcessingResult { Status = "success", NormalizedData = Parsed("FullName") });

        var session = service.CreateSession("employee", "a.csv", "csv", null);
        session.ParsedData = Parsed("ho va ten");

        var preview = await service.NormalizeAndPreviewAsync("employee", session.SessionId);

        Assert.Equal(session.SessionId, preview.SessionId);
        _aiService.Verify(a => a.NormalizeAsync(It.IsAny<FileParseResult>(), It.IsAny<IEntityImportHandler>(), It.IsAny<List<SynonymIssue>>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ConfirmImportAsync_ImportsAndRemovesSession()
    {
        var service = CreateService();
        _importExport.Setup(i => i.ImportAsync(It.IsAny<string>(), It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<ImportRequest?>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ImportResponse { Status = "Completed", TotalRows = 1 });

        var session = service.CreateSession("employee", "a.csv", "csv", null);
        session.ParsedData = Parsed("FullName");

        var result = await service.ConfirmImportAsync("employee", session.SessionId, new AiImportRequest { ConfirmNormalization = true }, 3);

        Assert.Equal("Completed", result.Status);
        _importExport.Verify(i => i.ImportAsync("employee", It.IsAny<Stream>(), It.IsAny<string>(), "application/json", It.IsAny<ImportRequest?>(), 3, It.IsAny<CancellationToken>()), Times.Once);
        Assert.Null(await service.GetPreviewAsync(session.SessionId));
    }

    [Fact]
    public async Task ConfirmImportAsync_SessionNotFound_Throws()
    {
        var service = CreateService();
        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            service.ConfirmImportAsync("employee", Guid.NewGuid(), new AiImportRequest(), 3));
    }

    [Fact]
    public async Task GetPreviewAsync_NoSession_ReturnsNull()
    {
        var service = CreateService();
        Assert.Null(await service.GetPreviewAsync(Guid.NewGuid()));
    }
}