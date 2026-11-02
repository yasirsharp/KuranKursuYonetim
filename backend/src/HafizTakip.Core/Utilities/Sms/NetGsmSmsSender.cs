using System.Text;
using System.Xml.Linq;
using HafizTakip.Core.Utilities.Helpers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace HafizTakip.Core.Utilities.Sms;

/// <summary>
/// NetGSM OTP SMS servisi (POST /sms/send/otp, XML) ile tek kullanımlık kod gönderimi.
/// OTP kanalı toplu SMS kuyruğuna girmez, en geç 3 dakikada iletilir; yalnızca OTP paketiyle çalışır.
/// Kurallar (NetGSM): Türkçe karakter desteklenmez, en fazla 1 SMS boyu, tek tek gönderim, dakikada en çok 100 istek.
/// Bu yüzden mesaj Türkçe karakterlerden arındırılır ve tek SMS boyunda olmalıdır. Bu gönderici yalnızca
/// doğrulama kodları içindir; bilgilendirme mesajları e-postayla gider.
/// Kaynak: NetGSM'in resmi OTP örneği (github.com/netgsm1/otp) ve https://www.netgsm.com.tr/sms/otp-sms
/// </summary>
// TODO(netgsm): Gerçek bir NetGSM hesabıyla uçtan uca doğrulanmalı (deploy/sms-dene.ps1 ile).
public class NetGsmSmsSender : ISmsSender
{
    /// <summary>Alfanümerik başlıkla tek SMS boyu (operatör eki sonrası).</summary>
    public const int MaxLength = 155;

    private static readonly IReadOnlyDictionary<string, string> ErrorMeanings = new Dictionary<string, string>
    {
        ["20"] = "Mesaj metni veya boyu uygun değil (Türkçe karakter ya da 1 SMS boyunu aşan metin).",
        ["30"] = "Kullanıcı adı/şifre geçersiz, API erişim izni yok veya sunucunun IP adresi NetGSM'de izinli değil.",
        ["40"] = "Gönderici adı (başlık) onaylı değil.",
        ["41"] = "Gönderici adı (başlık) onaylı değil.",
        ["50"] = "Telefon numarası geçersiz.",
        ["60"] = "Hesapta OTP SMS paketi tanımlı değil veya paket bitti.",
        ["70"] = "İstek parametreleri hatalı.",
        // Denendi: şifrede & veya < varsa (XML'de doğru kaçışlanmış olsa bile) NetGSM 73 döner
        ["73"] = "İstek parametreleri reddedildi (NetGSM API şifresinde & veya < karakteri olmamalı).",
        ["80"] = "Gönderim sınırı aşıldı (dakikada en çok 100 OTP).",
        ["100"] = "NetGSM sistem hatası."
    };

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
        var text = ToOtpText(message);
        if (text.Length > MaxLength)
            _logger.LogWarning("OTP mesajı {Length} karakter; NetGSM tek SMS boyunu ({Max}) aşan mesajı reddeder.", text.Length, MaxLength);

        var body = BuildRequestXml(_settings, PhoneNumberHelper.Normalize(phoneNumber), text);
        var url = $"{_settings.BaseUrl.TrimEnd('/')}/sms/send/otp";

        try
        {
            using var content = new StringContent(body, Encoding.UTF8, "text/xml");
            using var response = await _httpClient.PostAsync(url, content, cancellationToken);
            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

            var (success, code, error) = ParseResponse(responseBody);
            if (response.IsSuccessStatusCode && success) return true;

            // Telefon ve mesaj loglanmaz; yalnızca NetGSM'in yanıt kodu, anlamı ve kendi hata metni
            var meaning = code != null && ErrorMeanings.TryGetValue(code, out var m) ? m : "Bilinmeyen yanıt.";
            _logger.LogWarning("NetGSM OTP gönderimi başarısız. HTTP {Status}, kod {Code}: {Meaning} (NetGSM: {Error})",
                (int)response.StatusCode, code ?? "-", meaning, error ?? "-");
            return false;
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            _logger.LogError(ex, "NetGSM OTP isteği başarısız (bağlantı veya zaman aşımı).");
            return false;
        }
    }

    /// <summary>NetGSM OTP isteği. Değerler XML olarak kaçışlanır (şifrede veya başlıkta özel karakter olabilir).</summary>
    public static string BuildRequestXml(NetGsmSettings settings, string normalizedPhone, string text)
    {
        var document = new XDocument(
            new XDeclaration("1.0", "UTF-8", null),
            new XElement("mainbody",
                new XElement("header",
                    new XElement("usercode", settings.UserCode),
                    new XElement("password", settings.Password),
                    new XElement("msgheader", settings.Header)),
                new XElement("body",
                    new XElement("msg", new XCData(text)),
                    new XElement("no", normalizedPhone))));
        return document.Declaration + document.ToString(SaveOptions.DisableFormatting);
    }

    /// <summary>
    /// Yanıt: &lt;xml&gt;&lt;main&gt;&lt;code&gt;0&lt;/code&gt;&lt;jobID&gt;…&lt;/jobID&gt;&lt;/main&gt;&lt;/xml&gt;;
    /// hatada &lt;code&gt;30&lt;/code&gt;&lt;error&gt;Kullanici bilgisi bulunamadi&lt;/error&gt;.
    /// Başarı: kod 0 (veya kod yokken iş numarası). Hata kodları <see cref="ErrorMeanings"/>.
    /// </summary>
    public static (bool Success, string? Code, string? Error) ParseResponse(string responseBody)
    {
        try
        {
            var main = XDocument.Parse(responseBody).Root?.Element("main");
            var code = NullIfEmpty(main?.Element("code")?.Value);
            var jobId = NullIfEmpty(main?.Element("jobID")?.Value);
            var error = NullIfEmpty(main?.Element("error")?.Value);
            if (code is "0" or "00") return (true, code, null);
            if (code == null && jobId != null) return (true, null, null);
            return (false, code, error);
        }
        catch (System.Xml.XmlException)
        {
            return (false, null, null);
        }
    }

    private static string? NullIfEmpty(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    /// <summary>Türkçe harfler ASCII karşılıklarına çevrilir (OTP kanalı Türkçe karakter desteklemez).</summary>
    public static string ToOtpText(string message)
    {
        var builder = new StringBuilder(message.Length);
        foreach (var c in message)
        {
            builder.Append(c switch
            {
                'ı' => 'i', 'İ' => 'I',
                'ş' => 's', 'Ş' => 'S',
                'ğ' => 'g', 'Ğ' => 'G',
                'ü' => 'u', 'Ü' => 'U',
                'ö' => 'o', 'Ö' => 'O',
                'ç' => 'c', 'Ç' => 'C',
                'â' => 'a', 'Â' => 'A',
                'î' => 'i', 'Î' => 'I',
                'û' => 'u', 'Û' => 'U',
                '“' or '”' => '"',
                '‘' or '’' => '\'',
                _ => c
            });
        }
        return builder.ToString().Trim();
    }
}
