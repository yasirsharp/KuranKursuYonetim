namespace HafizTakip.Core.Constants;

/// <summary>
/// Türkiye ayetberkenar hafız mushafı (604 sayfa) sabitleri.
/// Bu düzende cüz 1 = s.1-21, cüz n = s.(20n-18)…(20n+1) ve cüz 30 = s.582-604'tür;
/// sayfa → cüz eşlemesi bu yüzden formülle kesin hesaplanır. Sure başlangıç sayfaları
/// veritabanındaki quran_surahs tablosundadır (database/02_quran_reference.sql).
/// </summary>
public static class QuranConstants
{
    public const int TotalSuras = 114;
    public const int TotalPages = 604;
    public const int TotalJuz = 30;
    public const int TotalAyahs = 6236;

    public static bool IsValidPage(int page) => page is >= 1 and <= TotalPages;

    public static bool IsValidJuz(int juz) => juz is >= 1 and <= TotalJuz;

    /// <summary>Sayfanın bulunduğu cüz.</summary>
    public static int JuzOfPage(int page)
    {
        if (!IsValidPage(page))
            throw new ArgumentOutOfRangeException(nameof(page), page, $"Sayfa 1-{TotalPages} arasında olmalıdır.");

        return page <= 21 ? 1 : Math.Min(TotalJuz, (page - 2) / 20 + 1);
    }

    /// <summary>Cüzün ilk sayfası.</summary>
    public static int JuzStartPage(int juz)
    {
        if (!IsValidJuz(juz))
            throw new ArgumentOutOfRangeException(nameof(juz), juz, $"Cüz 1-{TotalJuz} arasında olmalıdır.");

        return juz == 1 ? 1 : 20 * juz - 18;
    }

    /// <summary>Cüzün son sayfası.</summary>
    public static int JuzEndPage(int juz) =>
        juz == TotalJuz ? TotalPages : JuzStartPage(juz + 1) - 1;
}
