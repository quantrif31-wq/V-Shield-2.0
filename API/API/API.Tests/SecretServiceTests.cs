using API.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace API.Tests;

public sealed class EnvironmentSecretServiceTests
{
    [Fact]
    public async Task GetSecretAsync_ReadsFromConfiguration()
    {
        var config = new Mock<IConfiguration>();
        config.Setup(c => c["JwtSettings__SecretKey"]).Returns("cfg-value");

        var service = new EnvironmentSecretService(config.Object, NullLogger<EnvironmentSecretService>.Instance);

        var value = await service.GetSecretAsync("JwtSettings__SecretKey");
        Assert.Equal("cfg-value", value);
    }

    [Fact]
    public async Task GetSecretAsync_FallsBackToSecretsPrefix()
    {
        var config = new Mock<IConfiguration>();
        config.Setup(c => c["Secrets:TestKey"]).Returns("secrets-value");

        var service = new EnvironmentSecretService(config.Object, NullLogger<EnvironmentSecretService>.Instance);

        var value = await service.GetSecretAsync("TestKey");
        Assert.Equal("secrets-value", value);
    }

    [Fact]
    public async Task GetSecretAsync_ReturnsNullWhenNotConfigured()
    {
        var config = new Mock<IConfiguration>();
        var service = new EnvironmentSecretService(config.Object, NullLogger<EnvironmentSecretService>.Instance);

        var value = await service.GetSecretAsync("Missing_Key_Not_Configured");
        Assert.Null(value);
    }

    [Fact]
    public async Task GetSecretAsync_NormalizesDoubleUnderscoresForEnvironmentFallback()
    {
        var config = new Mock<IConfiguration>();
        var service = new EnvironmentSecretService(config.Object, NullLogger<EnvironmentSecretService>.Instance);

        var value = await service.GetSecretAsync("Some__Nested__Key");
        Assert.Null(value);
    }

    [Fact]
    public async Task HasSecretAsync_ReflectsConfigurationPresence()
    {
        var config = new Mock<IConfiguration>();
        config.Setup(c => c["Present"]).Returns("x");
        var service = new EnvironmentSecretService(config.Object, NullLogger<EnvironmentSecretService>.Instance);

        Assert.True(await service.HasSecretAsync("Present"));
        Assert.False(await service.HasSecretAsync("Absent"));
    }
}
