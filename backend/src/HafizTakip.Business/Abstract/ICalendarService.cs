using HafizTakip.Core.Utilities.Results;
using HafizTakip.Entities.Dtos;

namespace HafizTakip.Business.Abstract;

/// <summary>Oturumdaki kursun eğitim dönemleri, takvimi (tatil/etkinlik) ve ders günü hesabı.</summary>
public interface ICalendarService
{
    Task<IDataResult<List<AcademicTermDto>>> GetTermsAsync();

    /// <summary>Devam eden/sıradaki dönem ve dönem sonu yaklaşınca yeni dönem önerisi.</summary>
    Task<IDataResult<TermPlanningDto>> GetTermPlanningAsync();

    Task<IDataResult<AcademicTermDto>> AddTermAsync(CreateAcademicTermDto term);
    Task<IResult> UpdateTermAsync(UpdateAcademicTermDto term);

    /// <summary>Dönemi siler; dönemdeki takvim kayıtları silinmez, dönemsiz kalır.</summary>
    Task<IResult> DeleteTermAsync(Guid id);

    /// <summary>Aralıkla kesişen takvim kayıtları.</summary>
    Task<IDataResult<List<CalendarEventDto>>> GetEventsAsync(DateOnly from, DateOnly to);
    Task<IDataResult<CalendarEventDto>> AddEventAsync(CreateCalendarEventDto calendarEvent);
    Task<IResult> UpdateEventAsync(UpdateCalendarEventDto calendarEvent);
    Task<IResult> DeleteEventAsync(Guid id);

    /// <summary>Aralıktaki resmi tatiller ve dini bayramlar; takvimde zaten olanlar işaretlenir.</summary>
    Task<IDataResult<HolidaySuggestionListDto>> GetHolidaySuggestionsAsync(DateOnly from, DateOnly to);

    /// <summary>Seçilen tatilleri takvime ekler (zaten ekli olanlar atlanır).</summary>
    Task<IDataResult<ImportHolidaysResultDto>> ImportHolidaysAsync(ImportHolidaysDto import);

    /// <summary>
    /// Ders günleri: kurs çalışma günü olan, bir dönemin içinde kalan (kursun hiç dönemi yoksa bu şart aranmaz)
    /// ve tatil/kapalı gün kaydına denk gelmeyen günler. Aralık verilmezse devam eden dönem kullanılır.
    /// </summary>
    Task<IDataResult<SchoolDaysDto>> GetSchoolDaysAsync(DateOnly? from, DateOnly? to);
}
