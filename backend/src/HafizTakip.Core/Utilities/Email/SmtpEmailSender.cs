using System.Net;
using System.Net.Mail;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace HafizTakip.Core.Utilities.Email;

/// <summary>SMTP ile gönderim (ör. barındırma firmasının e-posta sunucusu).</summary>
public class SmtpEmailSender : IEmailSender
{
    private readonly SmtpSettings _settings;
    private readonly ILogger<SmtpEmailSender> _logger;

    public SmtpEmailSender(IOptions<EmailSettings> options, ILogger<SmtpEmailSender> logger)
    {
        _settings = options.Value.Smtp;
        _logger = logger;
    }

    public async Task<bool> SendAsync(string toAddress, string subject, string body, CancellationToken cancellationToken = default)
    {
        try
        {
            using var message = new MailMessage
            {
                From = new MailAddress(_settings.FromAddress, _settings.FromName, Encoding.UTF8),
                Subject = subject,
                SubjectEncoding = Encoding.UTF8,
                Body = body,
                BodyEncoding = Encoding.UTF8,
                IsBodyHtml = false
            };
            message.To.Add(toAddress);

            using var client = new SmtpClient(_settings.Host, _settings.Port)
            {
                EnableSsl = _settings.UseSsl,
                Credentials = string.IsNullOrEmpty(_settings.UserName)
                    ? null
                    : new NetworkCredential(_settings.UserName, _settings.Password)
            };
            await client.SendMailAsync(message, cancellationToken);
            return true;
        }
        catch (Exception ex)
        {
            // Alıcı adresi loga yazılmaz (kişisel veri)
            _logger.LogError(ex, "E-posta gönderilemedi (SMTP {Host}:{Port}).", _settings.Host, _settings.Port);
            return false;
        }
    }
}
