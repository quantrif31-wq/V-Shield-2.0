using API.DTOs;

namespace API.Services;

public interface IStepUpService
{
    Task<StepUpSessionResponse?> StartAsync(int userId, string action, string? reason, string? ipAddress, string? userAgent);
    Task<StepUpSessionResponse?> VerifyAsync(int userId, long sessionId, string? password, string? mfaCode);
    Task<StepUpSessionResponse?> GetStatusAsync(int userId, string? action, long? sessionId);
    Task<bool> HasActiveSessionAsync(int userId, string action, long? sessionId);
}
