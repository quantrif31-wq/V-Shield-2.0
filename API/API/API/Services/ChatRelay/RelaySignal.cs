namespace API.Services.ChatRelay;

public enum RelaySignalKind
{
    IncomingCall,
    CallResponse,
    CallEnded
}

/// <summary>
/// Payload carried across the AreaNode &lt;-&gt; Central realtime relay bridge.
/// </summary>
public class RelaySignal
{
    public RelaySignalKind Kind { get; set; }

    /// <summary>Employee that should receive the signal (routing key).</summary>
    public int TargetEmployeeId { get; set; }

    public int FromEmployeeId { get; set; }
    public string? FromFullName { get; set; }
    public string? SignalingType { get; set; }
    public string? SignalingData { get; set; }
    public int? ConversationId { get; set; }
}
