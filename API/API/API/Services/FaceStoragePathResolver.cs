using Microsoft.Extensions.Options;

namespace API.Services;

public sealed class FaceStorageOptions
{
    public const string SectionName = "FaceStorage";

    public string? InputRoot { get; set; }

    /// <summary>Thư mục chứa model Face ID đang active (do Face Runtime ghi).</summary>
    public string? ModelActiveDir { get; set; }
}

public interface IFaceStoragePathResolver
{
    string InputRoot { get; }
    string ModelActiveDir { get; }
    string ResolveDirectory(string directoryName);
    string ResolveFile(string directoryName, string fileName);
}

public sealed class FaceStoragePathResolver : IFaceStoragePathResolver
{
    private readonly StringComparison _pathComparison =
        OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;

    public FaceStoragePathResolver(
        IOptions<FaceStorageOptions> options,
        IWebHostEnvironment environment)
    {
        var configuredRoot = options.Value.InputRoot;
        var fallbackWebRoot = environment.WebRootPath
            ?? Path.Combine(environment.ContentRootPath, "wwwroot");
        InputRoot = Path.GetFullPath(
            string.IsNullOrWhiteSpace(configuredRoot)
                ? Path.Combine(fallbackWebRoot, "uploads", "VideoFace")
                : configuredRoot);

        var configuredModelDir = options.Value.ModelActiveDir;
        ModelActiveDir = Path.GetFullPath(
            string.IsNullOrWhiteSpace(configuredModelDir)
                ? Path.Combine(InputRoot, "models", "active")
                : configuredModelDir);
    }

    public string InputRoot { get; }

    public string ModelActiveDir { get; }

    public string ResolveDirectory(string directoryName)
    {
        ValidateSingleSegment(directoryName, nameof(directoryName));
        return EnsureWithinRoot(Path.Combine(InputRoot, directoryName));
    }

    public string ResolveFile(string directoryName, string fileName)
    {
        ValidateSingleSegment(fileName, nameof(fileName));
        return EnsureWithinRoot(Path.Combine(ResolveDirectory(directoryName), fileName));
    }

    private string EnsureWithinRoot(string candidate)
    {
        var fullPath = Path.GetFullPath(candidate);
        var rootWithSeparator = InputRoot.TrimEnd(
            Path.DirectorySeparatorChar,
            Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;

        if (!fullPath.StartsWith(rootWithSeparator, _pathComparison))
            throw new InvalidOperationException("Resolved face storage path escapes the configured input root.");

        return fullPath;
    }

    private static void ValidateSingleSegment(string value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value) ||
            value != Path.GetFileName(value) ||
            value.Contains(Path.DirectorySeparatorChar) ||
            value.Contains(Path.AltDirectorySeparatorChar))
        {
            throw new ArgumentException("Face storage path values must be a single safe segment.", parameterName);
        }
    }
}
