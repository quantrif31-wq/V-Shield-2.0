using System.Diagnostics;

namespace API.Services.ImportExport.AI;

public class OcrService : IOcrService
{
    private readonly ILogger<OcrService> _logger;

    public OcrService(ILogger<OcrService> logger)
    {
        _logger = logger;
    }

    public bool CanHandle(string fileName)
    {
        var ext = Path.GetExtension(fileName)?.TrimStart('.').ToLowerInvariant();
        return ext is "pdf" or "png" or "jpg" or "jpeg" or "tiff" or "tif" or "bmp" or "gif" or "doc" or "docx";
    }

    public async Task<OcrResult> ExtractTextAsync(Stream fileStream, string fileName, CancellationToken ct = default)
    {
        var ext = Path.GetExtension(fileName)?.TrimStart('.').ToLowerInvariant() ?? "bin";
        var memoryStream = new MemoryStream();
        await fileStream.CopyToAsync(memoryStream, ct);
        var fileBytes = memoryStream.ToArray();

        // Try Tesseract CLI if available
        var tesseractResult = await TryTesseractCliAsync(fileBytes, ext, ct);
        if (tesseractResult != null)
            return tesseractResult;

        // Try Python OCR script if available
        var pythonResult = await TryPythonOcrAsync(fileBytes, ext, ct);
        if (pythonResult != null)
            return pythonResult;

        // Fallback: extract raw text for PDF
        if (ext == "pdf")
        {
            var text = ExtractTextFromPdf(fileBytes);
            if (!string.IsNullOrWhiteSpace(text))
            {
                return new OcrResult
                {
                    Success = true,
                    RawText = text,
                    Pages = [new OcrPageResult { PageNumber = 1, Text = text, Confidence = 0.7 }],
                };
            }
        }

        // Last resort: return the file content as base64 for AI to process
        var base64 = Convert.ToBase64String(fileBytes);
        return new OcrResult
        {
            Success = true,
            RawText = $"[FILE:{fileName}]\n[FORMAT:{ext}]\n[SIZE:{fileBytes.Length}]\n[BASE64:{base64[..Math.Min(base64.Length, 500)]}...]",
            ErrorMessage = "OCR engine not available. File content sent as base64 for AI processing."
        };
    }

    private async Task<OcrResult?> TryTesseractCliAsync(byte[] fileBytes, string ext, CancellationToken ct)
    {
        try
        {
            var tempDir = Path.Combine(Path.GetTempPath(), "vshield_ocr");
            Directory.CreateDirectory(tempDir);
            var tempInput = Path.Combine(tempDir, $"input_{Guid.NewGuid():N}.{ext}");
            await File.WriteAllBytesAsync(tempInput, fileBytes, ct);

            var psi = new ProcessStartInfo
            {
                FileName = "tesseract",
                Arguments = $"\"{tempInput}\" stdout --psm 6 -l vie+eng",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };

            using var process = new Process { StartInfo = psi };
            process.Start();
            var output = await process.StandardOutput.ReadToEndAsync(ct);
            var error = await process.StandardError.ReadToEndAsync(ct);
            await process.WaitForExitAsync(ct);

            try { File.Delete(tempInput); } catch { }

            if (process.ExitCode == 0 && !string.IsNullOrWhiteSpace(output))
            {
                return new OcrResult
                {
                    Success = true,
                    RawText = output,
                    Pages = [new OcrPageResult { PageNumber = 1, Text = output, Confidence = 0.8 }],
                };
            }

            _logger.LogWarning("Tesseract CLI failed: {Error}", error);
        }
        catch (Exception ex)
        {
            _logger.LogDebug("Tesseract not available: {Message}", ex.Message);
        }

        return null;
    }

    private async Task<OcrResult?> TryPythonOcrAsync(byte[] fileBytes, string ext, CancellationToken ct)
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "python",
                Arguments = "-c \"import pytesseract; print('ok')\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };

            using var testProcess = new Process { StartInfo = psi };
            testProcess.Start();
            var testOutput = await testProcess.StandardOutput.ReadToEndAsync(ct);
            await testProcess.WaitForExitAsync(ct);

            if (testProcess.ExitCode != 0)
                return null;

            var tempDir = Path.Combine(Path.GetTempPath(), "vshield_ocr");
            Directory.CreateDirectory(tempDir);
            var tempInput = Path.Combine(tempDir, $"input_{Guid.NewGuid():N}.{ext}");

            try
            {
                await File.WriteAllBytesAsync(tempInput, fileBytes, ct);

                var script = $@"
import sys
import base64
try:
    import pytesseract
    from PIL import Image
    img = Image.open(r'{tempInput}')
    text = pytesseract.image_to_string(img, lang='vie+eng')
    print(text)
except Exception as e:
    print(f'ERROR:{{e}}', file=sys.stderr)
    sys.exit(1)
";
                var runPsi = new ProcessStartInfo
                {
                    FileName = "python",
                    Arguments = $"-c \"{script.Replace("\"", "\\\"")}\"",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                };

                using var process = new Process { StartInfo = runPsi };
                process.Start();
                var output = await process.StandardOutput.ReadToEndAsync(ct);
                var error = await process.StandardError.ReadToEndAsync(ct);
                await process.WaitForExitAsync(ct);

                if (process.ExitCode == 0 && !string.IsNullOrWhiteSpace(output))
                {
                    return new OcrResult
                    {
                        Success = true,
                        RawText = output,
                        Pages = [new OcrPageResult { PageNumber = 1, Text = output, Confidence = 0.85 }],
                    };
                }

                _logger.LogWarning("Python OCR failed: {Error}", error);
            }
            finally
            {
                try { File.Delete(tempInput); } catch { }
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug("Python OCR not available: {Message}", ex.Message);
        }

        return null;
    }

    private static string ExtractTextFromPdf(byte[] fileBytes)
    {
        try
        {
            var content = System.Text.Encoding.UTF8.GetString(fileBytes);
            var sb = new System.Text.StringBuilder();
            var inStream = false;
            foreach (var line in content.Split('\n'))
            {
                var t = line.Trim();
                if (t.StartsWith("stream")) { inStream = true; continue; }
                if (t == "endstream") { inStream = false; continue; }
                if (inStream) continue;
                if (t.StartsWith("BT") || t.StartsWith("ET") || t.StartsWith("Td") || t.StartsWith("Tf")) continue;
                if (t.StartsWith("/") || t.StartsWith("<<") || t.StartsWith(">>") || t == "endobj" || t == "obj") continue;

                var text = ExtractTextFromPdfLine(t);
                if (!string.IsNullOrWhiteSpace(text))
                    sb.AppendLine(text);
            }
            return sb.ToString();
        }
        catch
        {
            return string.Empty;
        }
    }

    private static string ExtractTextFromPdfLine(string line)
    {
        var result = new System.Text.StringBuilder();
        var inParens = false;
        foreach (var ch in line)
        {
            if (ch == '(') { inParens = true; continue; }
            if (ch == ')') { inParens = false; result.Append(' '); continue; }
            if (inParens) result.Append(ch);
        }
        return result.ToString().Trim();
    }
}
