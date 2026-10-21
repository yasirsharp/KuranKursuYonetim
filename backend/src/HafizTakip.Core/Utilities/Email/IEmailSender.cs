namespace HafizTakip.Core.Utilities.Email;

public interface IEmailSender
{
    /// <summary>Düz metin e-posta gönderir; başarısız olursa false döner (hata loglanır, fırlatılmaz).</summary>
    Task<bool> SendAsync(string toAddress, string subject, string body, CancellationToken cancellationToken = default);
}
