using HafizTakip.Core.Utilities.Helpers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace HafizTakip.Core.Utilities.Sms;

/// <summary>
/// NetGSM HTTP GET SMS API'si ile tekil SMS gönderimi.
/// Başarılı yanıt "00", "01" veya "02" ile başlar (ardından görev numarası gelir).
/// </summary>
// TODO(netgsm): Gerçek bir NetGSM hesabıyla henüz denenmedi. Proje sahibi daha sonra hesap açıp onaylı
// mesaj başlığı alacak; ardından Sms:NetGsm:UserCode / Password / Header ortam değişkenleriyle verilip
// gönderim, hata kodları ve Türkçe karakterli mesajlar uçtan uca doğrulanmalı. Canlıya çıkıştan önce zorunlu.
public class NetGsmSmsSender : ISmsSender
{
    private static readonly string[] SuccessCodes = { "00", "01", "02" };

    private readonly HttpClient _httpClient;
    private readonly NetGsmSettings _settings;
    private readonly ILogger<NetGsmSmsSender> _logger;

    public NetGsmSmsSender(HttpClient httpClient, IOptions<SmsSettings> settings, ILogger<NetGsmSmsSender> logger)
    {
        _httpClient = httpClient;
        _settings = settings.Value.NetGsm;
        _logger = logger;
    }

    public async Task<bool> SendAsync(string phoneNumber, string message, CancellationToken cancellationToken = default)
    {
        var query = new Dictionary<string, string>
        {
            ["usercode"] = _settings.UserCode,
            ["password"] = _settings.Password,
            ["gsmno"] = PhoneNumberHelper.ToInternational(phoneNumber),
            ["message"] = message,
            ["msgheader"] = _settings.Header,
            ["dil"] = "TR"
        };
        var queryString = string.Join("&", query.Select(kv => $"{kv.Key}={Uri.EscapeDataString(kv.Value)}"));
        var url = $"{_settings.BaseUrl.TrimEnd('/')}/sms/send/get?{queryString}";

        try
        {
            using var response = await _httpClient.GetAsync(url, cancellationToken);
            var body = (await response.Content.ReadAsStringAsync(cancellationToken)).Trim();

            if (response.IsSuccessStatusCode && SuccessCodes.Any(code => body.StartsWith(code)))
                return true;

            // Yanıt gövdesi yalnızca hata kodu içerir; telefon/mesaj loglanmaz
            _logger.LogWarning("NetGSM SMS gönderimi başarısız. HTTP {Status}, yanıt kodu: {Code}", (int)response.StatusCode, body);
            return false;
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            _logger.LogError(ex, "NetGSM SMS isteği başarısız.");
            return false;
        }
    }
}