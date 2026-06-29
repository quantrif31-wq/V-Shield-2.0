using API.Tests.LoadTesting;
using FluentAssertions;
using Xunit;

namespace API.Tests;

public class LoadTestEnvironmentTests
{
    [Fact]
    public void IsEnabled_ShouldReturnFalse_WhenVariableIsMissing()
    {
        using var _ = new EnvironmentVariableScope(
            (LoadTestEnvironment.EnableLoadTestsVariable, null));

        LoadTestEnvironment.IsEnabled().Should().BeFalse();
    }

    [Fact]
    public void IsEnabled_ShouldReturnTrue_WhenVariableIsTrueIgnoringCase()
    {
        using var _ = new EnvironmentVariableScope(
            (LoadTestEnvironment.EnableLoadTestsVariable, "TrUe"));

        LoadTestEnvironment.IsEnabled().Should().BeTrue();
    }

    [Fact]
    public void CreateConfiguration_ShouldUseDefaults_WhenVariablesAreMissingOrInvalid()
    {
        using var _ = new EnvironmentVariableScope(
            (LoadTestEnvironment.BaseUrlVariable, null),
            (LoadTestEnvironment.AdminTokenVariable, null),
            (LoadTestEnvironment.DurationSecondsVariable, "-5"),
            (LoadTestEnvironment.ConcurrencyVariable, "abc"),
            (LoadTestEnvironment.WarmUpSecondsVariable, "0"));

        var configuration = LoadTestEnvironment.CreateConfiguration(
            defaultDurationSeconds: 45,
            defaultConcurrency: 7);

        configuration.BaseUrl.Should().Be("http://localhost:5107");
        configuration.AuthToken.Should().BeEmpty();
        configuration.DefaultDurationSeconds.Should().Be(45);
        configuration.DefaultConcurrency.Should().Be(7);
        configuration.DefaultWarmUpSeconds.Should().Be(3);
    }

    [Fact]
    public void CreateConfiguration_ShouldUseEnvironmentOverrides_WhenVariablesAreValid()
    {
        using var _ = new EnvironmentVariableScope(
            (LoadTestEnvironment.BaseUrlVariable, "https://perf.local"),
            (LoadTestEnvironment.AdminTokenVariable, "token-123"),
            (LoadTestEnvironment.DurationSecondsVariable, "90"),
            (LoadTestEnvironment.ConcurrencyVariable, "25"),
            (LoadTestEnvironment.WarmUpSecondsVariable, "9"));

        var configuration = LoadTestEnvironment.CreateConfiguration(
            defaultDurationSeconds: 45,
            defaultConcurrency: 7);

        configuration.BaseUrl.Should().Be("https://perf.local");
        configuration.AuthToken.Should().Be("token-123");
        configuration.DefaultDurationSeconds.Should().Be(90);
        configuration.DefaultConcurrency.Should().Be(25);
        configuration.DefaultWarmUpSeconds.Should().Be(9);
    }

    [Fact]
    public void LoadTestFact_ShouldSkip_WhenOptInIsDisabled_AndRequiredVariablesAreMissing()
    {
        using var _ = new EnvironmentVariableScope(
            (LoadTestEnvironment.EnableLoadTestsVariable, null),
            (LoadTestEnvironment.BaseUrlVariable, null));

        var attribute = new LoadTestFactAttribute(
            "Seed data is required.",
            LoadTestEnvironment.BaseUrlVariable);

        attribute.Skip.Should().NotBeNull();
        attribute.Skip.Should().Contain("ENABLE_LOAD_TESTS=true");
        attribute.Skip.Should().Contain(LoadTestEnvironment.BaseUrlVariable);
        attribute.Skip.Should().Contain("Seed data is required.");
    }

    [Fact]
    public void LoadTestFact_ShouldNotSkip_WhenOptInIsEnabled_AndRequiredVariablesExist()
    {
        using var _ = new EnvironmentVariableScope(
            (LoadTestEnvironment.EnableLoadTestsVariable, "true"),
            (LoadTestEnvironment.BaseUrlVariable, "http://localhost:5107"),
            (LoadTestEnvironment.RefreshTokenVariable, "refresh-token"));

        var attribute = new LoadTestFactAttribute(
            "Should not appear when configuration is complete.",
            LoadTestEnvironment.BaseUrlVariable,
            LoadTestEnvironment.RefreshTokenVariable);

        attribute.Skip.Should().BeNull();
    }

    private sealed class EnvironmentVariableScope : IDisposable
    {
        private readonly List<(string Name, string? OriginalValue)> _originalValues = new();

        public EnvironmentVariableScope(params (string Name, string? Value)[] overrides)
        {
            foreach (var (name, value) in overrides)
            {
                _originalValues.Add((name, Environment.GetEnvironmentVariable(name)));
                Environment.SetEnvironmentVariable(name, value);
            }
        }

        public void Dispose()
        {
            foreach (var (name, originalValue) in _originalValues)
            {
                Environment.SetEnvironmentVariable(name, originalValue);
            }
        }
    }
}
