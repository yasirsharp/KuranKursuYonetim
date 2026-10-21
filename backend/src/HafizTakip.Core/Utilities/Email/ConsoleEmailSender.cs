using Microsoft.Extensions.Logging;

namespace HafizTakip.Core.Utilities.Email;

/// <summary>
/// Geliştirme ve test ortamı için: e-posta göndermek yerine mesajı loga yazar.
/// Üretimde kullanılması başlangıçta engellenir (Program.cs).
/// </summary>
public class ConsoleEmailSender : IEmailSender
{
    private readonly ILogger<ConsoleEmailSender> _logger;

    public ConsoleEmailSender(ILogger<ConsoleEmailSender> logger)
    {
        _logger = logger;
    }

    public Task<bool> SendAsync(string toAddress, string subject, string body, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("[DEV E-POSTA] {To} | {Subject}: {Body}", toAddress, subject, body);
        return Task.FromResult(true);
    }
}
