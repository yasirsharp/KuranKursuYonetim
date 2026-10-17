namespace HafizTakip.Core.Enums;

/// <summary>Kurs takvimindeki kayıt türleri.</summary>
public static class CalendarEventType
{
    /// <summary>Resmi tatil (1 Ocak, 23 Nisan, 29 Ekim …).</summary>
    public const string OfficialHoliday = "official_holiday";

    /// <summary>Dini bayram (Ramazan ve Kurban Bayramı).</summary>
    public const string ReligiousHoliday = "religious_holiday";

    /// <summary>Ara tatil / yarıyıl tatili.</summary>
    public const string Break = "break";

    /// <summary>Kursun kapalı olduğu diğer günler (idari izin, tadilat …).</summary>
    public const string Closure = "closure";

    /// <summary>Etkinlik (hatim merasimi, veli toplantısı …); kendiliğinden ders günü düşmez.</summary>
    public const string Event = "event";

    public static readonly IReadOnlyCollection<string> All = new[] { OfficialHoliday, ReligiousHoliday, Break, Closure, Event };
}
