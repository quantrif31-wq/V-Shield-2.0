namespace API.Services.Agent;

public class MailOptions
{
    public const string SectionName = "Mail";
    public string Host { get; set; } = "";
    public int Port { get; set; } = 587;
    public string? User { get; set; }
    public string? Password { get; set; }
    public string From { get; set; } = "no-reply@v-shield.local";
    public string? FromName { get; set; }
    public bool EnableSsl { get; set; } = true;
    /// <summary>Cho phép gửi tới email ngoài danh bạ nhân viên hay không.</summary>
    public bool AllowExternal { get; set; } = false;
    /// <summary>Domain email công ty (vd: v-shield.site). Cấu hình 1 lần trên VPS.</summary>
    public string Domain { get; set; } = "";
}

public sealed record MailMessage(
    IReadOnlyList<string> To,
    string Subject,
    string BodyHtml,
    IReadOnlyList<string>? Cc = null,
    string? FromEmail = null,
    string? FromName = null);

public sealed record MailSendResult(bool Success, string? MessageId, string? Error);

public interface IMailService
{
    Task<MailSendResult> SendAsync(MailMessage message, CancellationToken cancellationToken = default);
}

public class MailService : IMailService
{
    private readonly MailOptions _options;
    private readonly ILogger<MailService> _logger;

    public MailService(Microsoft.Extensions.Options.IOptions<MailOptions> options, ILogger<MailService> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public async Task<MailSendResult> SendAsync(MailMessage message, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_options.Host))
        {
            return new MailSendResult(false, null, "Hạ tầng email chưa được cấu hình (Mail:Host trống). Vui lòng liên hệ quản trị viên.");
        }

        try
        {
            using var client = new MailKit.Net.Smtp.SmtpClient();
            await client.ConnectAsync(_options.Host, _options.Port, _options.EnableSsl, cancellationToken);

            if (!string.IsNullOrWhiteSpace(_options.User))
            {
                await client.AuthenticateAsync(_options.User, _options.Password ?? "", cancellationToken);
            }

            var mime = new MimeKit.MimeMessage();
            var fromEmail = string.IsNullOrWhiteSpace(message.FromEmail) ? _options.From : message.FromEmail;
            var fromName = string.IsNullOrWhiteSpace(message.FromName)
                ? (_options.FromName ?? "")
                : message.FromName;
            mime.From.Add(new MimeKit.MailboxAddress(string.IsNullOrWhiteSpace(fromName) ? null : fromName, fromEmail));
            foreach (var to in message.To)
            {
                if (!string.IsNullOrWhiteSpace(to))
                    mime.To.Add(MimeKit.MailboxAddress.Parse(to));
            }
            foreach (var cc in message.Cc ?? Array.Empty<string>())
            {
                if (!string.IsNullOrWhiteSpace(cc))
                    mime.Cc.Add(MimeKit.MailboxAddress.Parse(cc));
            }

            mime.Subject = message.Subject;
            var builder = new MimeKit.BodyBuilder { HtmlBody = message.BodyHtml };
            mime.Body = builder.ToMessageBody();

            await client.SendAsync(mime, cancellationToken);
            await client.DisconnectAsync(true, cancellationToken);

            _logger.LogInformation("Email gửi thành công từ {From} tới {To}, subject={Subject}",
                _options.From, string.Join(";", message.To), message.Subject);

            return new MailSendResult(true, mime.MessageId, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Gửi email thất bại");
            return new MailSendResult(false, null, $"Gửi email thất bại: {ex.Message}");
        }
    }
}