using System.Net;
using System.Text;
using System.Text.Json;
using API.Services.FaceRecognition;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace API.Tests;

public sealed class FaceRecognitionClientTests
{
    [Theory]
    [InlineData("http://face-runtime:5001", "http://face-runtime:5001/api/camera/status")]
    [InlineData("http://face-runtime:5001/", "http://face-runtime:5001/api/camera/status")]
    [InlineData("http://face-runtime:5001/api", "http://face-runtime:5001/api/camera/status")]
    [InlineData("http://face-runtime:5001/api/", "http://face-runtime:5001/api/camera/status")]
    public async Task BaseUrlVariants_CreateTheSameRequest(string configuredUrl, string expectedUrl)
    {
        var handler = RecordingHandler.Returning(HttpStatusCode.OK, "{}");
        var client = CreateClient(handler, configuredUrl);

        await client.GetCameraStatusAsync(CancellationToken.None);

        Assert.Equal(expectedUrl, Assert.Single(handler.Requests).Uri);
    }

    [Fact]
    public async Task CameraOn_UsesPostPathAndPreservesJsonBody()
    {
        var handler = RecordingHandler.Returning(HttpStatusCode.OK, "{}");
        var client = CreateClient(handler);
        const string cameraUrl = "http://camera.local/video?profile=main";

        await client.StartCameraAsync(
            new FaceCameraStartRequest { Ip = cameraUrl },
            CancellationToken.None);

        var request = Assert.Single(handler.Requests);
        Assert.Equal(HttpMethod.Post, request.Method);
        Assert.Equal("http://face.test/api/camera/on", request.Uri);
        using var json = JsonDocument.Parse(Assert.IsType<string>(request.Body));
        Assert.Equal(cameraUrl, json.RootElement.GetProperty("ip").GetString());
    }

    [Theory]
    [InlineData("off", "http://face.test/api/camera/off")]
    [InlineData("reset", "http://face.test/api/camera/reset")]
    public async Task CameraPostWithoutBody_UsesExpectedPath(string operation, string expectedUrl)
    {
        var handler = RecordingHandler.Returning(HttpStatusCode.OK, "{}");
        var client = CreateClient(handler);

        if (operation == "off")
        {
            await client.StopCameraAsync(CancellationToken.None);
        }
        else
        {
            await client.ResetCameraAsync(CancellationToken.None);
        }

        var request = Assert.Single(handler.Requests);
        Assert.Equal(HttpMethod.Post, request.Method);
        Assert.Equal(expectedUrl, request.Uri);
        Assert.Null(request.Body);
    }

    [Theory]
    [InlineData("status", "http://face.test/api/camera/status")]
    [InlineData("result", "http://face.test/api/camera/result")]
    [InlineData("locked", "http://face.test/api/camera/locked-images")]
    [InlineData("models", "http://face.test/api/models")]
    public async Task GetOperations_UseExpectedPath(string operation, string expectedUrl)
    {
        var handler = RecordingHandler.Returning(HttpStatusCode.OK, "{}");
        var client = CreateClient(handler);

        _ = operation switch
        {
            "status" => await client.GetCameraStatusAsync(CancellationToken.None),
            "result" => await client.GetRecognitionResultAsync(CancellationToken.None),
            "locked" => await client.GetLockedImagesAsync(CancellationToken.None),
            _ => await client.GetModelsAsync(CancellationToken.None)
        };

        var request = Assert.Single(handler.Requests);
        Assert.Equal(HttpMethod.Get, request.Method);
        Assert.Equal(expectedUrl, request.Uri);
    }

    [Fact]
    public async Task Reload_UsesPostPathWithoutBody()
    {
        var handler = RecordingHandler.Returning(HttpStatusCode.OK, "{}");
        var client = CreateClient(handler);

        await client.ReloadModelsAsync(CancellationToken.None);

        var request = Assert.Single(handler.Requests);
        Assert.Equal(HttpMethod.Post, request.Method);
        Assert.Equal("http://face.test/api/models/reload", request.Uri);
        Assert.Null(request.Body);
    }

    [Theory]
    [InlineData(HttpStatusCode.OK)]
    [InlineData(HttpStatusCode.BadRequest)]
    [InlineData(HttpStatusCode.Conflict)]
    [InlineData(HttpStatusCode.UnprocessableEntity)]
    [InlineData(HttpStatusCode.InternalServerError)]
    public async Task PythonStatusAndBody_AreReturnedUnchanged(HttpStatusCode status)
    {
        const string body = """{"success":false,"value":1.00}""";
        var handler = RecordingHandler.Returning(status, body);
        var client = CreateClient(handler);

        var response = await client.GetModelsAsync(CancellationToken.None);

        Assert.Equal(status, response.StatusCode);
        Assert.Equal(body, response.Body);
    }

    [Theory]
    [InlineData("")]
    [InlineData("not-json")]
    public async Task EmptyOrInvalidJsonBody_IsReturnedUnchanged(string body)
    {
        var handler = RecordingHandler.Returning(HttpStatusCode.OK, body);
        var client = CreateClient(handler);

        var response = await client.GetCameraStatusAsync(CancellationToken.None);

        Assert.Equal(body, response.Body);
    }

