namespace HafizTakip.Core.Utilities.Security.Otp;

public class OtpOptions
{
    public int CodeLength { get; set; } = 6;
    public int ExpiryMinutes { get; set; } = 3;

    /// <summary>Bir kod için izin verilen en fazla hatalı deneme.</summary>
    public int MaxVerifyAttempts { get; set; } = 5;

    /// <summary>Aynı numaraya iki kod isteği arasındaki en kısa süre (saniye).</summary>
    public int ResendCooldownSeconds { get; set; } = 60;

    /// <summary>Bir numaraya bir saatte gönderilebilecek en fazla kod.</summary>
    public int MaxSendsPerHour { get; set; } = 5;

    /// <summary>
    /// Yalnızca geliştirme ortamı: doluysa rastgele kod yerine bu kod kullanılır.
    /// Geliştirme dışı ortamda doluysa uygulama başlamaz.
    /// </summary>
    public string? FixedCode { get; set; }
}