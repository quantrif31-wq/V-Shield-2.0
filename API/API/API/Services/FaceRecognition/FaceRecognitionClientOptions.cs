namespace API.Services.FaceRecognition;

public sealed class FaceRecognitionClientOptions
{
    public const string BaseUrlConfigurationKey = "AiServices:FaceCameraBaseUrl";
    public const string TimeoutConfigurationKey = "AiServices:FaceCameraTimeoutSeconds";
    public const int DefaultTimeoutSeconds = 100;

    private FaceRecognitionClientOptions(Uri baseAddress, TimeSpan timeout)
    {
        BaseAddress = baseAddress;
        Timeout = timeout;
    }

    public Uri BaseAddress { get; }

    public TimeSpan Timeout { get; }

    public static FaceRecognitionClientOptions FromConfiguration(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        var baseAddress = NormalizeBaseAddress(configuration[BaseUrlConfigurationKey]);
        var configuredTimeout = configuration[TimeoutConfigurationKey];
        var timeoutSeconds = DefaultTimeoutSeconds;

        if (!string.IsNullOrWhiteSpace(configuredTimeout) &&
            (!int.TryParse(configuredTimeout, out timeoutSeconds) || timeoutSeconds <= 0))
        {
            throw new InvalidOperationException(
                $"Configuration key '{TimeoutConfigurationKey}' must be an integer greater than 0.");
        }

        return new FaceRecognitionClientOptions(
            baseAddress,
            TimeSpan.FromSeconds(timeoutSeconds));
    }

    public static Uri NormalizeBaseAddress(string? configuredValue)
    {
        if (string.IsNullOrWhiteSpace(configuredValue))
        {
            throw new InvalidOperationException(
                $"Configuration key '{BaseUrlConfigurationKey}' is required.");
        }

        if (!Uri.TryCreate(configuredValue.Trim(), UriKind.Absolute, out var configuredUri) ||
            (configuredUri.Scheme != Uri.UriSchemeHttp && configuredUri.Scheme != Uri.UriSchemeHttps) ||
            !string.IsNullOrEmpty(configuredUri.Query) ||
            !string.IsNullOrEmpty(configuredUri.Fragment))
        {
            throw new InvalidOperationException(
                $"Configuration key '{BaseUrlConfigurationKey}' must be a valid absolute HTTP or HTTPS URI.");
        }

        var path = configuredUri.AbsolutePath.TrimEnd('/');
        if (string.IsNullOrEmpty(path))
        {
            path = "/api";
        }

        var normalized = new UriBuilder(configuredUri)
        {
            Path = $"{path}/",
            Query = string.Empty,
            Fragment = string.Empty
        };

        return normalized.Uri;
    }
}
