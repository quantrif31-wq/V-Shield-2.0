using System.Collections.Concurrent;

namespace API.Services.ChatRelay;

/// <summary>
/// Track which employees are currently connected to this server's ChatHub.
/// Used to decide whether call signaling should be delivered locally or
/// forwarded across the AreaNode &lt;-&gt; Central relay bridge.
/// </summary>
public class ChatPresenceRegistry
{
    private readonly ConcurrentDictionary<int, int> _connections = new();

    public void Add(int employeeId)
    {
        if (employeeId <= 0) return;
        _connections.AddOrUpdate(employeeId, 1, (_, count) => count + 1);
    }

    public void Remove(int employeeId)
    {
        if (employeeId <= 0) return;
        if (!_connections.TryGetValue(employeeId, out var count)) return;

        if (count <= 1)
        {
            _connections.TryRemove(employeeId, out _);
        }
        else
        {
            _connections.TryUpdate(employeeId, count - 1, count);
        }
    }

    public bool IsOnline(int employeeId) => employeeId > 0 && _connections.ContainsKey(employeeId);

    public int[] GetOnlineEmployeeIds() => _connections.Keys.ToArray();
}
