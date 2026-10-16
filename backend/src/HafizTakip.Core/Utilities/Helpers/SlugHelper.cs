using System.Globalization;
using System.Text;

namespace HafizTakip.Core.Utilities.Helpers;

/// <summary>Kurs adından URL'de kullanılabilecek kısa ad (slug) üretir: "Merkez Kur'an Kursu" → "merkez-kuran-kursu".</summary>
public static class SlugHelper
{
    public const int MaxLength = 80;
    private const string Fallback = "kurs";

    public static string From(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return Fallback;

        // Türkçe harfler önce elle çevrilir (ı/İ için ToLowerInvariant + aksan silme yetmez)
        var builder = new StringBuilder(text.Length);
        foreach (var ch in text.Trim())
        {
            builder.Append(ch switch
            {
                'ı' or 'I' or 'İ' => 'i',
                'ğ' or 'Ğ' => 'g',
                'ü' or 'Ü' => 'u',
                'ş' or 'Ş' => 's',
                'ö' or 'Ö' => 'o',
                'ç' or 'Ç' => 'c',
                _ => ch
            });
        }

        var decomposed = builder.ToString().Normalize(NormalizationForm.FormD).ToLowerInvariant();
        var slug = new StringBuilder(decomposed.Length);
        var pendingDash = false;
        foreach (var ch in decomposed)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(ch) == UnicodeCategory.NonSpacingMark) continue;
            if (ch == '\'' || ch == '’') continue; // "Kur'an" → "kuran"

            if (ch is >= 'a' and <= 'z' or >= '0' and <= '9')
            {
                if (pendingDash && slug.Length > 0) slug.Append('-');
                pendingDash = false;
                slug.Append(ch);
            }
            else
            {
                pendingDash = true;
            }
        }

        var result = slug.Length > MaxLength ? slug.ToString(0, MaxLength).TrimEnd('-') : slug.ToString();
        return result.Length == 0 ? Fallback : result;
    }

    /// <summary>baseSlug kullanılıyorsa en küçük boş "{baseSlug}-N" (N ≥ 2) döner.</summary>
    public static string MakeUnique(string baseSlug, IEnumerable<string> taken)
    {
        var used = new HashSet<string>(taken, StringComparer.OrdinalIgnoreCase);
        if (!used.Contains(baseSlug)) return baseSlug;

        for (var n = 2; ; n++)
        {
            var candidate = $"{baseSlug}-{n}";
            if (!used.Contains(candidate)) return candidate;
        }
    }
}
