using API.Hubs;
using API.Services.Sync;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Options;

namespace API.Services.ChatRelay;

/// <summary>
/// Runs on the AreaNode (local docker) API. Opens a persistent SignalR client
/// connection to the Central (VPS) ChatRelayHub, keeps Central informed about
/// which employees are currently served locally, and forwards call signaling in
/// both directions:
///
/// - local user -&gt; remote: local ChatHub asks the worker to send the signal up.
/// - remote -&gt; local: Central pushes "RelaySignal" here and the worker delivers
///   it to the local ChatHub group.
/// </summary>
public class AreaNodeChatRelayWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<AreaNodeChatRelayWorker> _logger;
    private readonly SyncRuntimeOptions _options;
    private readonly ChatPresenceRegistry _presenceRegistry;
    private readonly IHubContext<ChatHub> _chatHubContext;
    private readonly object _connectionLock = new();
    private HubConnection? _connection;

    public AreaNodeChatRelayWorker(
        IServiceScopeFactory scopeFactory,
        ILogger<AreaNodeChatRelayWorker> logger,
        IOptions<SyncRuntimeOptions> options,
        ChatPresenceRegistry presenceRegistry,
        IHubContext<ChatHub> chatHubContext)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _options = options.Value;
        _presenceRegistry = presenceRegistry;
        _chatHubContext = chatHubContext;
    }

    public bool IsConnected
    {
        get
        {
            lock (_connectionLock)
            {
                return _connection?.State == HubConnectionState.Connected;
            }
        }
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
                var nodeSecret = await GetNodeSecretAsync(stoppingToken);
                if (string.IsNullOrWhiteSpace(nodeSecret))
                {
                    _logger.LogDebug("Chat relay waiting for node registration before connecting.");
                    await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
                    continue;
                }

                await RunRelayCycleAsync(nodeSecret, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Chat relay cycle failed.");
            }

            await Task.Delay(TimeSpan.FromSeconds(3), stoppingToken);
        }
    }

    private async Task<string?> GetNodeSecretAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var store = scope.ServiceProvider.GetRequiredService<SyncSystemConfigStore>();
        return await store.GetNodeSecretAsync(cancellationToken);
    }

    private async Task RunRelayCycleAsync(string nodeSecret, CancellationToken stoppingToken)
    {
        HubConnection? connection = null;
        try
        {
            var builder = new HubConnectionBuilder()
                .WithUrl(
                    $"{_options.CentralBaseUrl!.TrimEnd('/')}/hubs/chat-relay?nodeId={Uri.EscapeDataString(_options.LocalAreaNodeId!)}&nodeSecret={Uri.EscapeDataString(nodeSecret)}",
                    options =>
                    {
                        options.HttpMessageHandlerFactory = handler => handler;
                    })
                .WithAutomaticReconnect(new[]
                {
                    TimeSpan.Zero,
                    TimeSpan.FromSeconds(2),
                    TimeSpan.FromSeconds(5),
                    TimeSpan.FromSeconds(10)
                });

            connection = builder.Build();

            connection.On<RelaySignal>("RelaySignal", signal =>
            {
                _ = DeliverToLocalAsync(signal);
            });

            lock (_connectionLock)
            {
                _connection = connection;
            }

            _logger.LogInformation("Connecting chat relay to Central {Central}", _options.CentralBaseUrl);
            await connection.StartAsync(stoppingToken);
            _logger.LogInformation("Chat relay connected.");

            while (!stoppingToken.IsCancellationRequested &&
                   connection.State == HubConnectionState.Connected)
            {
                var onlineIds = _presenceRegistry.GetOnlineEmployeeIds();
                try
                {
                    await connection.InvokeAsync("RegisterPresence", onlineIds, stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogDebug(ex, "Failed to register presence; relay may be reconnecting.");
                }

                await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);
            }
        }
        finally
        {
            lock (_connectionLock)
            {
                if (ReferenceEquals(_connection, connection))
                {
                    _connection = null;
                }
            }

            if (connection != null)
            {
                await connection.DisposeAsync();
            }
        }
    }

    /// <summary>Send a signal from a local user up to Central.</summary>
    public async Task<bool> SendSignalAsync(RelaySignal signal)
    {
        HubConnection? connection;
        lock (_connectionLock)
        {
            connection = _connection;
        }

        if (connection == null || connection.State != HubConnectionState.Connected)
        {
            return false;
        }

        try
        {
            await connection.InvokeAsync("RelaySignal", signal);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Failed to forward relay signal.");
            return false;
        }
    }

    private async Task DeliverToLocalAsync(RelaySignal signal)
    {
        try
        {
            var eventName = RelaySignalHelper.GetClientEventName(signal);
            var payload = RelaySignalHelper.BuildClientPayload(signal);
            await _chatHubContext.Clients.Group($"user_{signal.TargetEmployeeId}").SendAsync(eventName, payload);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to deliver relayed signal locally.");
        }
    }
}
