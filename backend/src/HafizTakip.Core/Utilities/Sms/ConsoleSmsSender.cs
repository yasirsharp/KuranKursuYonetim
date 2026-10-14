using Microsoft.Extensions.Logging;

namespace HafizTakip.Core.Utilities.Sms;

/// <summary>
/// Geliştirme ortamı için: SMS göndermek yerine mesajı loga yazar.
/// Üretimde kullanılması başlangıçta engellenir (Program.cs).
/// </summary>
public class ConsoleSmsSender : ISmsSender
{
    private readonly ILogger<ConsoleSmsSender> _logger;

    public ConsoleSmsSender(ILogger<ConsoleSmsSender> logger)
    {
        _logger = logger;
    }

    public Task<bool> SendAsync(string phoneNumber, string message, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("[DEV SMS] {Phone}: {Message}", phoneNumber, message);
        return Task.FromResult(true);
    }
}