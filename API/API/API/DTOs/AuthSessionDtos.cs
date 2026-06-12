using System.ComponentModel.DataAnnotations;

namespace API.DTOs;

public class RefreshTokenRequest
{
    [Required]
    public string RefreshToken { get; set; } = string.Empty;
}

public class LogoutRequest
{
    public string? RefreshToken { get; set; }
}

public class StepUpStartRequest
{
    [Required]
    [MaxLength(120)]
    public string Action { get; set; } = "AllPrivilegedActions";

    [MaxLength(500)]
    public string? Reason { get; set; }
}

public class StepUpVerifyRequest
{
    [Required]
    public long SessionId { get; set; }

    public string? Password { get; set; }
    public string? MfaCode { get; set; }
}

public class StepUpSessionResponse
{
    public long SessionId { get; set; }
    public string Action { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string ChallengeNonce { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? VerifiedAtUtc { get; set; }
    public DateTime ExpiresAtUtc { get; set; }
    public bool Active { get; set; }
    public string? Message { get; set; }
}

public class MfaRecoveryCodeRequest
{
    public int Count { get; set; } = 10;
}

public class MfaRecoveryCodeResponse
{
    public int UserId { get; set; }
    public DateTime ExpiresAtUtc { get; set; }
    public IReadOnlyList<string> Codes { get; set; } = Array.Empty<string>();
}
