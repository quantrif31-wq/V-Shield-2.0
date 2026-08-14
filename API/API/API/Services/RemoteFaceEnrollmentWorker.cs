using System.Net.Http.Json;
using System.Text.Json;
using API.Services.FaceRecognition;
using API.Services.Sync;
using Microsoft.Extensions.Options;

namespace API.Services;

/// <summary>
/// Local node worker: định kỳ gọi VPS claim job đăng ký Face ID từ xa, chạy AI
/// (Face Runtime) trên ảnh của khách, rồi báo kết quả về VPS (complete/fail).
/// Chỉ hoạt động khi Sync.Mode = AreaNode và có CentralBaseUrl.
/// </summary>
public class RemoteFaceEnrollmentWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<RemoteFaceEnrollmentWorker> _logger;
    private readonly SyncRuntimeOptions _options;

    public RemoteFaceEnrollmentWorker(
        IServiceScopeFactory scopeFactory,
        IHttpClientFactory httpClientFactory,
        ILogger<RemoteFaceEnrollmentWorker> logger,
        IOptions<SyncRuntimeOptions> options)
    {
        _scopeFactory = scopeFactory;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        _options = options.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!string.Equals(_options.Mode, SyncRuntimeModes.AreaNode, StringComparison.OrdinalIgnoreCase) ||
            string.IsNullOrWhiteSpace(_options.CentralBaseUrl) ||
            string.IsNullOrWhiteSpace(_options.LocalAreaNodeId))
        {
            return;
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessOneBatchAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Remote face enrollment worker cycle failed.");
            }

            await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
        }
    }

    private async Task ProcessOneBatchAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<API.Data.ApplicationDbContext>();
        var faceClient = scope.ServiceProvider.GetRequiredService<IFaceRecognitionClient>();
        var configStore = scope.ServiceProvider.GetRequiredService<SyncSystemConfigStore>();

        var nodeSecret = await configStore.GetNodeSecretAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(nodeSecret))
        {
            // Node chưa đăng ký; AreaNodeSyncWorker sẽ xử lý đăng ký.
            return;
        }

        var client = _httpClientFactory.CreateClient();

        // Đồng bộ template Face ID từ VPS về model dir + reload registry.
        await SyncTemplatesAsync(client, nodeSecret, scope, cancellationToken);

        // Claim + xử lý job đăng ký từ xa.
        await ClaimAndProcessAsync(client, nodeSecret, scope, faceClient, cancellationToken);
    }

    private async Task SyncTemplatesAsync(
        HttpClient client, string nodeSecret, IServiceScope scope,
        CancellationToken cancellationToken)
    {
        try
        {
            var storage = scope.ServiceProvider.GetRequiredService<IFaceStoragePathResolver>();
            var faceClient = scope.ServiceProvider.GetRequiredService<IFaceRecognitionClient>();

            var request = CreateNodeRequest(HttpMethod.Get, "/api/sync/face-enrollment/templates", nodeSecret);
            using var response = await client.SendAsync(request, cancellationToken);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            if (string.IsNullOrWhiteSpace(json) || json == "[]")
            {
                return;
            }

            using var document = JsonDocument.Parse(json);
            var changed = false;
            Directory.CreateDirectory(storage.ModelActiveDir);

            foreach (var item in document.RootElement.EnumerateArray())
            {
                var employeeId = item.GetProperty("employeeId").GetInt32();
                var modelFileName = item.GetProperty("modelFileName").GetString();
                var templateContent = item.GetProperty("templateContent").GetString();
                if (string.IsNullOrWhiteSpace(modelFileName) || string.IsNullOrWhiteSpace(templateContent))
                {
                    continue;
                }

                // Ghi template mới; bỏ các template cũ của cùng employee.
                var targetPath = Path.Combine(storage.ModelActiveDir, modelFileName);
                if (File.Exists(targetPath))
                {
                    continue;
                }

                // Xoá các file cũ của employee này (emp_{id}_v*).
                foreach (var oldFile in Directory.GetFiles(storage.ModelActiveDir, $"emp_{employeeId}_v*.json"))
                {
                    try { File.Delete(oldFile); } catch { }
                }

                await File.WriteAllTextAsync(targetPath, templateContent, cancellationToken);
                changed = true;
            }

            if (changed)
            {
                await faceClient.ReloadModelsAsync(cancellationToken);
                _logger.LogInformation("Reloaded Face Runtime models after syncing templates.");
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to sync face templates from central.");
        }
    }

    private async Task ClaimAndProcessAsync(
        HttpClient client, string nodeSecret, IServiceScope scope,
        IFaceRecognitionClient faceClient, CancellationToken cancellationToken)
    {
        var claimRequest = CreateNodeRequest(HttpMethod.Post, "/api/sync/face-enrollment/claim-next", nodeSecret);
        using var claimResponse = await client.SendAsync(claimRequest, cancellationToken);
        claimResponse.EnsureSuccessStatusCode();

        var claimJson = await claimResponse.Content.ReadAsStringAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(claimJson) || claimJson == "null")
        {
            return;
        }

        using var document = JsonDocument.Parse(claimJson);
        var root = document.RootElement;
        var jobId = root.GetProperty("jobId").GetGuid();
        var employeeId = root.GetProperty("employeeId").GetInt32();
        var frames = root.GetProperty("frames").EnumerateArray()
            .Select(item => item.GetString() ?? string.Empty)
            .Where(item => !string.IsNullOrWhiteSpace(item))
            .ToList();

        if (frames.Count == 0)
        {
            await FailAsync(client, jobId, nodeSecret, "NoFrames", "Job không có ảnh.", cancellationToken);
            return;
        }

        try
        {
            var response = await faceClient.LiveEnrollAsync(employeeId.ToString(), frames, cancellationToken);
            if ((int)response.StatusCode is < 200 or >= 300)
            {
                await FailAsync(client, jobId, nodeSecret, $"EnrollHttp{(int)response.StatusCode}",
                    response.Body?.Length > 500 ? response.Body[..500] : response.Body, cancellationToken);
                return;
            }

            var payload = ParseJson(response.Body);
            var modelFileName = GetString(payload, "modelFileName");
            var checksum = GetString(payload, "checksum");
            var encodingCount = GetInt(payload, "encodingCount");

            if (string.IsNullOrWhiteSpace(modelFileName))
            {
                await FailAsync(client, jobId, nodeSecret, "NoModelFileName",
                    "Face Runtime không trả về tên file model.", cancellationToken);
                return;
            }

            // Đọc nội dung template JSON từ thư mục model active của Face Runtime
            // (cùng volume được mount vào API container).
            var storage = scope.ServiceProvider.GetRequiredService<IFaceStoragePathResolver>();
            var templatePath = Path.Combine(storage.ModelActiveDir, modelFileName);
            var templateContent = File.Exists(templatePath)
                ? await File.ReadAllTextAsync(templatePath, cancellationToken)
                : null;

            var completeRequest = CreateNodeRequest(
                HttpMethod.Post,
                $"/api/sync/face-enrollment/{jobId}/complete",
                nodeSecret,
                new { modelFileName, checksum, encodingCount, templateContent });
            using var completeResponse = await client.SendAsync(completeRequest, cancellationToken);
            completeResponse.EnsureSuccessStatusCode();

            _logger.LogInformation("Completed remote face enrollment job {JobId} -> {ModelFileName}", jobId, modelFileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process remote face enrollment job {JobId}", jobId);
            try
            {
                await FailAsync(client, jobId, nodeSecret, "ProcessingError", ex.Message, cancellationToken);
            }
            catch
            {
                // best-effort
            }
        }
    }

    private async Task FailAsync(
        HttpClient client, Guid jobId, string nodeSecret, string code, string message,
        CancellationToken cancellationToken)
    {
        var request = CreateNodeRequest(
            HttpMethod.Post,
            $"/api/sync/face-enrollment/{jobId}/fail",
            nodeSecret,
            new { failureCode = code, failureMessage = message });
        using var response = await client.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    private HttpRequestMessage CreateNodeRequest(HttpMethod method, string relativePath, string nodeSecret, object? body = null)
    {
        var request = new HttpRequestMessage(method, $"{_options.CentralBaseUrl!.TrimEnd('/')}{relativePath}");
        request.Headers.Add("X-VShield-Node-Id", _options.LocalAreaNodeId!);
        request.Headers.Add("X-VShield-Node-Secret", nodeSecret);
        if (body != null)
        {
            request.Content = JsonContent.Create(body);
        }
        return request;
    }

    private static JsonElement? ParseJson(string? body)
    {
        if (string.IsNullOrWhiteSpace(body)) return null;
        try
        {
            using var doc = JsonDocument.Parse(body);
            return doc.RootElement.Clone();
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static string? GetString(JsonElement? element, string property)
    {
        if (element is { } el && el.ValueKind == JsonValueKind.Object &&
            el.TryGetProperty(property, out var value) &&
            value.ValueKind == JsonValueKind.String)
        {
            return value.GetString();
        }
        return null;
    }

    private static int? GetInt(JsonElement? element, string property)
    {
        if (element is { } el && el.ValueKind == JsonValueKind.Object &&
            el.TryGetProperty(property, out var value) &&
            value.ValueKind == JsonValueKind.Number && value.TryGetInt32(out var number))
        {
            return number;
        }
        return null;
    }
}
