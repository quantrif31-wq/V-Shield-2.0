namespace API.Services.Sync;

public interface ISyncExecutionContext
{
    bool SuppressOutboxPublishing { get; set; }
}

public class SyncExecutionContext : ISyncExecutionContext
{
    private static readonly AsyncLocal<bool> SuppressState = new();

    public bool SuppressOutboxPublishing
    {
        get => SuppressState.Value;
        set => SuppressState.Value = value;
    }
}
