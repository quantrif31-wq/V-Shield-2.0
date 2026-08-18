using API.Controllers;
using API.DTOs;
using API.Services.ImportExport;
using API.Services.ImportExport.AI;
using API.Services.ImportExport.Validation;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;
using Moq;
using Xunit;

namespace API.Tests;

public sealed class AiImportControllerTests
{
    private readonly Mock<IAiImportService> _aiService = new();
    private readonly Mock<IFileAnalyzer> _fileAnalyzer = new();
    private readonly Mock<IOcrService> _ocrService = new();
    private readonly Mock<IAiNormalizationService> _aiNormalization = new();
    private readonly SynonymRegistry _registry = new();
    private readonly FileParserFactory _factory = new(new IFileParser[] { new CsvFileParser() });

    private AiImportController CreateController(Action<AiImportController>? configure = null)
    {
        var controller = new AiImportController(
            _aiService.Object,
            _fileAnalyzer.Object,
            _ocrService.Object,
            _aiNormalization.Object,
            _registry,
            _factory,
            new IEntityImportHandler[] { new FakeImportHandlers.EmployeeHandler() });
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new System.Security.Claims.ClaimsPrincipal(
                    new System.Security.Claims.ClaimsIdentity(
                        new[] { new System.Security.Claims.Claim("sub", "7") }, "test"))
            },
            RouteData = new Microsoft.AspNetCore.Routing.RouteData()
        };
        controller.RouteData.Values["entityType"] = "employee";
        configure?.Invoke(controller);
        return controller;
    }

    private static void AttachFile(AiImportController controller, byte[] content, string name, string contentType)
    {
        var stream = new MemoryStream(content);
        var file = new FormFile(stream, 0, stream.Length, "file", name)
        {
            Headers = new HeaderDictionary(),
            ContentType = contentType
        };
        SetForm(controller, new FormCollection(new Dictionary<string, StringValues>(), new FormFileCollection { file }));
    }

    private static void SetForm(AiImportController controller, IFormCollection form)
    {
        controller.Request.HttpContext.Features.Set<IFormFeature>(new FakeFormFeature(form));
    }

    private static void AttachEmptyForm(AiImportController controller)
    {
        SetForm(controller, new FormCollection(new Dictionary<string, StringValues>()));
    }

    private sealed class FakeFormFeature : IFormFeature
    {
        public FakeFormFeature(IFormCollection form) => Form = form;
        public bool HasFormContentType => true;
        public bool HasForm => true;
        public IFormCollection Form { get; set; }
        public IFormCollection ReadForm() => Form;
        public Task<IFormCollection> ReadFormAsync(CancellationToken cancellationToken) => Task.FromResult(Form);
    }

    private static FileParseResult Parsed() => new()
    {
        Headers = ["FullName"],
        Rows = [new Dictionary<string, object?> { ["FullName"] = "Nguyen Van A" }]
    };

    [Fact]
    public async Task Analyze_NoFile_BadRequest()
    {
        var controller = CreateController();
        AttachEmptyForm(controller);
        var result = await controller.Analyze();
        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task Analyze_ReadableFile_ReturnsSession()
    {
        _fileAnalyzer.Setup(a => a.AnalyzeAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<string?>()))
            .ReturnsAsync(new FileAnalysisResult { IsReadable = true, DetectedFormat = "csv", SuggestedAction = "proceed", ParsedData = Parsed() });
        _aiService.Setup(s => s.CreateSession(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<byte[]>()))
            .Returns(new AiSession { EntityType = "employee", FileName = "a.csv", FileFormat = "csv" });

        var controller = CreateController();
        AttachFile(controller, "a,b\n1,2\n"u8.ToArray(), "a.csv", "text/csv");

        var result = await controller.Analyze();
        var ok = Assert.IsType<OkObjectResult>(result);
        var value = ok.Value!;
        Assert.Equal(true, GetProp(value, "isReadable"));
        Assert.NotNull(GetProp(value, "sessionId"));
    }

    [Fact]
    public async Task Analyze_NonReadable_ReturnsOcrSession()
    {
        _fileAnalyzer.Setup(a => a.AnalyzeAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<string?>()))
            .ReturnsAsync(new FileAnalysisResult { IsReadable = false, DetectedFormat = "png", SuggestedAction = "ocr", Message = "need ocr" });
        _ocrService.Setup(o => o.CanHandle(It.IsAny<string>())).Returns(true);
        _aiService.Setup(s => s.CreateSession(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<byte[]>()))
            .Returns(new AiSession { EntityType = "employee", FileName = "a.png", FileFormat = "png" });

        var controller = CreateController();
        AttachFile(controller, "x"u8.ToArray(), "a.png", "image/png");

        var result = await controller.Analyze();
        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(false, GetProp(ok.Value!, "isReadable"));
        Assert.Equal("ocr", GetProp(ok.Value!, "suggestedAction"));
    }

    [Fact]
    public async Task ProcessOcr_NoFile_BadRequest()
    {
        var controller = CreateController();
        AttachEmptyForm(controller);
        Assert.IsType<BadRequestObjectResult>(await controller.ProcessOcr());
    }

    [Fact]
    public async Task ProcessOcr_Failed_ReturnsBadRequest()
    {
        _aiService.Setup(s => s.ProcessOcrAsync(It.IsAny<string>(), It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AiProcessingResult { Status = "failed", ErrorMessage = "boom" });

        var controller = CreateController();
        AttachFile(controller, "x"u8.ToArray(), "a.pdf", "application/pdf");

        var result = await controller.ProcessOcr();
        var bad = Assert.IsType<BadRequestObjectResult>(result);
        Assert.NotNull(bad.Value);
    }

    [Fact]
    public async Task ProcessOcr_Success_ReturnsOk()
    {
        _aiService.Setup(s => s.ProcessOcrAsync(It.IsAny<string>(), It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AiProcessingResult { Status = "ready", NormalizedData = Parsed() });

        var controller = CreateController();
        AttachFile(controller, "x"u8.ToArray(), "a.pdf", "application/pdf");

        var result = await controller.ProcessOcr();
        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal("ready", GetProp(ok.Value!, "status"));
    }

    [Fact]
    public async Task Normalize_ReturnsPreview()
    {
        _aiService.Setup(s => s.NormalizeAndPreviewAsync(It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AiImportPreviewResponse
            {
                SessionId = Guid.NewGuid(),
                PreviewData = Parsed(),
                Validation = new ValidationResult { IsValid = true },
                ReadyForImport = true,
                TotalRows = 1,
                ChangeCount = 0
            });

        var controller = CreateController();
        var result = await controller.Normalize(new NormalizeRequest { SessionId = Guid.NewGuid() });
        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(true, GetProp(ok.Value!, "readyForImport"));
    }

    [Fact]
    public async Task Confirm_Success_ReturnsOk()
    {
        _aiService.Setup(s => s.ConfirmImportAsync(It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<AiImportRequest>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ImportResponse { Status = "Completed" });

        var controller = CreateController();
        var result = await controller.Confirm(Guid.NewGuid(), new AiImportRequest());
        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(ok.Value);
    }

    [Fact]
    public async Task Confirm_SessionMissing_ReturnsNotFound()
    {
        _aiService.Setup(s => s.ConfirmImportAsync(It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<AiImportRequest>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new KeyNotFoundException());

        var controller = CreateController();
        Assert.IsType<NotFoundObjectResult>(await controller.Confirm(Guid.NewGuid(), new AiImportRequest()));
    }

    [Fact]
    public async Task Confirm_NoData_ReturnsBadRequest()
    {
        _aiService.Setup(s => s.ConfirmImportAsync(It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<AiImportRequest>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("no data"));

        var controller = CreateController();
        Assert.IsType<BadRequestObjectResult>(await controller.Confirm(Guid.NewGuid(), new AiImportRequest()));
    }

    [Fact]
    public async Task GetPreview_Found_ReturnsOk()
    {
        _aiService.Setup(s => s.GetPreviewAsync(It.IsAny<Guid>()))
            .ReturnsAsync(new AiImportPreviewResponse
            {
                SessionId = Guid.NewGuid(),
                PreviewData = Parsed(),
                Validation = new ValidationResult { IsValid = true },
                ReadyForImport = true
            });

        var controller = CreateController();
        Assert.IsType<OkObjectResult>(await controller.GetPreview(Guid.NewGuid()));
    }

    [Fact]
    public async Task GetPreview_Missing_ReturnsNotFound()
    {
        _aiService.Setup(s => s.GetPreviewAsync(It.IsAny<Guid>()))
            .ReturnsAsync((AiImportPreviewResponse?)null);

        var controller = CreateController();
        Assert.IsType<NotFoundObjectResult>(await controller.GetPreview(Guid.NewGuid()));
    }

    [Fact]
    public void GetSynonyms_ReturnsRegistry()
    {
        var controller = CreateController();
        var result = Assert.IsType<OkObjectResult>(controller.GetSynonyms());
        Assert.NotNull(result.Value);
    }

    [Fact]
    public void GetStatus_ReturnsFormatsAndAvailability()
    {
        var controller = CreateController();
        var result = Assert.IsType<OkObjectResult>(controller.GetStatus());
        Assert.NotNull(GetProp(result.Value!, "supportedFormats"));
    }

    private static object? GetProp(object o, string name) =>
        o.GetType().GetProperty(name)?.GetValue(o);
}