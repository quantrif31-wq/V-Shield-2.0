using System.Diagnostics;
using System.Text.Json;

namespace API.Services;

public sealed class RuntimeOrchestrator
{
    private const string ManagedModeLegacy = "legacy_process";
    private const string ManagedModeExternal = "external_service";
    private readonly IConfiguration _configuration;
    private readonly IWebHostEnvironment _env;
    private readonly object _sync = new();

    public RuntimeOrchestrator(IConfiguration configuration, IWebHostEnvironment env)
    {
        _configuration = configuration;
        _env = env;
    }

    public IReadOnlyList<RuntimeServiceState> GetServices()
    {
        var configs = LoadConfigs();
        return configs.Select(ToState).ToList();
    }

    public RuntimeServiceState? GetService(string name)
    {
        var config = LoadConfigs().FirstOrDefault(x => x.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
        return config == null ? null : ToState(config);
    }

    public RuntimeServiceState? UpdateConfig(string name, bool? enabled, bool? autoStart)
    {
        var configs = LoadConfigs();
        var config = configs.FirstOrDefault(x => x.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
        if (config == null) return null;

        if (enabled.HasValue) config.Enabled = enabled.Value;
        if (autoStart.HasValue) config.AutoStart = autoStart.Value;
        config.UpdatedAt = DateTimeOffset.UtcNow;
        SaveConfigs(configs);
        return ToState(config);
    }

    public async Task EnsureAutoStartAsync(CancellationToken cancellationToken = default)
    {
        foreach (var service in LoadConfigs().Where(s => s.Enabled && s.AutoStart))
        {
            if (cancellationToken.IsCancellationRequested) return;
            await StartAsync(service.Name);
        }
    }

    public async Task<RuntimeActionResult> StartAsync(string name)
    {
        var service = LoadConfigs().FirstOrDefault(x => x.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
        if (service == null) return RuntimeActionResult.Fail($"Khong tim thay service {name}.");
        if (!service.Enabled) return RuntimeActionResult.Fail($"Service {name} dang bi tat (Enabled=false).");
        return await Task.FromResult(RuntimeActionResult.Ok($"Dịch vụ {name} được quản lý bởi Docker Compose."));
    }

    public async Task<RuntimeActionResult> StopAsync(string name)
    {
        return await Task.FromResult(RuntimeActionResult.Ok($"Dịch vụ {name} được quản lý bởi Docker Compose."));
    }

    private RuntimeServiceState ToState(RuntimeServiceConfig config)
    {
        return new RuntimeServiceState
        {
            Name = config.Name,
            DisplayName = config.DisplayName,
            Enabled = config.Enabled,
            AutoStart = config.AutoStart,
            ManagedMode = ManagedModeExternal,
            Running = config.Enabled,
            UpdatedAt = config.UpdatedAt
        };
    }

    private List<RuntimeServiceConfig> LoadConfigs()
    {
        lock (_sync)
        {
            var path = GetConfigFilePath();
            var defaults = GetDefaultConfigs();
            if (!File.Exists(path))
            {
                SaveConfigs(defaults);
                return defaults;
            }

            try
            {
                var json = File.ReadAllText(path);
                var loaded = JsonSerializer.Deserialize<List<RuntimeServiceConfig>>(json) ?? new();

                foreach (var d in defaults)
                {
                    var existing = loaded.FirstOrDefault(x => x.Name.Equals(d.Name, StringComparison.OrdinalIgnoreCase));
                    if (existing == null) loaded.Add(d);
                }

                SaveConfigs(loaded);
                return loaded;
            }
            catch
            {
                SaveConfigs(defaults);
                return defaults;
            }
        }
    }

    private void SaveConfigs(List<RuntimeServiceConfig> configs)
    {
        lock (_sync)
        {
            var path = GetConfigFilePath();
            var dir = Path.GetDirectoryName(path)!;
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

            var json = JsonSerializer.Serialize(configs, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = true
            });
            File.WriteAllText(path, json);
        }
    }

    private string GetConfigFilePath() =>
        Path.Combine(_env.ContentRootPath, ".runtime", "runtime-services.json");

    private string ResolveAiRootFolderName() =>
        _configuration["RuntimePaths:AiRootFolderName"] ?? "AI_Project";

    private bool IsDockerMode() => true;

    private static List<RuntimeServiceConfig> GetDefaultConfigs() =>
        new()
        {
            new RuntimeServiceConfig("python_qr", "Python doc QR", true, false, ManagedModeExternal),
            new RuntimeServiceConfig("python_plate", "Python doc bien so", true, false, ManagedModeExternal),
            new RuntimeServiceConfig("go2rtc", "Go2RTC", true, true, ManagedModeExternal),
            new RuntimeServiceConfig("cloudflared", "Cloudflared", true, true, ManagedModeExternal),
        };
}

public sealed class RuntimeServiceConfig
{
    public RuntimeServiceConfig(string name, string displayName, bool enabled, bool autoStart, string managedMode)
    {
        Name = name;
        DisplayName = displayName;
        Enabled = enabled;
        AutoStart = autoStart;
        ManagedMode = managedMode;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public string Name { get; set; }
    public string DisplayName { get; set; }
    public bool Enabled { get; set; }
    public bool AutoStart { get; set; }
    public string ManagedMode { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}

public sealed class RuntimeServiceState
{
    public string Name { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public bool Enabled { get; set; }
    public bool AutoStart { get; set; }
    public string ManagedMode { get; set; } = string.Empty;
    public bool Running { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}

public sealed class RuntimeActionResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;

    public static RuntimeActionResult Ok(string message) => new() { Success = true, Message = message };
    public static RuntimeActionResult Fail(string message) => new() { Success = false, Message = message };
}
