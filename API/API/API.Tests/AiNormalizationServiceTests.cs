using API.DTOs;
using API.Services.ImportExport;
using API.Services.ImportExport.AI;
using API.Services.ImportExport.Validation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Moq.Protected;
using Xunit;

namespace API.Tests;

public sealed class AiNormalizationServiceTests
{
    private sealed class FakeHandler : IEntityImportHandler
    {
        public string EntityType => "employee";
        public string DisplayName => "Employee";
        public List<TemplateFieldInfo> GetTemplateFields() =>
        [
            new() { FieldName = "FullName", DisplayName = "Full Name", DataType = "string", IsRequired = true },
            new() { FieldName = "Status", DisplayName = "Status", DataType = "string", AllowedValues = ["Active", "Inactive"] },
            new() { FieldName = "Gender", DisplayName = "Gender", DataType = "bool" },
        ];
        public Task<List<ImportErrorDetail>> ValidateRowAsync(Dictionary<string, object?> row, int rowIndex, ImportValidationContext context) => Task.FromResult(new List<ImportErrorDetail>());
        public Task<object?> CreateEntityAsync(Dictionary<string, object?> row, ImportValidationContext context) => Task.FromResult<object?>(null);
        public Task<object?> UpdateEntityAsync(Dictionary<string, object?> row, object existingEntity, ImportValidationContext context) => Task.FromResult<object?>(null);
        public Task<Dictionary<string, object?>> EntityToDictionaryAsync(object entity) => Task.FromResult(new Dictionary<string, object?>());
        public Task<List<Dictionary<string, object?>>> ExportDataAsync(ExportRequest request) => Task.FromResult(new List<Dictionary<string, object?>>());
        public Func<object, bool>? GetExportFilter(ExportRequest request) => null;
    }

    private static AiNormalizationService CreateService(
        out Mock<HttpMessageHandler> handlerMock,
        string? apiKey = null,
        string? responseJson = null)
    {
        var handler = new Mock<HttpMessageHandler>();
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = System.Net.HttpStatusCode.OK,
                Content = new StringContent(responseJson ?? """{"choices":[{"message":{"content":"ok"}}]}""")
            });
        handlerMock = handler;

        var services = new ServiceCollection();
        services.AddHttpClient("AiLlm").ConfigurePrimaryHttpMessageHandler(() => handler.Object);
        var sp = services.BuildServiceProvider();

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["AiImport:Llm:ApiKey"] = apiKey,
                ["AiImport:Llm:Endpoint"] = "https://llm.test/v1/chat/completions",
                ["AiImport:Llm:Model"] = "test-model",
            })
            .Build();

        return new AiNormalizationService(
            new SynonymRegistry(),
            sp.GetRequiredService<IHttpClientFactory>(),
            config,
            NullLogger<AiNormalizationService>.Instance);
    }

    [Fact]
    public void IsAvailable_False_WhenNoApiKeyConfigured()
    {
        var original = Environment.GetEnvironmentVariable("VSHIELD_AI_API_KEY");
        Environment.SetEnvironmentVariable("VSHIELD_AI_API_KEY", null);
        try
        {
            var service = CreateService(out _);
            Assert.False(service.IsAvailable());
        }
        finally
        {
            Environment.SetEnvironmentVariable("VSHIELD_AI_API_KEY", original);
        }
    }

    [Fact]
    public void IsAvailable_True_WhenApiKeyConfigured()
    {
        var original = Environment.GetEnvironmentVariable("VSHIELD_AI_API_KEY");
        Environment.SetEnvironmentVariable("VSHIELD_AI_API_KEY", null);
        try
        {
            var service = CreateService(out _, apiKey: "secret-key");
            Assert.True(service.IsAvailable());
        }
        finally
        {
            Environment.SetEnvironmentVariable("VSHIELD_AI_API_KEY", original);
        }
    }

    [Fact]
    public async Task NormalizeAsync_WithoutApiKey_AppliesRuleBasedSynonymNormalization()
    {
        var service = CreateService(out _);
        var data = new FileParseResult
        {
            Headers = ["FullName", "Status", "Gender"],
            Rows =
            [
                new Dictionary<string, object?> { ["FullName"] = "Nguyen Van A", ["Status"] = "hoat dong", ["Gender"] = "1" },
            ]
        };

        var result = await service.NormalizeAsync(data, new FakeHandler(), []);

        Assert.Equal("success", result.Status);
        Assert.Single(result.NormalizedData.Rows);
        Assert.Equal(2, result.Changes.Count);
        Assert.All(result.Changes, c => Assert.NotEqual(string.Empty, c.Reason));
        Assert.Equal("true", result.NormalizedData.Rows[0]["Gender"]);
        Assert.Equal("Active", result.NormalizedData.Rows[0]["Status"]);
    }

    [Fact]
    public async Task NormalizeAsync_WithApiKey_UsesLlmResult()
    {
        var llmRowsJson = """{"rows":[{"FullName":"A","Status":"Active"}],"changes":[{"row":1,"column":"Status","originalValue":"x","normalizedValue":"Active","reason":"llm"}]}""";
        var serializedRows = System.Text.Json.JsonSerializer.Serialize(llmRowsJson);
        var responseJson = "{\"choices\":[{\"message\":{\"content\":" + serializedRows + "}}]}";
        var service = CreateService(out _, apiKey: "k", responseJson: responseJson);
        var data = new FileParseResult
        {
            Headers = ["FullName", "Status"],
            Rows = [new Dictionary<string, object?> { ["FullName"] = "A", ["Status"] = "x" }]
        };

        var result = await service.NormalizeAsync(data, new FakeHandler(), []);

        Assert.Equal("success", result.Status);
        var change = Assert.Single(result.Changes);
        Assert.Equal("llm", change.Reason);
        Assert.Equal("Active", result.NormalizedData.Rows[0]["Status"]?.ToString());
    }

    [Fact]
    public async Task ParseOcrTextAsync_WithoutApiKey_FallsBackToCsvLikeParsing()
    {
        var service = CreateService(out _);
        var raw = "FullName,Status\nNguyen Van A,Active\n";

        var result = await service.ParseOcrTextAsync(raw, new FakeHandler());

        Assert.Equal(2, result.Headers.Count);
        Assert.Single(result.Rows);
        Assert.Equal("Nguyen Van A", result.Rows[0]["FullName"]);
    }

    [Fact]
    public async Task ParseOcrTextAsync_WithApiKey_UsesLlmRows()
    {
        var llmRowsJson = """{"rows":[{"FullName":"A","Status":"Active"}]}""";
        var serializedRows = System.Text.Json.JsonSerializer.Serialize(llmRowsJson);
        var responseJson = "{\"choices\":[{\"message\":{\"content\":" + serializedRows + "}}]}";
        var service = CreateService(out _, apiKey: "k", responseJson: responseJson);

        var result = await service.ParseOcrTextAsync("raw text", new FakeHandler());

        var row = Assert.Single(result.Rows);
        Assert.Equal("A", row["FullName"]?.ToString());
    }

    [Fact]
    public async Task NormalizeAsync_AllowsSynonymInAllowedValues()
    {
        var service = CreateService(out _);
        var data = new FileParseResult
        {
            Headers = ["Status"],
            Rows = [new Dictionary<string, object?> { ["Status"] = "Inactive" }]
        };

        var result = await service.NormalizeAsync(data, new FakeHandler(), []);

        Assert.Equal("success", result.Status);
        Assert.Empty(result.Changes);
        Assert.Equal("Inactive", result.NormalizedData.Rows[0]["Status"]);
    }
}
