using HafizTakip.Core.Enums;
using HafizTakip.Entities.Dtos;

namespace HafizTakip.Business.Calendar;

/// <summary>
/// Türkiye resmi tatilleri ve dini bayramları (2429 sayılı Kanun).
/// Resmi tatiller sabit tarihlidir ve her yıl için hesaplanır. Dini bayramlar hicri takvime
/// bağlı olduğundan Diyanet'in yayımladığı tarihlerden tabloya işlenir; tabloda olmayan yıl
/// <see cref="GetHolidays"/> sonucunda "eksik yıl" olarak bildirilir.
/// Arife günleri öğleden sonra yarım gün tatildir; tam gün tatil sayılmaz (IsDayOff = false).
/// Hükümetin ilan ettiği ek idari izinler (köprü günleri) bu listede yoktur; kurs bunları elle ekler.
/// </summary>
public static class TurkishHolidayCalendar
{
    /// <summary>
    /// Ramazan ve Kurban Bayramı'nın ilk günleri.
    /// TODO: Her yıl Diyanet'in "Dini Günler" takvimi yayımlandığında yeni yılı ekleyin ve doğrulayın.
    /// 2025–2027 Diyanet takvimiyle karşılaştırıldı (Ekim 2026).
    /// </summary>
    private static readonly IReadOnlyDictionary<int, (DateOnly Ramazan, DateOnly Kurban)> ReligiousHolidays =
        new Dictionary<int, (DateOnly, DateOnly)>
        {
            [2025] = (new DateOnly(2025, 3, 30), new DateOnly(2025, 6, 6)),
            [2026] = (new DateOnly(2026, 3, 20), new DateOnly(2026, 5, 27)),
            [2027] = (new DateOnly(2027, 3, 9), new DateOnly(2027, 5, 16)),
        };

    private const int RamazanBayramiDays = 3;
    private const int KurbanBayramiDays = 4;
    private const string HalfDayNote = "Öğleden sonra yarım gün resmi tatil.";

    /// <summary>Aralıkla kesişen tatiller (başlangıç tarihine göre sıralı) ve dini bayram tarihi tabloda bulunmayan yıllar.</summary>
    public static (List<HolidaySuggestionDto> Holidays, List<int> MissingReligiousYears) GetHolidays(DateOnly from, DateOnly to)
    {
        var holidays = new List<HolidaySuggestionDto>();
        var missingYears = new List<int>();

        for (var year = from.Year; year <= to.Year; year++)
        {
            holidays.AddRange(OfficialHolidays(year));

            if (ReligiousHolidays.TryGetValue(year, out var religious))
                holidays.AddRange(ReligiousHolidaysOf(year, religious.Ramazan, religious.Kurban));
            else
                missingYears.Add(year);
        }

        var inRange = holidays
            .Where(h => h.StartDate <= to && h.EndDate >= from)
            .OrderBy(h => h.StartDate).ThenBy(h => h.Key)
            .ToList();

        // Bayramlar hicri takvimle her yıl ~11 gün kayar ve herhangi bir aya düşebilir;
        // bu yüzden aralığın dokunduğu her eksik yıl bildirilir
        return (inRange, missingYears);
    }

    private static IEnumerable<HolidaySuggestionDto> OfficialHolidays(int year)
    {
        yield return Official(year, "yilbasi", "Yılbaşı", 1, 1);
        yield return Official(year, "ulusal-egemenlik", "Ulusal Egemenlik ve Çocuk Bayramı", 4, 23);
        yield return Official(year, "emek", "Emek ve Dayanışma Günü", 5, 1);
        yield return Official(year, "genclik-spor", "Atatürk'ü Anma, Gençlik ve Spor Bayramı", 5, 19);
        yield return Official(year, "demokrasi", "Demokrasi ve Millî Birlik Günü", 7, 15);
        yield return Official(year, "zafer", "Zafer Bayramı", 8, 30);
        yield return new HolidaySuggestionDto
        {
            Key = $"{year}-cumhuriyet-arife",
            Title = "Cumhuriyet Bayramı Arifesi",
            EventType = CalendarEventType.OfficialHoliday,
            StartDate = new DateOnly(year, 10, 28),
            EndDate = new DateOnly(year, 10, 28),
            IsDayOff = false,
            Description = HalfDayNote
        };
        yield return Official(year, "cumhuriyet", "Cumhuriyet Bayramı", 10, 29);
    }

    private static HolidaySuggestionDto Official(int year, string key, string title, int month, int day) => new()
    {
        Key = $"{year}-{key}",
        Title = title,
        EventType = CalendarEventType.OfficialHoliday,
        StartDate = new DateOnly(year, month, day),
        EndDate = new DateOnly(year, month, day),
        IsDayOff = true
    };

    private static IEnumerable<HolidaySuggestionDto> ReligiousHolidaysOf(int year, DateOnly ramazan, DateOnly kurban)
    {
        foreach (var (key, title, start, days) in new[]
                 {
                     ("ramazan-bayrami", "Ramazan Bayramı", ramazan, RamazanBayramiDays),
                     ("kurban-bayrami", "Kurban Bayramı", kurban, KurbanBayramiDays)
                 })
        {
            yield return new HolidaySuggestionDto
            {
                Key = $"{year}-{key}-arife",
                Title = $"{title} Arifesi",
                EventType = CalendarEventType.ReligiousHoliday,
                StartDate = start.AddDays(-1),
                EndDate = start.AddDays(-1),
                IsDayOff = false,
                Description = HalfDayNote
            };
            yield return new HolidaySuggestionDto
            {
                Key = $"{year}-{key}",
                Title = title,
                EventType = CalendarEventType.ReligiousHoliday,
                StartDate = start,
                EndDate = start.AddDays(days - 1),
                IsDayOff = true
            };
        }
    }
}
