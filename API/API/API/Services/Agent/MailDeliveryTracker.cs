using API.Data;
using Microsoft.EntityFrameworkCore;

namespace API.Services.Agent;

/// <summary>
/// Theo dõi trạng thái gửi THẬT (Delivered / Bounced) bằng cách đọc log outbound
/// của mail server tự host (Poste.io Haraka) — mount thư mục log vào container api.
/// Cấu hình qua env MAIL_LOG_PATH (để trống = tắt).
/// </summary>
public sealed class MailDeliveryTracker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<MailDeliveryTracker> _logger;
    private readonly string _logPath;

    public MailDeliveryTracker(IServiceScopeFactory scopeFactory, IConfiguration config, ILogger<MailDeliveryTracker> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _logPath = Environment.GetEnvironmentVariable("MAIL_LOG_PATH") ?? config["Mail:LogPath"] ?? "";
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (string.IsNullOrWhiteSpace(_logPath))
        {
            _logger.LogInformation("MailDeliveryTracker: MAIL_LOG_PATH chưa cấu hình — tắt theo dõi delivery.");
            return;
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await TickAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "MailDeliveryTracker tick lỗi");
            }
            await Task.Delay(TimeSpan.FromSeconds(45), stoppingToken);
        }
    }

    private async Task TickAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var pending = await db.AgentDrafts
            .Where(d => d.Status == "Sent" && d.MessageId != null)
            .ToListAsync(ct);
        if (pending.Count == 0) return;

        var logFile = ResolveLogFile();
        if (logFile == null) return;

        var offsetKey = "agent.mailLogOffset";
        var cfg = await db.SystemConfigs.FirstOrDefaultAsync(c => c.Key == offsetKey, ct);
        long offset = 0;
        if (cfg != null && long.TryParse(cfg.Value, out var o)) offset = o;

        var size = new FileInfo(logFile).Length;
        if (size <= offset) return;

        string content;
        await using (var fs = File.Open(logFile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
        {
            fs.Seek(offset, SeekOrigin.Begin);
            using var reader = new StreamReader(fs);
            content = await reader.ReadToEndAsync(ct);
        }

        if (cfg == null)
        {
            cfg = new API.Models.SystemConfig { Key = offsetKey, Value = size.ToString(), UpdatedAtUtc = DateTime.UtcNow };
            db.SystemConfigs.Add(cfg);
        }
        else
        {
            cfg.Value = size.ToString();
            cfg.UpdatedAtUtc = DateTime.UtcNow;
        }
        await db.SaveChangesAsync(ct);

        foreach (var line in content.Split('\n'))
        {
            var l = line.ToLowerInvariant();
            var isDelivered = l.Contains("delivered") && l.Contains("outbound");
            var isBounce = l.Contains("outbound") && (l.Contains("bounce") || l.Contains("permanent failure") || l.Contains("550 ") || l.Contains("554 "));
            if (!isDelivered && !isBounce) continue;

            foreach (var d in pending)
            {
                var recipients = (d.To ?? "").Split(';', StringSplitOptions.RemoveEmptyEntries)
                    .Select(x => x.Trim().ToLowerInvariant())
                    .Where(x => x.Length > 0)
                    .ToArray();
                if (recipients.Length == 0 || !recipients.Any(r => l.Contains(r))) continue;

                if (isDelivered && d.Status == "Sent")
                {
                    d.Status = "Delivered";
                    d.SendError = null;
                    await db.SaveChangesAsync(ct);
                    _logger.LogInformation("Draft #{Id} delivered tới {To}", d.AgentDraftId, d.To);
                }
                else if (isBounce && (d.Status == "Sent" || d.Status == "Delivered"))
                {
                    d.Status = "Bounced";
                    d.SendError = "Email bị trả lại (bounce). Kiểm tra địa chỉ người nhận hoặc DNS/SPF.";
                    await db.SaveChangesAsync(ct);
                    _logger.LogWarning("Draft #{Id} bounced tới {To}", d.AgentDraftId, d.To);
                }
            }
        }
    }

    private string? ResolveLogFile()
    {
        try
        {
            if (Directory.Exists(_logPath))
            {
                var file = new DirectoryInfo(_logPath)
                    .GetFiles()
                    .OrderByDescending(f => f.LastWriteTimeUtc)
                    .FirstOrDefault();
                return file?.FullName;
            }
            if (File.Exists(_logPath)) return _logPath;
        }
        catch { }
        return null;
    }
}