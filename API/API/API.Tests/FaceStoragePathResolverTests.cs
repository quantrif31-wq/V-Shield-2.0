using API.Services;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Options;
using Xunit;

namespace API.Tests;

public sealed class FaceStoragePathResolverTests : IDisposable
{
    private readonly string _root =
        Path.Combine(Path.GetTempPath(), "vshield-face-storage-tests", Guid.NewGuid().ToString("N"));

    [Fact]
    public void ConfiguredInputRootIsUsedForVideoDirectories()
    {
        var resolver = CreateResolver(_root);

        resolver.InputRoot.Should().Be(Path.GetFullPath(_root));
        resolver.ResolveDirectory("video_notok")
            .Should().Be(Path.Combine(Path.GetFullPath(_root), "video_notok"));
        resolver.ResolveDirectory("video_ok")
            .Should().Be(Path.Combine(Path.GetFullPath(_root), "video_ok"));
    }

    [Theory]
    [InlineData("../escape.mp4")]
    [InlineData("folder/escape.mp4")]
    [InlineData("folder\\escape.mp4")]
    public void FilenameTraversalIsRejected(string fileName)
    {
        var resolver = CreateResolver(_root);

        var action = () => resolver.ResolveFile("video_notok", fileName);

        action.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void ExistingPublicContractDoesNotExposeConfiguredAbsoluteRoot()
    {
        const string fileName = "emp_1_20260728010101001.mp4";
        var publicPath = "/uploads/VideoFace/video_notok/" + fileName;

        publicPath.Should().Be("/uploads/VideoFace/video_notok/emp_1_20260728010101001.mp4");
        publicPath.Should().NotContain(_root);
    }

    private static FaceStoragePathResolver CreateResolver(string root)
    {
        var environment = new TestWebHostEnvironment
        {
            ContentRootPath = Path.GetTempPath(),
            WebRootPath = Path.Combine(Path.GetTempPath(), "wwwroot")
        };
        return new FaceStoragePathResolver(
            Options.Create(new FaceStorageOptions { InputRoot = root }),
            environment);
    }

    public void Dispose()
    {
        if (Directory.Exists(_root))
            Directory.Delete(_root, recursive: true);
    }

    private sealed class TestWebHostEnvironment : IWebHostEnvironment
    {
        public string ApplicationName { get; set; } = "API.Tests";
        public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
        public string WebRootPath { get; set; } = string.Empty;
        public string EnvironmentName { get; set; } = "Testing";
        public string ContentRootPath { get; set; } = string.Empty;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
