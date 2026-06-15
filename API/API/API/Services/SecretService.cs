using Microsoft.Extensions.Options;

namespace API.Services;

public interface ISecretService
{
    Task<string?> GetSecretAsync(string key);
    Task<bool> HasSecretAsync(string key);
}

public class EnvironmentSecretService : ISecretService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<EnvironmentSecretService> _logger;
    private readonly HashSet<string> _warnedKeys = new();

    private static readonly HashSet<string> KnownConfigurationKeys = new(StringComparer.OrdinalIgnoreCase)
    {
        "JwtSettings__SecretKey",
        "JwtSettings__Issuer",
        "JwtSettings__Audience",
        "ConnectionStrings__DefaultConnection",
        "MfaSettings__Issuer",
        "MfaSettings__AppName",
        "StepUp__SessionTimeoutMinutes",
        "EvidenceExport__SigningKey",
        "EnterpriseOperations__WorkerIntervalSeconds",
        "RateLimiting__Backend",
        "Redis__ConnectionString"
    };

    public EnvironmentSecretService(IConfiguration configuration, ILogger<EnvironmentSecretService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public Task<string?> GetSecretAsync(string key)
    {
        var value = _configuration[key]
            ?? _configuration[$"Secrets:{key}"]
            ?? Environment.GetEnvironmentVariable(key)
            ?? Environment.GetEnvironmentVariable(key.Replace("__", "_"));

        if (string.IsNullOrEmpty(value) && !_warnedKeys.Contains(key))
        {
            _warnedKeys.Add(key);
            _logger.LogWarning("Secret '{Key}' is not configured. Using fallback or default.", key);
        }

        return Task.FromResult(value);
    }

    public Task<bool> HasSecretAsync(string key)
    {
        var value = _configuration[key]
            ?? _configuration[$"Secrets:{key}"]
            ?? Environment.GetEnvironmentVariable(key)
            ?? Environment.GetEnvironmentVariable(key.Replace("__", "_"));

        return Task.FromResult(!string.IsNullOrEmpty(value));
    }
}
