using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;
using Seven.Domain.Common;
using Seven.Infrastructure.Configuration;

namespace Seven.Infrastructure.Mail;

public interface IEmailService
{
    Task<WebResponseContent> SendAsync(string to, string subject, string htmlBody, CancellationToken ct = default);
}

public class MailKitEmailService : IEmailService
{
    private readonly MailOptions _options;
    private readonly FeatureOptions _features;
    private readonly ILogger<MailKitEmailService> _logger;

    public MailKitEmailService(
        IOptions<MailOptions> options,
        IOptions<FeatureOptions> features,
        ILogger<MailKitEmailService> logger)
    {
        _options = options.Value;
        _features = features.Value;
        _logger = logger;
    }

    public async Task<WebResponseContent> SendAsync(string to, string subject, string htmlBody, CancellationToken ct = default)
    {
        if (!_features.Mail || !_options.Enabled)
            return WebResponseContent.Error("邮件服务未启用");
        if (string.IsNullOrWhiteSpace(to))
            return WebResponseContent.Error("收件人不能为空");

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(_options.FromName, _options.From));
        message.To.Add(MailboxAddress.Parse(to));
        message.Subject = subject;
        message.Body = new TextPart("html") { Text = htmlBody };

        try
        {
            using var client = new SmtpClient();
            await client.ConnectAsync(_options.Host, _options.Port,
                _options.UseSsl ? SecureSocketOptions.StartTls : SecureSocketOptions.Auto, ct);
            if (!string.IsNullOrWhiteSpace(_options.UserName))
                await client.AuthenticateAsync(_options.UserName, _options.Password, ct);
            await client.SendAsync(message, ct);
            await client.DisconnectAsync(true, ct);
            return WebResponseContent.Ok("发送成功");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "发送邮件失败");
            return WebResponseContent.Error($"发送失败: {ex.Message}");
        }
    }
}
