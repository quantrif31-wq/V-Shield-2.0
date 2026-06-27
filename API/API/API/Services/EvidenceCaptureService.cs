using System.Security.Cryptography;
using System.Text;
using API.Data;
using API.Models;
using Microsoft.EntityFrameworkCore;

namespace API.Services;

public class EvidenceCaptureService
{
    private readonly ApplicationDbContext _context;
    private readonly IWebHostEnvironment _env;

    public EvidenceCaptureService(ApplicationDbContext context, IWebHostEnvironment env)
    {
        _context = context;
        _env = env;
    }

    public async Task<string?> CaptureBase64Async(
        string? base64Data, string evidenceType, string? sourceRef,
        int? siteId = null, int? createdByUserId = null)
    {
        if (string.IsNullOrWhiteSpace(base64Data)) return null;

        var bytes = DecodeBase64Image(base64Data);
        if (bytes == null || bytes.Length == 0) return null;

        var dateDir = DateTime.UtcNow.ToString("yyyy-MM-dd");
        var fileName = $"{Guid.NewGuid():N}.jpg";
        var relativePath = Path.Combine("uploads", "evidence", dateDir, fileName);
        var absoluteDir = Path.Combine(_env.WebRootPath, "uploads", "evidence", dateDir);
        Directory.CreateDirectory(absoluteDir);
        var absolutePath = Path.Combine(absoluteDir, fileName);
        await File.WriteAllBytesAsync(absolutePath, bytes);

        var url = $"/{relativePath.Replace("\\", "/")}";
        var hash = ComputeSha256(bytes);
        var evidenceTypeClean = evidenceType switch
        {
            "plate-crop" => "PlateCrop",
            "face-crop" => "FaceCrop",
            "qr-snapshot" => "QrSnapshot",
            "snapshot" => "Snapshot",
            _ => evidenceType
        };

        var item = new EvidenceItem
        {
            EvidenceType = evidenceTypeClean,
            SourceType = "AccessLog",
            SourceReference = sourceRef,
            StorageReference = url,
            HashSha256 = hash,
            PrivacyLabel = "Internal",
            RetentionCategory = "Evidence",
            SiteId = siteId,
            CreatedByUserId = createdByUserId
        };
        _context.EvidenceItems.Add(item);
        await _context.SaveChangesAsync();

        _context.ChainOfCustodyEntries.Add(new ChainOfCustodyEntry
        {
            EvidenceItemId = item.EvidenceItemId,
            Action = "Registered",
            ActorUserId = createdByUserId,
            ToCustodian = "V-Shield Auto-Evidence",
            HashAfter = hash,
            Note = $"Auto-captured {evidenceTypeClean} from {sourceRef}"
        });
        await _context.SaveChangesAsync();

        return url;
    }

    private static byte[]? DecodeBase64Image(string base64)
    {
        try
        {
            var trimmed = base64.Trim();
            if (trimmed.Contains(','))
                trimmed = trimmed[(trimmed.IndexOf(',') + 1)..];
            trimmed = trimmed.Trim();
            return Convert.FromBase64String(trimmed);
        }
        catch
        {
            return null;
        }
    }

    private static string ComputeSha256(byte[] data)
    {
        var hash = SHA256.HashData(data);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}
