namespace API.DTOs;

public class LoginResponse
{
    public int UserId { get; set; }
    public string? Token { get; set; }
    public string? RefreshToken { get; set; }
    public string Username { get; set; } = null!;
    public string FullName { get; set; } = null!;
    public string Role { get; set; } = null!;
    public int? EmployeeId { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public DateTime? RefreshTokenExpiresAt { get; set; }
    public bool RequiresMfa { get; set; }
    public bool RequiresMfaSetup { get; set; }
    public string? MfaSetupSecret { get; set; }
    public string? MfaSetupUri { get; set; }
    public string? Message { get; set; }
    public bool HasOperationalScopeAssignments { get; set; }
    public List<string> OperationalTaskKeys { get; set; } = new();
}
