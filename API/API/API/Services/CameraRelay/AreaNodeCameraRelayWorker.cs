using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using API.Services.Sync;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Options;

namespace API.Services.CameraRelay;

/// <summary>
/// Runs only on an AreaNode. It keeps a private SignalR control channel to the
/// Central server, while each camera stream stays inside the local Docker
/// network and is published as a WebRTC peer by go2rtc.
/// </summary>
public sealed class AreaNodeCameraRelayWorker : BackgroundService
{
    private static readonly Regex StreamNamePattern = new("^[A-Za-z0-9_.-]{1,96}$", RegexOptions.Compiled);
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<AreaNodeCameraRelayWorker> _logger;
    private readonly SyncRuntimeOptions _options;
    private readonly object _connectionLock = new();
    private readonly ConcurrentDictionary<string, ClientWebSocket> _sockets = new();
    private HubConnection? _connection;

    public AreaNodeCameraRelayWorker(IServiceScopeFactory scopeFactory, ILogger<AreaNodeCameraRelayWorker> logger, IOptions<SyncRuntimeOptions> options)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _options = options.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!string.Equals(_options.Mode, SyncRuntimeModes.AreaNode, StringComparison.OrdinalIgnoreCase) ||
            string.IsNullOrWhiteSpace(_options.CentralBaseUrl) || string.IsNullOrWhiteSpace(_options.LocalAreaNodeId))
            return;

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var secret = await GetNodeSecretAsync(stoppingToken);
                if (string.IsNullOrWhiteSpace(secret))
                {
                    await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
                    continue;
                }
                await RunCycleAsync(secret, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
            catch (Exception error) { _logger.LogWarning(error, "Camera relay cycle failed."); }

            await Task.Delay(TimeSpan.FromSeconds(3), stoppingToken);
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        foreach (var sessionId in _sockets.Keys.ToArray()) await StopSessionAsync(sessionId);
        await base.StopAsync(cancellationToken);
    }

    private async Task<string?> GetNodeSecretAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        return await scope.ServiceProvider.GetRequiredService<SyncSystemConfigStore>().GetNodeSecretAsync(cancellationToken);
    }

    private async Task RunCycleAsync(string nodeSecret, CancellationToken stoppingToken)
    {
        HubConnection? connection = null;
        try
        {
            connection = new HubConnectionBuilder()
                .WithUrl($"{_options.CentralBaseUrl!.TrimEnd('/')}/hubs/camera-relay?nodeId={Uri.EscapeDataString(_options.LocalAreaNodeId!)}&nodeSecret={Uri.EscapeDataString(nodeSecret)}")
                .WithAutomaticReconnect(new[] { TimeSpan.Zero, TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(10) })
                .Build();

            connection.On<string, string>("CameraRelayStart", (sessionId, streamName) => _ = StartSessionAsync(sessionId, streamName));
            connection.On<string, string, string>("CameraRelaySignal", (sessionId, kind, value) => _ = ForwardSignalAsync(sessionId, kind, value));
            connection.On<string>("CameraRelayStop", sessionId => _ = StopSessionAsync(sessionId));
            connection.Closed += async _ =>
            {
                foreach (var sessionId in _sockets.Keys.ToArray()) await StopSessionAsync(sessionId);
            };

            lock (_connectionLock) _connection = connection;
            await connection.StartAsync(stoppingToken);
            _logger.LogInformation("Camera relay connected to Central {Central}", _options.CentralBaseUrl);

            while (!stoppingToken.IsCancellationRequested && connection.State == HubConnectionState.Connected)
                await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
        }
        finally
        {
            lock (_connectionLock) if (ReferenceEquals(_connection, connection)) _connection = null;
            foreach (var sessionId in _sockets.Keys.ToArray()) await StopSessionAsync(sessionId);
            if (connection != null) await connection.DisposeAsync();
        }
    }

    private async Task StartSessionAsync(string sessionId, string streamName)
    {
        if (string.IsNullOrWhiteSpace(sessionId) || !StreamNamePattern.IsMatch(streamName ?? string.Empty)) return;
        await StopSessionAsync(sessionId);
        var socket = new ClientWebSocket();
        try
        {
            await socket.ConnectAsync(new Uri($"ws://go2rtc:1984/api/ws?src={Uri.EscapeDataString(streamName)}"), CancellationToken.None);
            if (!_sockets.TryAdd(sessionId, socket)) { socket.Dispose(); return; }
            await NotifyReadyAsync(sessionId, true, null);
            _ = ReceiveGo2RtcAsync(sessionId, socket);
        }
        catch (Exception error)
        {
            socket.Dispose();
            _logger.LogWarning(error, "Cannot open local go2rtc stream {StreamName}", streamName);
            await NotifyReadyAsync(sessionId, false, "Không thể mở camera local.");
        }
    }

    private async Task ForwardSignalAsync(string sessionId, string kind, string value)
    {
        if (kind is not ("offer" or "answer" or "candidate") || !_sockets.TryGetValue(sessionId, out var socket) || socket.State != WebSocketState.Open) return;
        try
        {
            var payload = JsonSerializer.Serialize(new { type = $"webrtc/{kind}", value = value ?? string.Empty });
            await socket.SendAsync(Encoding.UTF8.GetBytes(payload), WebSocketMessageType.Text, true, CancellationToken.None);
        }
        catch (Exception error) { _logger.LogDebug(error, "Could not forward camera relay signal."); }
    }

    private async Task ReceiveGo2RtcAsync(string sessionId, ClientWebSocket socket)
    {
        try
        {
            var buffer = new byte[32 * 1024];
            while (socket.State == WebSocketState.Open)
            {
                using var message = new MemoryStream();
                WebSocketReceiveResult result;
                do
                {
                    result = await socket.ReceiveAsync(buffer, CancellationToken.None);
                    if (result.MessageType == WebSocketMessageType.Close) return;
                    message.Write(buffer, 0, result.Count);
                } while (!result.EndOfMessage);
                if (result.MessageType != WebSocketMessageType.Text) continue;

                using var doc = JsonDocument.Parse(message.ToArray());
                var type = doc.RootElement.TryGetProperty("type", out var typeValue) ? typeValue.GetString() : null;
                var value = doc.RootElement.TryGetProperty("value", out var valueElement) ? valueElement.GetString() : null;
                var kind = type?.Replace("webrtc/", string.Empty, StringComparison.Ordinal);
                if (kind is "answer" or "candidate") await NotifySignalAsync(sessionId, kind, value ?? string.Empty);
            }
        }
        catch (Exception error) { _logger.LogDebug(error, "Local go2rtc relay stream ended."); }
        finally { await StopSessionAsync(sessionId, socket); }
    }

    private async Task NotifySignalAsync(string sessionId, string kind, string value)
    {
        HubConnection? connection;
        lock (_connectionLock) connection = _connection;
        if (connection?.State == HubConnectionState.Connected)
            await connection.InvokeAsync("Signal", sessionId, kind, value);
    }

    private async Task NotifyReadyAsync(string sessionId, bool ready, string? message)
    {
        HubConnection? connection;
        lock (_connectionLock) connection = _connection;
        if (connection?.State == HubConnectionState.Connected)
            await connection.InvokeAsync("Ready", sessionId, ready, message);
    }

    private async Task StopSessionAsync(string sessionId, ClientWebSocket? expectedSocket = null)
    {
        if (!_sockets.TryGetValue(sessionId, out var socket) || (expectedSocket != null && !ReferenceEquals(socket, expectedSocket))) return;
        _sockets.TryRemove(sessionId, out _);
        try { if (socket.State is WebSocketState.Open or WebSocketState.CloseReceived) await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "closed", CancellationToken.None); }
        catch { }
        socket.Dispose();
    }
}