    [Fact]
    public async Task ContentType_IsReturnedUnchanged()
    {
        var handler = new RecordingHandler((_, _) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("plain", Encoding.UTF8, "text/plain")
            }));
        var client = CreateClient(handler);

        var response = await client.GetModelsAsync(CancellationToken.None);

        Assert.Equal("text/plain; charset=utf-8", response.ContentType);
    }

    [Fact]
    public async Task ConnectionFailure_IsClassified()
    {
        var handler = new RecordingHandler((_, _) =>
            Task.FromException<HttpResponseMessage>(
                new HttpRequestException("simulated connection refused")));
        var client = CreateClient(handler);

        var exception = await Assert.ThrowsAsync<FaceRuntimeUnavailableException>(
            () => client.GetCameraStatusAsync(CancellationToken.None));

        Assert.Equal(FaceRuntimeFailureKind.ConnectionFailure, exception.FailureKind);
        Assert.Contains("simulated connection refused", exception.Message);
    }

    [Fact]
    public async Task UnexpectedFailure_IsSanitized()
    {
        var handler = new RecordingHandler((_, _) =>
            Task.FromException<HttpResponseMessage>(
                new InvalidOperationException("secret technical detail")));
        var client = CreateClient(handler);

        var exception = await Assert.ThrowsAsync<FaceRuntimeUnavailableException>(
            () => client.GetCameraStatusAsync(CancellationToken.None));

        Assert.Equal(FaceRuntimeFailureKind.UnexpectedFailure, exception.FailureKind);
        Assert.DoesNotContain("secret technical detail", exception.Message);
    }

    [Fact]
    public async Task HttpClientTimeout_IsClassified()
    {
        var handler = new RecordingHandler(async (_, cancellationToken) =>
        {
            await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
            return new HttpResponseMessage(HttpStatusCode.OK);
        });
        var client = CreateClient(handler, timeout: TimeSpan.FromMilliseconds(20));

        var exception = await Assert.ThrowsAsync<FaceRuntimeUnavailableException>(
            () => client.GetCameraStatusAsync(CancellationToken.None));

        Assert.Equal(FaceRuntimeFailureKind.Timeout, exception.FailureKind);
    }

    [Fact]
    public async Task CallerCancellation_PropagatesWithoutUnavailableConversion()
    {
        var handler = new RecordingHandler(async (_, cancellationToken) =>
        {
            await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
            return new HttpResponseMessage(HttpStatusCode.OK);
        });
        var client = CreateClient(handler);
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => client.GetCameraStatusAsync(cancellation.Token));
    }

    [Fact]
    public async Task CancellationToken_ReachesHandler()
    {
        var tokenObserved = false;
        var handler = new RecordingHandler((_, cancellationToken) =>
        {
            tokenObserved = cancellationToken.CanBeCanceled;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{}")
            });
        });
        var client = CreateClient(handler);
        using var cancellation = new CancellationTokenSource();

        await client.GetModelsAsync(cancellation.Token);

        Assert.True(tokenObserved);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("relative/path")]
    [InlineData("ftp://face.test/api")]
    public void InvalidBaseUrl_FailsFastAndNamesConfigurationKey(string? value)
    {
        var configuration = Configuration(value, null);

        var exception = Assert.Throws<InvalidOperationException>(
            () => FaceRecognitionClientOptions.FromConfiguration(configuration));

        Assert.Contains(FaceRecognitionClientOptions.BaseUrlConfigurationKey, exception.Message);
    }

    [Theory]
    [InlineData("0")]
    [InlineData("-1")]
    [InlineData("not-a-number")]
    public void InvalidTimeout_FailsFastAndNamesConfigurationKey(string value)
    {
        var configuration = Configuration("http://face.test/api", value);

        var exception = Assert.Throws<InvalidOperationException>(
            () => FaceRecognitionClientOptions.FromConfiguration(configuration));

        Assert.Contains(FaceRecognitionClientOptions.TimeoutConfigurationKey, exception.Message);
    }

    [Fact]
    public void MissingTimeout_UsesCompatibleHttpClientDefault()
    {
        var options = FaceRecognitionClientOptions.FromConfiguration(
            Configuration("http://face.test/api", null));

        Assert.Equal(TimeSpan.FromSeconds(100), options.Timeout);
    }

    private static FaceRecognitionClient CreateClient(
        RecordingHandler handler,
        string configuredUrl = "http://face.test/api",
        TimeSpan? timeout = null)
    {
        var httpClient = new HttpClient(handler)
        {
            BaseAddress = FaceRecognitionClientOptions.NormalizeBaseAddress(configuredUrl),
            Timeout = timeout ?? TimeSpan.FromSeconds(100)
        };
        return new FaceRecognitionClient(httpClient);
    }

    private static IConfiguration Configuration(string? baseUrl, string? timeout)
    {
        var values = new Dictionary<string, string?>();
        if (baseUrl != null)
        {
            values[FaceRecognitionClientOptions.BaseUrlConfigurationKey] = baseUrl;
        }
        if (timeout != null)
        {
            values[FaceRecognitionClientOptions.TimeoutConfigurationKey] = timeout;
        }

        return new ConfigurationBuilder().AddInMemoryCollection(values).Build();
    }

    private sealed class RecordingHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> _send;

        public RecordingHandler(
            Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> send)
        {
            _send = send;
        }

        public List<RecordedRequest> Requests { get; } = [];

        public static RecordingHandler Returning(HttpStatusCode status, string body) =>
            new((_, _) => Task.FromResult(new HttpResponseMessage(status)
            {
                Content = new StringContent(body, Encoding.UTF8, "application/json")
            }));

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            Requests.Add(new RecordedRequest(
                request.Method,
                request.RequestUri!.ToString(),
                request.Content == null
                    ? null
                    : await request.Content.ReadAsStringAsync(cancellationToken)));
            return await _send(request, cancellationToken);
        }
    }

    private sealed record RecordedRequest(HttpMethod Method, string Uri, string? Body);
}
