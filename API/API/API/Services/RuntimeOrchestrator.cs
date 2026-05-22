using System.Diagnostics;
using System.Text.Json;

namespace API.Services;

public sealed class RuntimeOrchestrator
{
    private const string ManagedModeLegacy = "legacy_process";
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

        try
        {
            var result = service.Name switch
            {
                "python_qr" => StartPythonScript("QR_Dong", "QR_Dong.py"),
                "python_plate" => StartPythonScript("doc_bien_gpu", "docbien.py"),
                "python_cam_gia_lap" => StartPythonScript("cam\\cam_gia_lap", "cam_gia_lap.py"),
                "go2rtc" => StartGo2Rtc(),
                "cloudflared" => StartCloudflared(),
                _ => RuntimeActionResult.Fail($"Service {name} chua duoc ho tro.")
            };
            return await Task.FromResult(result);
        }
        catch (Exception ex)
        {
            return RuntimeActionResult.Fail(ex.Message);
        }
    }

    public async Task<RuntimeActionResult> StopAsync(string name)
    {
        try
        {
            var result = name switch
            {
                "python_qr" => StopPythonScript("QR_Dong.py"),
                "python_plate" => StopPythonScript("docbien.py"),
                "python_cam_gia_lap" => StopPythonCamGiaLap(),
                "go2rtc" => KillProcesses("go2rtc"),
                "cloudflared" => KillProcesses("cloudflared"),
                _ => RuntimeActionResult.Fail($"Service {name} chua duoc ho tro.")
            };
            return await Task.FromResult(result);
        }
        catch (Exception ex)
        {
            return RuntimeActionResult.Fail(ex.Message);
        }
    }

    private RuntimeServiceState ToState(RuntimeServiceConfig config)
    {
        var running = config.Name switch
        {
            "python_qr" => IsPythonScriptRunning("QR_Dong.py"),
            "python_plate" => IsPythonScriptRunning("docbien.py"),
            "python_cam_gia_lap" => IsPythonScriptRunning("cam_gia_lap.py"),
            "go2rtc" => IsProcessRunning("go2rtc"),
            "cloudflared" => IsProcessRunning("cloudflared"),
            _ => false
        };

        return new RuntimeServiceState
        {
            Name = config.Name,
            DisplayName = config.DisplayName,
            Enabled = config.Enabled,
            AutoStart = config.AutoStart,
            ManagedMode = config.ManagedMode,
            Running = running,
            UpdatedAt = config.UpdatedAt
        };
    }

    private List<RuntimeServiceConfig> LoadConfigs()
    {
        lock (_sync)
        {
            var defaults = GetDefaultConfigs();
            var path = GetConfigFilePath();
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

    private static List<RuntimeServiceConfig> GetDefaultConfigs() =>
        new()
        {
            new RuntimeServiceConfig("python_qr", "Python doc QR", true, false, ManagedModeLegacy),
            new RuntimeServiceConfig("python_plate", "Python doc bien so", true, false, ManagedModeLegacy),
            new RuntimeServiceConfig("python_cam_gia_lap", "Python cam gia lap", true, false, ManagedModeLegacy),
            new RuntimeServiceConfig("go2rtc", "Go2RTC", true, true, ManagedModeLegacy),
            new RuntimeServiceConfig("cloudflared", "Cloudflared", true, true, ManagedModeLegacy),
        };

    private RuntimeActionResult StartGo2Rtc()
    {
        if (IsProcessRunning("go2rtc")) return RuntimeActionResult.Ok("go2rtc da dang chay.");

        var basePath = Directory.GetCurrentDirectory();
        var go2rtcPath = Path.GetFullPath(Path.Combine(basePath, "..", "..", "..", ResolveAiRootFolderName(), "cam", "go2rtc_win64"));
        var exePath = Path.Combine(go2rtcPath, "go2rtc.exe");
        if (!File.Exists(exePath)) return RuntimeActionResult.Fail("Khong tim thay go2rtc.exe");

        Process.Start(new ProcessStartInfo
        {
            FileName = exePath,
            WorkingDirectory = go2rtcPath,
            UseShellExecute = true
        });
        return RuntimeActionResult.Ok("Da bat go2rtc.");
    }

    private RuntimeActionResult StartCloudflared()
    {
        if (IsProcessRunning("cloudflared")) return RuntimeActionResult.Ok("cloudflared da dang chay.");

        var cloudflareDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".cloudflared");
        var configPath = Path.Combine(cloudflareDir, "config.yml");
        if (!File.Exists(configPath)) return RuntimeActionResult.Fail("Khong tim thay ~/.cloudflared/config.yml");

        Process.Start(new ProcessStartInfo
        {
            FileName = "cloudflared",
            Arguments = $"tunnel --config \"{configPath}\" run",
            UseShellExecute = true,
            CreateNoWindow = true
        });

        return RuntimeActionResult.Ok("Da bat cloudflared.");
    }

    private RuntimeActionResult StartPythonScript(string folderName, string scriptName)
    {
        if (IsPythonScriptRunning(scriptName))
        {
            return RuntimeActionResult.Ok($"{scriptName} da dang chay.");
        }

        var basePath = Directory.GetCurrentDirectory();
        var projectPath = Path.GetFullPath(Path.Combine(basePath, "..", "..", "..", ResolveAiRootFolderName(), folderName));
        var scriptPath = Path.Combine(projectPath, scriptName);
        if (!File.Exists(scriptPath)) return RuntimeActionResult.Fail($"Khong tim thay script {scriptPath}");

        var pythonExe = Path.Combine(projectPath, "venv", "Scripts", "python.exe");
        if (!File.Exists(pythonExe)) pythonExe = "python";

        var psi = new ProcessStartInfo
        {
            FileName = pythonExe,
            Arguments = $"\"{scriptPath}\"",
            WorkingDirectory = projectPath,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        Process.Start(psi);
        return RuntimeActionResult.Ok($"Da bat {scriptName}.");
    }

    private RuntimeActionResult StopPythonCamGiaLap()
    {
        try
        {
            foreach (var proc in Process.GetProcessesByName("mediamtx")) proc.Kill();
            foreach (var proc in Process.GetProcessesByName("ffmpeg")) proc.Kill();
        }
        catch
        {
            // ignore
        }

        return StopPythonScript("cam_gia_lap.py");
    }

    private RuntimeActionResult StopPythonScript(string scriptName)
    {
        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "powershell.exe",
                Arguments = $"-Command \"Get-WmiObject Win32_Process | Where-Object {{ $_.CommandLine -match '{scriptName}' -and $_.Name -eq 'python.exe' }} | ForEach-Object {{ $_.Terminate() }}\"",
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };
        process.Start();
        process.WaitForExit();
        return RuntimeActionResult.Ok($"Da tat {scriptName}.");
    }

    private static RuntimeActionResult KillProcesses(string name)
    {
        foreach (var proc in Process.GetProcessesByName(name))
        {
            proc.Kill();
        }
        return RuntimeActionResult.Ok($"Da tat {name}.");
    }

    private static bool IsProcessRunning(string processName) =>
        Process.GetProcessesByName(processName).Length > 0;

    private bool IsPythonScriptRunning(string scriptName)
    {
        try
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "powershell.exe",
                    Arguments = $"-Command \"@(Get-WmiObject Win32_Process | Where-Object {{ $_.CommandLine -match '{scriptName}' -and $_.Name -eq 'python.exe' }}).Count\"",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true
                }
            };
            process.Start();
            var output = process.StandardOutput.ReadToEnd();
            process.WaitForExit();
            return int.TryParse(output.Trim(), out var count) && count > 0;
        }
        catch
        {
            return false;
        }
    }
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
