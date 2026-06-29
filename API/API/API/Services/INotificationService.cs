namespace API.Services;

public interface INotificationService
{
    Task NotifyEventAsync(
        string eventType,
        string title,
        string? body = null,
        string? referenceType = null,
        string? referenceId = null,
        string? actionUrl = null,
        decimal? latitude = null,
        decimal? longitude = null,
        string? locationLabel = null
    );

    Task NotifyUsersAsync(
        List<int> userIds,
        string title,
        string? body = null,
        string? category = "System",
        string? referenceType = null,
        string? referenceId = null,
        string? actionUrl = null,
        decimal? latitude = null,
        decimal? longitude = null,
        string? locationLabel = null
    );

    Task NotifyRolesAsync(
        List<string> roles,
        string title,
        string? body = null,
        string? category = "System",
        string? referenceType = null,
        string? referenceId = null,
        string? actionUrl = null,
        decimal? latitude = null,
        decimal? longitude = null,
        string? locationLabel = null
    );

    Task NotifyAllAsync(
        string title,
        string? body = null,
        string? category = "System",
        string? referenceType = null,
        string? referenceId = null,
        string? actionUrl = null,
        decimal? latitude = null,
        decimal? longitude = null,
        string? locationLabel = null
    );
}
