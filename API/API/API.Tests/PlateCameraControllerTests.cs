using System.Net;
using API.Controllers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Moq;
using Xunit;

namespace API.Tests;

public sealed class PlateCameraControllerTests
{
    private sealed class StubHandler : HttpMessageHandler
    {
        public List<(string Method, string Path)> Requests { get; } = [];
        public Func<HttpRequestMessage, HttpResponseMessage> Responder { get; set; } =
            _ => new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("{\"ok\":true}") };

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Requests.Add((request.Method.Method, request.RequestUri!.PathAndQuery));
            return Task.FromResult(Responder(request));
        }
    }

    private sealed class FakeHttpClientFactory : IHttpClientFactory
    {
        public HttpClient Client { get; set; } = null!;
        public HttpClient CreateClient(string name) => Client;
    }

    private static (PlateCameraController Controller, StubHandler Handler) Create(
        string? baseUrl = null)
    {
        var handler = new StubHandler();
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["AiServices:PlateBaseUrl"] = baseUrl ?? "http://plate:5002/api/"
            })
            .Build();
        var factory = new FakeHttpClientFactory { Client = new HttpClient(handler) };
        var controller = new PlateCameraController(factory, config);
        return (controller, handler);
    }

    [Fact]
    public async Task TurnOnCamera_ProxiesPostWithBody()
    {
        var (controller, handler) = Create();
        var result = await controller.TurnOnCamera(new PlateCameraController.CameraOnRequest { Ip = "192.168.1.5" });

        var content = Assert.IsType<ContentResult>(result);
        Assert.Equal(200, content.StatusCode);
        Assert.Equal("{\"ok\":true}", content.Content);
        Assert.Contains(handler.Requests, r => r.Method == "POST" && r.Path.EndsWith("/camera/on"));
    }

    [Fact]
    public async Task TurnOffCamera_ProxiesPost()
    {
        var (controller, handler) = Create();
        await controller.TurnOffCamera();
        Assert.Contains(handler.Requests, r => r.Method == "POST" && r.Path.EndsWith("/camera/off"));
    }

    [Fact]
    public async Task ResetCameraState_ProxiesPost()
    {
        var (controller, handler) = Create();
        await controller.ResetCameraState();
        Assert.Contains(handler.Requests, r => r.Method == "POST" && r.Path.EndsWith("/camera/reset"));
    }

    [Fact]
    public async Task GetCameraStatus_ProxiesGet()
    {
        var (controller, handler) = Create();
        await controller.GetCameraStatus();
        Assert.Contains(handler.Requests, r => r.Method == "GET" && r.Path.EndsWith("/camera/status"));
    }

    [Fact]
    public async Task GetCameraResult_ProxiesGet()
    {
        var (controller, handler) = Create();
        await controller.GetCameraResult();
        Assert.Contains(handler.Requests, r => r.Method == "GET" && r.Path.EndsWith("/camera/result"));
    }

    [Fact]
    public async Task GetLockedImages_ProxiesGet()
    {
        var (controller, handler) = Create();
        await controller.GetLockedImages();
        Assert.Contains(handler.Requests, r => r.Method == "GET" && r.Path.EndsWith("/camera/locked-images"));
    }

    [Fact]
    public async Task DownstreamError_Returns503()
    {
        var handler = new StubHandler
        {
            Responder = _ => throw new HttpRequestException("connection refused")
        };
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["AiServices:PlateBaseUrl"] = "http://plate:5002/api/"
        }).Build();
        var factory = new FakeHttpClientFactory { Client = new HttpClient(handler) };
        var controller = new PlateCameraController(factory, config);

        var result = await controller.GetCameraStatus();

        var status = Assert.IsType<ObjectResult>(result);
        Assert.Equal(503, status.StatusCode);
    }

    [Fact]
    public void BaseUrl_WithoutConfiguredValue_UsesFallback()
    {
        var (controller, handler) = Create(baseUrl: null);
        _ = controller.GetCameraStatus().Result;
        Assert.Contains(handler.Requests, r => r.Path.EndsWith("/camera/status"));
    }
}