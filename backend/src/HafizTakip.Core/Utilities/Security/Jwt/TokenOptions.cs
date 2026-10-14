using System.Text;

namespace HafizTakip.Core.Utilities.Security.Jwt;

public class TokenOptions
{
    /// <summary>HmacSha512 için anahtarın UTF-8 karşılığı en az 64 bayt olmalıdır.</summary>
    public const int MinimumSecurityKeyBytes = 64;

    public string Audience { get; set; } = string.Empty;
    public string Issuer { get; set; } = string.Empty;

    /// <summary>Access token geçerlilik süresi (dakika).</summary>
    public int AccessTokenExpiration { get; set; } = 60;

    /// <summary>Refresh token geçerlilik süresi (gün).</summary>
    public int RefreshTokenExpirationDays { get; set; } = 30;

    public string SecurityKey { get; set; } = string.Empty;

    /// <summary>Yapılandırma hatalarını döner; geçerliyse boş liste.</summary>
    public IReadOnlyList<string> Validate()
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(Issuer)) errors.Add("TokenOptions:Issuer boş olamaz.");
        if (string.IsNullOrWhiteSpace(Audience)) errors.Add("TokenOptions:Audience boş olamaz.");
        if (AccessTokenExpiration <= 0) errors.Add("TokenOptions:AccessTokenExpiration pozitif olmalı.");
        if (RefreshTokenExpirationDays <= 0) errors.Add("TokenOptions:RefreshTokenExpirationDays pozitif olmalı.");
        if (Encoding.UTF8.GetByteCount(SecurityKey ?? string.Empty) < MinimumSecurityKeyBytes)
            errors.Add($"TokenOptions:SecurityKey en az {MinimumSecurityKeyBytes} bayt olmalı (user-secrets veya ortam değişkeni ile verin).");
        return errors;
    }
}