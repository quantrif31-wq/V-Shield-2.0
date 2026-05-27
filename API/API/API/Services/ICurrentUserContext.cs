namespace API.Services;

public interface ICurrentUserContext
{
    int? UserId { get; }
    string? Username { get; }
}

