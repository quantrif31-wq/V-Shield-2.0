using System.Text.RegularExpressions;

namespace API.Services.FaceRecognition;

public static partial class FaceCameraIdValidator
{
    public static bool TryValidate(string? cameraId, out string validCameraId)
    {
        validCameraId = cameraId ?? string.Empty;
        return validCameraId.Length is >= 1 and <= 64
            && !validCameraId.Contains("..", StringComparison.Ordinal)
            && CameraIdPattern().IsMatch(validCameraId);
    }

    public static string Validate(string? cameraId)
    {
        if (!TryValidate(cameraId, out var validCameraId))
        {
            throw new ArgumentException("cameraId is invalid.", nameof(cameraId));
        }

        return validCameraId;
    }

    [GeneratedRegex("^[A-Za-z0-9_.-]+$", RegexOptions.CultureInvariant)]
    private static partial Regex CameraIdPattern();
}
