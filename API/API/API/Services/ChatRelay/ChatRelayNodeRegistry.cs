using System.Collections.Concurrent;

namespace API.Services.ChatRelay;

/// <summary>
/// Registry kept on the Central (VPS) server that maps an employeeId to the
/// AreaNode connection that currently serves that employee's ChatHub presence.
/// The node connection refreshes this set periodically.
/// </summary>
public class ChatRelayNodeRegistry
{
    private readonly ConcurrentDictionary<int, string> _employeeToNodeConnection = new();
    private readonly ConcurrentDictionary<string, HashSet<int>> _nodeToEmployees = new();

    public void ReplaceEmployees(string nodeConnectionId, IEnumerable<int> employeeIds)
    {
        if (string.IsNullOrWhiteSpace(nodeConnectionId))
        {
            return;
        }

        if (_nodeToEmployees.TryGetValue(nodeConnectionId, out var existing))
        {
            foreach (var id in existing)
            {
                _employeeToNodeConnection.TryRemove(new KeyValuePair<int, string>(id, nodeConnectionId));
            }
        }

        var next = new HashSet<int>(employeeIds.Where(id => id > 0));
        _nodeToEmployees[nodeConnectionId] = next;

        foreach (var id in next)
        {
            _employeeToNodeConnection[id] = nodeConnectionId;
        }
    }

    public void RemoveConnection(string nodeConnectionId)
    {
        if (_nodeToEmployees.TryRemove(nodeConnectionId, out var employeeIds))
        {
            foreach (var id in employeeIds)
            {
                _employeeToNodeConnection.TryRemove(new KeyValuePair<int, string>(id, nodeConnectionId));
            }
        }
    }

    public bool TryGetNodeConnection(int employeeId, out string? nodeConnectionId)
    {
        if (_employeeToNodeConnection.TryGetValue(employeeId, out var connectionId))
        {
            nodeConnectionId = connectionId;
            return true;
        }

        nodeConnectionId = null;
        return false;
    }
}
