namespace API.Services.ChatRelay;

/// <summary>
/// Maps a relay signal kind to the SignalR client event name and payload shape
/// that web/mobile clients already expect from ChatHub.
/// </summary>
public static class RelaySignalHelper
{
    public static string GetClientEventName(RelaySignal signal) => signal.Kind switch
    {
        RelaySignalKind.IncomingCall => "IncomingCall",
        RelaySignalKind.CallResponse => "CallResponse",
        RelaySignalKind.CallEnded => "CallEnded",
        RelaySignalKind.CallHandledElsewhere => "CallEnded",
        _ => "IncomingCall"
    };

    public static object BuildClientPayload(RelaySignal signal) => signal.Kind switch
    {
        RelaySignalKind.IncomingCall => new
        {
            fromEmployeeId = signal.FromEmployeeId,
            fromFullName = signal.FromFullName,
            signalingType = signal.SignalingType,
            signalingData = signal.SignalingData,
            conversationId = signal.ConversationId
        },
        RelaySignalKind.CallResponse => new
        {
            fromEmployeeId = signal.FromEmployeeId,
            fromFullName = signal.FromFullName,
            signalingType = signal.SignalingType,
            signalingData = signal.SignalingData
        },
        RelaySignalKind.CallEnded => new
        {
            fromEmployeeId = signal.FromEmployeeId,
            conversationId = signal.ConversationId
        },
        RelaySignalKind.CallHandledElsewhere => new
        {
            fromEmployeeId = signal.FromEmployeeId,
            conversationId = signal.ConversationId
        },
        _ => new { }
    };
}
