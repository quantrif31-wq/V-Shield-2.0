using API.DTOs;
using API.Models;

namespace API.Services;

public interface IAuthenticationService
{
    Task<LoginResponse?> LoginAsync(LoginRequest request);
    Task<LoginResponse?> RefreshAsync(string refreshToken);
    Task LogoutAsync(int userId, string? refreshToken);
    Task<bool> ValidateAccessTokenVersionAsync(int userId, int tokenVersion);
    bool IsLoginTemporarilyLocked(string? username);
    bool RequiresMfa(AppUser user);
}
