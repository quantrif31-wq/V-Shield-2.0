using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using API.Data;
using API.Models;
using Microsoft.EntityFrameworkCore;

namespace API.Services.FaceRecognition;

public sealed class FaceCameraConfigurationService :
    IFaceCameraConfigurationService,
    IFaceCameraConfigurationStore
{
    private static readonly HashSet<string> SupportedSchemes =
        new(StringComparer.OrdinalIgnoreCase) { "rtsp", "rtsps", "http", "https" };

    private readonly ApplicationDbContext _db;
    private readonly IFaceRecognitionClient _runtime;

    public FaceCameraConfigurationService(
        ApplicationDbContext db,
        IFaceRecognitionClient runtime)
    {
        _db = db;
        _runtime = runtime;
    }

    public async Task<FaceCameraConfigurationOverviewDto> GetOverviewAsync(
        CancellationToken cancellationToken)
    {
        var configurations = await LoadManagedAsync(cancellationToken);
        var inventory = await GetRuntimeInventoryAsync(cancellationToken);
        var managedIds = configurations.Select(x => x.RuntimeCameraId)
            .ToHashSet(StringComparer.Ordinal);

        var mapped = configurations.Select(configuration =>
        {
            inventory.Sessions.TryGetValue(configuration.RuntimeCameraId, out var session);
            return ToDto(configuration, session, inventory.Available);
        }).ToList();

        var unmanaged = inventory.Sessions.Values
            .Where(session => !managedIds.Contains(session.CameraId))
            .Select(session => new FaceCameraRuntimeSessionDto(
                session.CameraId,
                session.LaneId,
                session.Enabled,
                session.Connected,
                "Unmanaged",
                false))
            .ToList();

        return new FaceCameraConfigurationOverviewDto(mapped, unmanaged, inventory.Available);
    }

    public async Task<FaceCameraConfigurationDto?> GetAsync(
        string runtimeCameraId,
        CancellationToken cancellationToken)
    {
        var validId = FaceCameraIdValidator.Validate(runtimeCameraId);
        var configuration = await QueryManaged()
            .SingleOrDefaultAsync(x => x.RuntimeCameraId == validId, cancellationToken);
        if (configuration is null)
        {
            return null;
        }

        await RefreshConfigurationVersionAsync(configuration, cancellationToken);
        var inventory = await GetRuntimeInventoryAsync(cancellationToken);
        inventory.Sessions.TryGetValue(validId, out var session);
        return ToDto(configuration, session, inventory.Available);
    }

    public async Task<FaceCameraConfigurationDto> UpsertAsync(
        string runtimeCameraId,
        UpdateFaceCameraConfigurationRequest request,
        CancellationToken cancellationToken)
    {
        var validId = FaceCameraIdValidator.Validate(runtimeCameraId);
        var camera = await _db.Cameras.FindAsync([request.CameraId], cancellationToken)
            ?? throw new KeyNotFoundException("Camera does not exist.");
        ValidateStreamUrl(camera.StreamUrl);

        if (request.LaneId.HasValue &&
            !await _db.Lanes.AnyAsync(x => x.LaneId == request.LaneId.Value, cancellationToken))
        {
            throw new KeyNotFoundException("Lane does not exist.");
        }

        var cameraAlreadyUsed = await _db.FaceCameraConfigurations
            .AnyAsync(x => x.CameraId == request.CameraId && x.RuntimeCameraId != validId, cancellationToken);
        if (cameraAlreadyUsed)
        {
            throw new InvalidOperationException("Camera already has a Face ID configuration.");
        }

        var configuration = await QueryManaged()
            .SingleOrDefaultAsync(x => x.RuntimeCameraId == validId, cancellationToken);
        var now = DateTime.UtcNow;
        if (configuration is null)
        {
            configuration = new FaceCameraConfiguration
            {
                CameraId = request.CameraId,
                RuntimeCameraId = validId,
                LaneId = request.LaneId,
                AutoRestore = request.AutoRestore,
                DesiredState = FaceCameraDesiredStates.Stopped,
                ConfigurationVersion = 1,
                LastSyncStatus = FaceCameraSyncStatuses.Pending,
                CreatedAtUtc = now,
                UpdatedAtUtc = now,
                Camera = camera,
                ConfigurationFingerprint = Fingerprint(validId, camera.StreamUrl!, request.LaneId)
            };
            _db.FaceCameraConfigurations.Add(configuration);
        }
        else
        {
            ApplyConcurrencyToken(configuration, request.RowVersion);
            var fingerprint = Fingerprint(validId, camera.StreamUrl!, request.LaneId);
            if (!string.Equals(configuration.ConfigurationFingerprint, fingerprint, StringComparison.Ordinal))
            {
                configuration.ConfigurationVersion++;
                configuration.ConfigurationFingerprint = fingerprint;
                configuration.LastSyncStatus = FaceCameraSyncStatuses.Pending;
            }
            configuration.CameraId = request.CameraId;
            configuration.Camera = camera;
            configuration.LaneId = request.LaneId;
            configuration.AutoRestore = request.AutoRestore;
            configuration.UpdatedAtUtc = now;
        }

        await _db.SaveChangesAsync(cancellationToken);
        if (request.LaneId.HasValue)
        {
            await _db.Entry(configuration).Reference(x => x.Lane).LoadAsync(cancellationToken);
        }
        return ToDto(configuration, null, false);
    }

    public Task<FaceCameraDesiredStateDto> StartAsync(
        string runtimeCameraId,
        CancellationToken cancellationToken) =>
        SetDesiredStateAsync(runtimeCameraId, true, cancellationToken);

    public Task<FaceCameraDesiredStateDto> StopAsync(
        string runtimeCameraId,
        CancellationToken cancellationToken) =>
        SetDesiredStateAsync(runtimeCameraId, false, cancellationToken);

    public Task<List<FaceCameraConfiguration>> LoadManagedAsync(
        CancellationToken cancellationToken) =>
        QueryManaged().OrderBy(x => x.RuntimeCameraId).ToListAsync(cancellationToken);

    public async Task<FaceRuntimeInventory> GetRuntimeInventoryAsync(
        CancellationToken cancellationToken)
    {
        try
        {
            var response = await _runtime.GetCamerasAsync(cancellationToken);
            if (!response.StatusCode.IsSuccess())
            {
                return new FaceRuntimeInventory(false, new Dictionary<string, FaceRuntimeSession>());
            }
            return new FaceRuntimeInventory(true, ParseSessions(response.Body));
        }
        catch (FaceRuntimeUnavailableException)
        {
            return new FaceRuntimeInventory(false, new Dictionary<string, FaceRuntimeSession>());
        }
    }

    public async Task RefreshConfigurationVersionAsync(
        FaceCameraConfiguration configuration,
        CancellationToken cancellationToken)
    {
        ValidateStreamUrl(configuration.Camera.StreamUrl);
        var fingerprint = Fingerprint(
            configuration.RuntimeCameraId,
            configuration.Camera.StreamUrl!,
            configuration.LaneId);
        if (string.Equals(fingerprint, configuration.ConfigurationFingerprint, StringComparison.Ordinal))
        {
            return;
        }

        configuration.ConfigurationFingerprint = fingerprint;
        configuration.ConfigurationVersion++;
        configuration.LastSyncStatus = FaceCameraSyncStatuses.Pending;
        configuration.UpdatedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task MarkSyncedAsync(
        FaceCameraConfiguration configuration,
        CancellationToken cancellationToken)
    {
        if (configuration.LastAppliedVersion == configuration.ConfigurationVersion &&
            configuration.LastSyncStatus == FaceCameraSyncStatuses.Synced &&
            configuration.LastSyncError is null)
        {
            return;
        }
        configuration.LastAppliedVersion = configuration.ConfigurationVersion;
        configuration.LastSyncStatus = FaceCameraSyncStatuses.Synced;
        configuration.LastSyncError = null;
        configuration.LastSyncAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task MarkFailureAsync(
        FaceCameraConfiguration configuration,
        string status,
        string message,
        CancellationToken cancellationToken)
    {
        var sanitizedMessage = Sanitize(message);
        if (configuration.LastSyncStatus == status &&
            configuration.LastSyncError == sanitizedMessage)
        {
            return;
        }
        configuration.LastSyncStatus = status;
        configuration.LastSyncError = sanitizedMessage;
        configuration.LastSyncAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
    }

    public FaceCameraStartRequest CreateStartRequest(FaceCameraConfiguration configuration)
    {
        ValidateStreamUrl(configuration.Camera.StreamUrl);
        return new FaceCameraStartRequest
        {
            Ip = configuration.Camera.StreamUrl!,
            LaneId = configuration.LaneId?.ToString()
        };
    }

    private async Task<FaceCameraDesiredStateDto> SetDesiredStateAsync(
        string runtimeCameraId,
        bool start,
        CancellationToken cancellationToken)
    {
        var validId = FaceCameraIdValidator.Validate(runtimeCameraId);
        var configuration = await QueryManaged()
            .SingleOrDefaultAsync(x => x.RuntimeCameraId == validId, cancellationToken)
            ?? throw new KeyNotFoundException("Face camera configuration does not exist.");

        await RefreshConfigurationVersionAsync(configuration, cancellationToken);
        configuration.DesiredState = start
            ? FaceCameraDesiredStates.Running
            : FaceCameraDesiredStates.Stopped;
        configuration.LastSyncStatus = FaceCameraSyncStatuses.Pending;
        configuration.LastSyncError = null;
        configuration.UpdatedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);

        try
        {
            var response = start
                ? await _runtime.StartCameraAsync(validId, CreateStartRequest(configuration), cancellationToken)
                : await _runtime.StopCameraAsync(validId, cancellationToken);
            var success = response.StatusCode.IsSuccess() ||
                (!start && response.StatusCode == HttpStatusCode.NotFound);
            if (success)
            {
                await MarkSyncedAsync(configuration, cancellationToken);
            }
            else
            {
                await MarkFailureAsync(
                    configuration,
                    FaceCameraSyncStatuses.Error,
                    $"Face Runtime rejected the operation (HTTP {(int)response.StatusCode}).",
                    cancellationToken);
            }
            return new FaceCameraDesiredStateDto(
                ToDto(configuration, null, true),
                success,
                (int)response.StatusCode);
        }
        catch (FaceRuntimeUnavailableException)
        {
            await MarkFailureAsync(
                configuration,
                FaceCameraSyncStatuses.Unavailable,
                "Face Runtime is unavailable.",
                cancellationToken);
            return new FaceCameraDesiredStateDto(ToDto(configuration, null, false), false, null);
        }
    }

    private IQueryable<FaceCameraConfiguration> QueryManaged() =>
        _db.FaceCameraConfigurations
            .Include(x => x.Camera)
            .Include(x => x.Lane);

    private static FaceCameraConfigurationDto ToDto(
        FaceCameraConfiguration configuration,
        FaceRuntimeSession? session,
        bool runtimeAvailable) =>
        new(
            configuration.Id,
            configuration.CameraId,
            configuration.Camera.CameraName,
            configuration.RuntimeCameraId,
            configuration.LaneId,
            configuration.Lane?.Name,
            configuration.DesiredState,
            configuration.AutoRestore,
            configuration.ConfigurationVersion,
            configuration.LastAppliedVersion,
            configuration.LastSyncStatus,
            configuration.LastSyncError,
            configuration.LastSyncAtUtc,
            MaskUrl(configuration.Camera.StreamUrl),
            SafePreviewUrl(configuration.Camera.UrlView),
            session?.Enabled,
            session?.Connected,
            session?.Status ?? (runtimeAvailable ? "Missing" : "Unavailable"),
            Convert.ToBase64String(configuration.RowVersion ?? Array.Empty<byte>()));

    private void ApplyConcurrencyToken(
        FaceCameraConfiguration configuration,
        string? encodedRowVersion)
    {
        if (string.IsNullOrWhiteSpace(encodedRowVersion))
        {
            return;
        }
        try
        {
            _db.Entry(configuration).Property(x => x.RowVersion).OriginalValue =
                Convert.FromBase64String(encodedRowVersion);
        }
        catch (FormatException)
        {
            throw new ArgumentException("rowVersion is invalid.", nameof(encodedRowVersion));
        }
    }

    private static void ValidateStreamUrl(string? streamUrl)
    {
        if (string.IsNullOrWhiteSpace(streamUrl) ||
            !Uri.TryCreate(streamUrl, UriKind.Absolute, out var uri) ||
            !SupportedSchemes.Contains(uri.Scheme))
        {
            throw new ArgumentException("Camera stream URL is missing or unsupported.");
        }
    }

    private static string Fingerprint(string runtimeCameraId, string streamUrl, int? laneId)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(
            $"{runtimeCameraId}\n{streamUrl}\n{laneId?.ToString() ?? string.Empty}"));
        return Convert.ToHexString(bytes);
    }

    private static string? MaskUrl(string? streamUrl)
    {
        if (string.IsNullOrWhiteSpace(streamUrl) ||
            !Uri.TryCreate(streamUrl, UriKind.Absolute, out var uri))
        {
            return null;
        }
        if (string.IsNullOrEmpty(uri.UserInfo))
        {
            return uri.ToString();
        }
        var builder = new UriBuilder(uri) { UserName = "***", Password = "***" };
        return builder.Uri.ToString();
    }

    private static string? SafePreviewUrl(string? previewUrl)
    {
        if (string.IsNullOrWhiteSpace(previewUrl) ||
            !Uri.TryCreate(previewUrl, UriKind.Absolute, out var uri) ||
            uri.Scheme is not ("http" or "https") ||
            !string.IsNullOrEmpty(uri.UserInfo))
        {
            return null;
        }
        return uri.ToString();
    }

    private static string Sanitize(string message)
    {
        var value = string.IsNullOrWhiteSpace(message)
            ? "Face Runtime synchronization failed."
            : message.Trim();
        return value.Length <= 500 ? value : value[..500];
    }

    private static IReadOnlyDictionary<string, FaceRuntimeSession> ParseSessions(string body)
    {
        var sessions = new Dictionary<string, FaceRuntimeSession>(StringComparer.Ordinal);
        try
        {
            using var document = JsonDocument.Parse(body);
            if (!document.RootElement.TryGetProperty("sessions", out var values) ||
                values.ValueKind != JsonValueKind.Array)
            {
                return sessions;
            }
            foreach (var value in values.EnumerateArray())
            {
                var cameraId = GetString(value, "cameraId");
                if (!FaceCameraIdValidator.TryValidate(cameraId, out var validId))
                {
                    continue;
                }
                sessions[validId] = new FaceRuntimeSession(
                    validId,
                    GetString(value, "laneId"),
                    GetBoolean(value, "enabled"),
                    GetBoolean(value, "connected"),
                    GetString(value, "status") ?? "Unknown");
            }
        }
        catch (JsonException)
        {
            return sessions;
        }
        return sessions;
    }

    private static string? GetString(JsonElement element, string name) =>
        element.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;

    private static bool GetBoolean(JsonElement element, string name) =>
        element.TryGetProperty(name, out var value) &&
        value.ValueKind is JsonValueKind.True or JsonValueKind.False &&
        value.GetBoolean();
}

internal static class HttpStatusCodeExtensions
{
    public static bool IsSuccess(this HttpStatusCode statusCode) =>
        (int)statusCode is >= 200 and <= 299;
}
