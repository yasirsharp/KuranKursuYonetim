using System.Security.Cryptography;
using System.Text;
using HafizTakip.Core.Utilities.Results;
using HafizTakip.Core.Utilities.Security.Otp;
using Microsoft.Extensions.Caching.Memory;

namespace HafizTakip.Business.Security;

/// <summary>Bir OTP kodunun hangi akış için üretildiği; farklı akışların kodları birbirinin yerine geçmez.</summary>
public enum OtpPurpose
{
    Login,
    CourseApplication,
    EmailLogin
}

public enum OtpVerification
{
    Valid,
    InvalidOrExpired,
    TooManyAttempts
}

/// <summary>
/// Telefon doğrulama kodlarının üretimi, saklanması ve doğrulanması.
/// - Kod cache'te yalnızca SHA-256 özeti olarak tutulur, hatalı deneme sayısı sınırlıdır.
/// - Gönderim sınırı (bekleme süresi + saatlik limit) numara başınadır ve tüm akışlarda ortaktır
///   (SMS maliyeti ve taciz riski numara başına).
/// </summary>
public sealed class OtpChallenge
{
    private readonly IMemoryCache _cache;
    private readonly OtpOptions _options;
    private readonly TimeProvider _timeProvider;

    public OtpChallenge(IMemoryCache cache, OtpOptions options, TimeProvider timeProvider)
    {
        _cache = cache;
        _options = options;
        _timeProvider = timeProvider;
    }

    public int ExpiryMinutes => _options.ExpiryMinutes;

    /// <summary>Bekleme süresi veya saatlik limit aşıldıysa hata, aksi halde null döner ve denemeyi kaydeder.</summary>
    public IResult? CheckAndRegisterSendAttempt(string phone)
    {
        var cooldownKey = $"otp:cooldown:{phone}";
        if (_cache.TryGetValue(cooldownKey, out DateTimeOffset retryAt))
        {
            var seconds = Math.Max(1, (int)Math.Ceiling((retryAt - _timeProvider.GetUtcNow()).TotalSeconds));
            return new ErrorResult($"Yeni kod istemek için {seconds} saniye bekleyin.", ResultErrorType.TooManyRequests);
        }

        var hourlyKey = $"otp:hourly:{phone}";
        var counter = _cache.GetOrCreate(hourlyKey, e =>
        {
            e.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1);
            return new SendCounter();
        })!;

        if (counter.Count >= _options.MaxSendsPerHour)
        {
            return new ErrorResult("Çok fazla kod istendi. Lütfen daha sonra tekrar deneyin.", ResultErrorType.TooManyRequests);
        }

        counter.Count++;
        if (_options.ResendCooldownSeconds > 0)
        {
            var cooldown = TimeSpan.FromSeconds(_options.ResendCooldownSeconds);
            _cache.Set(cooldownKey, _timeProvider.GetUtcNow().Add(cooldown), cooldown);
        }
        return null;
    }

    /// <summary>Yeni kod üretir ve özetini saklar (aynı amaç için önceki kod geçersiz olur). Kodun kendisi SMS ile gönderilmek üzere döner.</summary>
    public string Issue(OtpPurpose purpose, string phone)
    {
        var code = GenerateCode();
        _cache.Set(Key(purpose, phone), new OtpEntry(HashCode(phone, code)), TimeSpan.FromMinutes(_options.ExpiryMinutes));
        return code;
    }

    /// <summary>Kodu doğrular; kod tüketilmez (bkz. <see cref="Consume"/>). Hatalı denemeler sayılır.</summary>
    public OtpVerification Verify(OtpPurpose purpose, string phone, string code)
    {
        if (!_cache.TryGetValue(Key(purpose, phone), out OtpEntry? entry) || entry == null)
            return OtpVerification.InvalidOrExpired;

        if (entry.FailedAttempts >= _options.MaxVerifyAttempts)
        {
            Consume(purpose, phone);
            return OtpVerification.TooManyAttempts;
        }

        if (!CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(entry.CodeHash),
                Encoding.UTF8.GetBytes(HashCode(phone, code))))
        {
            entry.FailedAttempts++;
            return OtpVerification.InvalidOrExpired;
        }

        return OtpVerification.Valid;
    }

    public void Consume(OtpPurpose purpose, string phone) => _cache.Remove(Key(purpose, phone));

    private string GenerateCode()
    {
        if (!string.IsNullOrEmpty(_options.FixedCode))
            return _options.FixedCode;

        var max = (int)Math.Pow(10, _options.CodeLength);
        return RandomNumberGenerator.GetInt32(0, max).ToString(new string('0', _options.CodeLength));
    }

    private static string Key(OtpPurpose purpose, string phone) => $"otp:code:{purpose}:{phone}";

    private static string HashCode(string phone, string code) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes($"{phone}:{code}")));

    private sealed class OtpEntry
    {
        public OtpEntry(string codeHash) => CodeHash = codeHash;
        public string CodeHash { get; }
        public int FailedAttempts { get; set; }
    }

    private sealed class SendCounter
    {
        public int Count { get; set; }
    }
}
