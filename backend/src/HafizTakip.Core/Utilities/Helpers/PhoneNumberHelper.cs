namespace HafizTakip.Core.Utilities.Helpers;

/// <summary>
/// Telefon numaralarını tek bir kanonik biçimde saklamak için yardımcı.
/// Kanonik biçim: ülke kodu ve baştaki 0 olmadan 10 hane (örn. 5321234567).
/// </summary>
public static class PhoneNumberHelper
{
    public static string Normalize(string? phoneNumber)
    {
        if (string.IsNullOrWhiteSpace(phoneNumber))
            return string.Empty;

        var digits = new string(phoneNumber.Where(char.IsDigit).ToArray());

        if (digits.StartsWith("90") && digits.Length == 12)
            digits = digits[2..];
        else if (digits.StartsWith("0") && digits.Length == 11)
            digits = digits[1..];

        return digits;
    }

    /// <summary>Normalize edilmiş numara 5 ile başlayan 10 haneli bir GSM numarası mı?</summary>
    public static bool IsValidMobile(string? phoneNumber)
    {
        var normalized = Normalize(phoneNumber);
        return normalized.Length == 10 && normalized[0] == '5';
    }

    /// <summary>SMS/WhatsApp sağlayıcıları için uluslararası biçim (905321234567).</summary>
    public static string ToInternational(string? phoneNumber) => "90" + Normalize(phoneNumber);
}
