namespace HafizTakip.Core.Utilities.Sms;

public interface ISmsSender
{
    /// <summary>SMS gönderir. Başarılıysa true döner; hata fırlatmaz.</summary>
    Task<bool> SendAsync(string phoneNumber, string message, CancellationToken cancellationToken = default);
}