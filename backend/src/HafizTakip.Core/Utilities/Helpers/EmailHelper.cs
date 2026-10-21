using System.Net.Mail;

namespace HafizTakip.Core.Utilities.Helpers;

/// <summary>E-posta adreslerini tek biçimde (boşluksuz, küçük harf) saklamak ve karşılaştırmak için yardımcı.</summary>
public static class EmailHelper
{
    public static string Normalize(string? email) =>
        string.IsNullOrWhiteSpace(email) ? string.Empty : email.Trim().ToLowerInvariant();

    /// <summary>Boş değer null olur; aksi halde normalize edilmiş adres.</summary>
    public static string? NormalizeOrNull(string? email)
    {
        var normalized = Normalize(email);
        return normalized.Length == 0 ? null : normalized;
    }

    public static bool IsValid(string? email)
    {
        var normalized = Normalize(email);
        if (normalized.Length is 0 or > 255 || normalized.Contains(' ')) return false;
        if (!MailAddress.TryCreate(normalized, out var address)) return false;
        // "ad@alan" gibi noktasız alan adlarını kabul etme; MailAddress görünen ad ekleyen biçimleri de reddetsin
        return address.Address == normalized && address.Host.Contains('.');
    }
}
