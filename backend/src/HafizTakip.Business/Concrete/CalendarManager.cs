using HafizTakip.Business.Abstract;
using HafizTakip.Business.Calendar;
using HafizTakip.Business.ValidationRules.FluentValidation;
using HafizTakip.Core.Aspects.Autofac.Validation;
using HafizTakip.Core.DataAccess;
using HafizTakip.Core.Utilities.IoC;
using HafizTakip.Core.Utilities.Results;
using HafizTakip.DataAccess.Abstract;
using HafizTakip.Entities.Concrete;
using HafizTakip.Entities.Dtos;

namespace HafizTakip.Business.Concrete;

public class CalendarManager : ICalendarService
{
    /// <summary>Devam eden dönemin bitmesine bu kadar gün kala yeni dönem planlaması hatırlatılır.</summary>
    public const int PlanNextTermDaysBefore = 60;

    private const string DefaultTimeZone = "Europe/Istanbul";
    private static readonly int[] DefaultWorkingDays = { 1, 2, 3, 4, 5, 6 };

    private readonly IAcademicTermDal _termDal;
    private readonly ICalendarEventDal _eventDal;
    private readonly ITenantSettingDal _settingDal;
    private readonly ITenantContext _tenantContext;
    private readonly IUnitOfWork _unitOfWork;
    private readonly TimeProvider _timeProvider;

    public CalendarManager(
        IAcademicTermDal termDal,
        ICalendarEventDal eventDal,
        ITenantSettingDal settingDal,
        ITenantContext tenantContext,
        IUnitOfWork unitOfWork,
        TimeProvider timeProvider)
    {
        _termDal = termDal;
        _eventDal = eventDal;
        _settingDal = settingDal;
        _tenantContext = tenantContext;
        _unitOfWork = unitOfWork;
        _timeProvider = timeProvider;
    }

    // ── Dönemler ──────────────────────────────────────────────────

    public async Task<IDataResult<List<AcademicTermDto>>> GetTermsAsync()
    {
        var today = await TodayAsync();
        var terms = await _termDal.GetListAsync();
        var dtos = terms.OrderByDescending(t => t.StartDate).Select(t => ToDto(t, today)).ToList();
        return new SuccessDataResult<List<AcademicTermDto>>(dtos, "Dönemler listelendi.");
    }

    public async Task<IDataResult<TermPlanningDto>> GetTermPlanningAsync()
    {
        var today = await TodayAsync();
        var terms = (await _termDal.GetListAsync()).OrderBy(t => t.StartDate).ToList();

        var current = terms.FirstOrDefault(t => t.StartDate <= today && today <= t.EndDate);
        var next = terms.FirstOrDefault(t => t.StartDate > today);

        var planning = new TermPlanningDto
        {
            CurrentTerm = current == null ? null : ToDto(current, today),
            NextTerm = next == null ? null : ToDto(next, today),
            DaysUntilCurrentTermEnds = current == null ? null : current.EndDate.DayNumber - today.DayNumber
        };

        if (next == null)
        {
            // Dönem yoksa veya devam eden dönemin sonu yaklaştıysa planlama hatırlatılır
            planning.ShouldPlanNextTerm = current == null || planning.DaysUntilCurrentTermEnds <= PlanNextTermDaysBefore;

            var latest = current ?? terms.LastOrDefault();
            if (planning.ShouldPlanNextTerm && latest != null)
                planning.SuggestedNextTerm = SuggestFollowingTerm(latest, today);
        }

        return new SuccessDataResult<TermPlanningDto>(planning);
    }

    /// <summary>
    /// Önceki dönemin 52 hafta (364 gün) sonrası: başlangıç/bitiş aynı haftanın aynı gününe denk gelir
    /// (ör. 14.09.2026 Pzt–28.08.2027 Cmt → 13.09.2027 Pzt–26.08.2028 Cmt). Önceki dönem çok eskiyse
    /// bugünden sonra başlayan ilk yıla kaydırılır.
    /// </summary>
    public static CreateAcademicTermDto SuggestFollowingTerm(AcademicTerm previous, DateOnly today)
    {
        var start = previous.StartDate.AddDays(364);
        var end = previous.EndDate.AddDays(364);
        while (end < today)
        {
            start = start.AddDays(364);
            end = end.AddDays(364);
        }

        return new CreateAcademicTermDto
        {
            Name = $"{start.Year}-{end.Year} Eğitim Dönemi",
            StartDate = start,
            EndDate = end
        };
    }

    [ValidationAspect(typeof(CreateAcademicTermValidator))]
    public async Task<IDataResult<AcademicTermDto>> AddTermAsync(CreateAcademicTermDto dto)
    {
        return await _unitOfWork.ExecuteInTransactionAsync<IDataResult<AcademicTermDto>>(async () =>
        {
            var overlapError = await FindOverlapAsync(dto.StartDate, dto.EndDate, excludeId: null);
            if (overlapError != null) return new ErrorDataResult<AcademicTermDto>(overlapError, ResultErrorType.Conflict);

            var term = new AcademicTerm
            {
                TenantId = _tenantContext.TenantId,
                Name = dto.Name.Trim(),
                StartDate = dto.StartDate,
                EndDate = dto.EndDate,
                Notes = dto.Notes
            };
            _termDal.Add(term);
            // Model bu ilişkiyi bilmediğinden EF ekleme/güncelleme sırasını garanti etmez; dönem önce yazılır (FK)
            await _unitOfWork.SaveChangesAsync();

            // Dönem açılmadan önce eklenmiş, bu döneme düşen takvim kayıtları döneme bağlanır
            var orphanEvents = await _eventDal.GetListAsync(e =>
                e.TermId == null && e.StartDate >= term.StartDate && e.StartDate <= term.EndDate);
            foreach (var calendarEvent in orphanEvents)
            {
                calendarEvent.TermId = term.Id;
                _eventDal.Update(calendarEvent);
            }

            return new SuccessDataResult<AcademicTermDto>(ToDto(term, await TodayAsync()), "Dönem oluşturuldu.");
        });
    }

    [ValidationAspect(typeof(UpdateAcademicTermValidator))]
    public async Task<IResult> UpdateTermAsync(UpdateAcademicTermDto dto)
    {
        return await _unitOfWork.ExecuteInTransactionAsync<IResult>(async () =>
        {
            var term = await _termDal.GetAsync(t => t.Id == dto.Id);
            if (term == null) return new ErrorResult("Dönem bulunamadı.", ResultErrorType.NotFound);

            var overlapError = await FindOverlapAsync(dto.StartDate, dto.EndDate, excludeId: term.Id);
            if (overlapError != null) return new ErrorResult(overlapError, ResultErrorType.Conflict);

            term.Name = dto.Name.Trim();
            term.StartDate = dto.StartDate;
            term.EndDate = dto.EndDate;
            term.Notes = dto.Notes;
            _termDal.Update(term);

            // Tarihler değiştiyse takvim kayıtlarının dönem bağlantısı yeni aralığa göre düzeltilir
            var affected = await _eventDal.GetListAsync(e =>
                e.TermId == term.Id || (e.TermId == null && e.StartDate >= term.StartDate && e.StartDate <= term.EndDate));
            foreach (var calendarEvent in affected)
            {
                Guid? termId = calendarEvent.StartDate >= term.StartDate && calendarEvent.StartDate <= term.EndDate ? term.Id : null;
                if (calendarEvent.TermId == termId) continue;
                calendarEvent.TermId = termId;
                _eventDal.Update(calendarEvent);
            }

            return new SuccessResult("Dönem güncellendi.");
        });
    }

    public async Task<IResult> DeleteTermAsync(Guid id)
    {
        return await _unitOfWork.ExecuteInTransactionAsync<IResult>(async () =>
        {
            var term = await _termDal.GetAsync(t => t.Id == id);
            if (term == null) return new ErrorResult("Dönem bulunamadı.", ResultErrorType.NotFound);

            // Tatiller kurs takviminde kalır; yalnızca dönem bağlantısı kaldırılır
            foreach (var calendarEvent in await _eventDal.GetListAsync(e => e.TermId == id))
            {
                calendarEvent.TermId = null;
                _eventDal.Update(calendarEvent);
            }

            _termDal.Remove(term);
            return new SuccessResult("Dönem silindi.");
        });
    }

    private async Task<string?> FindOverlapAsync(DateOnly start, DateOnly end, Guid? excludeId)
    {
        var overlapping = await _termDal.GetAsync(t =>
            t.Id != excludeId && t.StartDate <= end && t.EndDate >= start);
        return overlapping == null
            ? null
            : $"Bu tarihler \"{overlapping.Name}\" dönemiyle ({overlapping.StartDate:dd.MM.yyyy}–{overlapping.EndDate:dd.MM.yyyy}) çakışıyor.";
    }

    // ── Takvim kayıtları ──────────────────────────────────────────

    public async Task<IDataResult<List<CalendarEventDto>>> GetEventsAsync(DateOnly from, DateOnly to)
    {
        var rangeError = ValidateRange(from, to);
        if (rangeError != null) return new ErrorDataResult<List<CalendarEventDto>>(rangeError);

        var events = await _eventDal.GetListAsync(e => e.StartDate <= to && e.EndDate >= from);
        var dtos = events.OrderBy(e => e.StartDate).ThenBy(e => e.Title).Select(ToDto).ToList();
        return new SuccessDataResult<List<CalendarEventDto>>(dtos, "Takvim kayıtları listelendi.");
    }

    [ValidationAspect(typeof(CreateCalendarEventValidator))]
    public async Task<IDataResult<CalendarEventDto>> AddEventAsync(CreateCalendarEventDto dto)
    {
        var calendarEvent = new CalendarEvent
        {
            TenantId = _tenantContext.TenantId,
            Title = dto.Title.Trim(),
            EventType = dto.EventType,
            StartDate = dto.StartDate,
            EndDate = dto.EndDate,
            IsDayOff = dto.IsDayOff,
            Description = dto.Description,
            TermId = await FindTermIdAsync(dto.StartDate)
        };

        await _eventDal.AddAsync(calendarEvent);
        return new SuccessDataResult<CalendarEventDto>(ToDto(calendarEvent), "Takvim kaydı eklendi.");
    }

    [ValidationAspect(typeof(UpdateCalendarEventValidator))]
    public async Task<IResult> UpdateEventAsync(UpdateCalendarEventDto dto)
    {
        var calendarEvent = await _eventDal.GetAsync(e => e.Id == dto.Id);
        if (calendarEvent == null) return new ErrorResult("Takvim kaydı bulunamadı.", ResultErrorType.NotFound);

        calendarEvent.Title = dto.Title.Trim();
        calendarEvent.EventType = dto.EventType;
        calendarEvent.StartDate = dto.StartDate;
        calendarEvent.EndDate = dto.EndDate;
        calendarEvent.IsDayOff = dto.IsDayOff;
        calendarEvent.Description = dto.Description;
        calendarEvent.TermId = await FindTermIdAsync(dto.StartDate);

        await _eventDal.UpdateAsync(calendarEvent);
        return new SuccessResult("Takvim kaydı güncellendi.");
    }

    public async Task<IResult> DeleteEventAsync(Guid id)
    {
        var calendarEvent = await _eventDal.GetAsync(e => e.Id == id);
        if (calendarEvent == null) return new ErrorResult("Takvim kaydı bulunamadı.", ResultErrorType.NotFound);

        await _eventDal.DeleteAsync(calendarEvent);
        return new SuccessResult("Takvim kaydı silindi.");
    }

    private async Task<Guid?> FindTermIdAsync(DateOnly date)
    {
        var term = await _termDal.GetAsync(t => t.StartDate <= date && t.EndDate >= date);
        return term?.Id;
    }

    // ── Tatiller ──────────────────────────────────────────────────

    public async Task<IDataResult<HolidaySuggestionListDto>> GetHolidaySuggestionsAsync(DateOnly from, DateOnly to)
    {
        var rangeError = ValidateRange(from, to);
        if (rangeError != null) return new ErrorDataResult<HolidaySuggestionListDto>(rangeError);

        var (holidays, missingYears) = TurkishHolidayCalendar.GetHolidays(from, to);
        var existingKeys = await GetExistingHolidayKeysAsync(holidays.Select(h => h.Key).ToList());
        foreach (var holiday in holidays)
            holiday.AlreadyAdded = existingKeys.Contains(holiday.Key);

        var message = missingYears.Count == 0
            ? "Resmi ve dini tatiller getirildi."
            : $"{string.Join(", ", missingYears)} yılı dini bayram tarihleri henüz listede yok; bu bayramları takvime elle ekleyin.";

        return new SuccessDataResult<HolidaySuggestionListDto>(new HolidaySuggestionListDto
        {
            From = from,
            To = to,
            Holidays = holidays,
            MissingReligiousHolidayYears = missingYears
        }, message);
    }

    [ValidationAspect(typeof(ImportHolidaysValidator))]
    public async Task<IDataResult<ImportHolidaysResultDto>> ImportHolidaysAsync(ImportHolidaysDto dto)
    {
        var (holidays, _) = TurkishHolidayCalendar.GetHolidays(dto.From, dto.To);
        if (dto.Keys.Count > 0)
        {
            var unknownKey = dto.Keys.FirstOrDefault(k => holidays.All(h => h.Key != k));
            if (unknownKey != null)
                return new ErrorDataResult<ImportHolidaysResultDto>($"\"{unknownKey}\" bu tarih aralığındaki tatiller arasında yok.");

            holidays = holidays.Where(h => dto.Keys.Contains(h.Key)).ToList();
        }

        return await _unitOfWork.ExecuteInTransactionAsync<IDataResult<ImportHolidaysResultDto>>(async () =>
        {
            var existingKeys = await GetExistingHolidayKeysAsync(holidays.Select(h => h.Key).ToList());
            var terms = await _termDal.GetListAsync(t => t.StartDate <= dto.To && t.EndDate >= dto.From);

            var added = 0;
            foreach (var holiday in holidays.Where(h => !existingKeys.Contains(h.Key)))
            {
                _eventDal.Add(new CalendarEvent
                {
                    TenantId = _tenantContext.TenantId,
                    Title = holiday.Title,
                    EventType = holiday.EventType,
                    StartDate = holiday.StartDate,
                    EndDate = holiday.EndDate,
                    IsDayOff = holiday.IsDayOff,
                    Description = holiday.Description,
                    HolidayKey = holiday.Key,
                    TermId = terms.FirstOrDefault(t => t.StartDate <= holiday.StartDate && holiday.StartDate <= t.EndDate)?.Id
                });
                added++;
            }

            var result = new ImportHolidaysResultDto { Added = added, Skipped = holidays.Count - added };
            return new SuccessDataResult<ImportHolidaysResultDto>(result, $"{added} tatil takvime eklendi.");
        });
    }

    private async Task<HashSet<string>> GetExistingHolidayKeysAsync(List<string> keys)
    {
        if (keys.Count == 0) return new HashSet<string>();
        var existing = await _eventDal.GetListAsync(e => e.HolidayKey != null && keys.Contains(e.HolidayKey));
        return existing.Select(e => e.HolidayKey!).ToHashSet();
    }

    // ── Ders günleri ──────────────────────────────────────────────

    public async Task<IDataResult<SchoolDaysDto>> GetSchoolDaysAsync(DateOnly? from, DateOnly? to)
    {
        var settings = await _settingDal.GetAsync(s => s.TenantId == _tenantContext.TenantId);
        var terms = await _termDal.GetListAsync();

        if (from == null || to == null)
        {
            if (from != null || to != null)
                return new ErrorDataResult<SchoolDaysDto>("Başlangıç ve bitiş tarihi birlikte verilmelidir.");

            var today = Today(settings?.TimeZone);
            var current = terms.FirstOrDefault(t => t.StartDate <= today && today <= t.EndDate);
            if (current == null)
                return new ErrorDataResult<SchoolDaysDto>("Devam eden bir dönem yok; tarih aralığı belirtin.");
            (from, to) = (current.StartDate, current.EndDate);
        }

        var rangeError = ValidateRange(from.Value, to.Value);
        if (rangeError != null) return new ErrorDataResult<SchoolDaysDto>(rangeError);

        var events = await _eventDal.GetListAsync(e => e.IsDayOff && e.StartDate <= to && e.EndDate >= from);
        var result = CalculateSchoolDays(from.Value, to.Value, settings?.WorkingDays ?? DefaultWorkingDays, terms, events);
        return new SuccessDataResult<SchoolDaysDto>(result, $"{result.SchoolDayCount} ders günü hesaplandı.");
    }

    /// <summary>
    /// Ders günü hesabı (veritabanından bağımsız). Bir gün ders günü değilse ilk geçerli gerekçe yazılır:
    /// dönem dışı → çalışma günü değil → tatil/kapalı gün kaydı.
    /// Kursun hiç dönemi yoksa dönem şartı aranmaz.
    /// </summary>
    public static SchoolDaysDto CalculateSchoolDays(
        DateOnly from, DateOnly to, IReadOnlyCollection<int> workingDays,
        IReadOnlyCollection<AcademicTerm> terms, IReadOnlyCollection<CalendarEvent> dayOffEvents)
    {
        var result = new SchoolDaysDto { From = from, To = to };

        for (var date = from; date <= to; date = date.AddDays(1))
        {
            var day = date;
            OffDayDto? offDay = null;

            if (terms.Count > 0 && !terms.Any(t => t.StartDate <= day && day <= t.EndDate))
            {
                offDay = new OffDayDto { Date = day, Reason = "outside_term" };
            }
            else if (!workingDays.Contains(IsoDayOfWeek(day)))
            {
                offDay = new OffDayDto { Date = day, Reason = "not_working_day" };
            }
            else
            {
                var holiday = dayOffEvents.FirstOrDefault(e => e.IsDayOff && e.StartDate <= day && day <= e.EndDate);
                if (holiday != null)
                    offDay = new OffDayDto { Date = day, Reason = holiday.EventType, Title = holiday.Title };
            }

            if (offDay == null) result.SchoolDays.Add(day);
            else result.OffDays.Add(offDay);
        }

        result.SchoolDayCount = result.SchoolDays.Count;
        return result;
    }

    /// <summary>1 = Pazartesi … 7 = Pazar.</summary>
    private static int IsoDayOfWeek(DateOnly date) => date.DayOfWeek == DayOfWeek.Sunday ? 7 : (int)date.DayOfWeek;

    // ── Yardımcılar ───────────────────────────────────────────────

    private static string? ValidateRange(DateOnly from, DateOnly to)
    {
        if (to < from) return "Bitiş tarihi başlangıç tarihinden önce olamaz.";
        if (to.DayNumber - from.DayNumber >= CalendarRules.MaxRangeDays)
            return $"Tarih aralığı en fazla {CalendarRules.MaxRangeDays} gün olabilir.";
        return null;
    }

    /// <summary>Kursun saat dilimine göre bugünün tarihi.</summary>
    private async Task<DateOnly> TodayAsync()
    {
        var settings = await _settingDal.GetAsync(s => s.TenantId == _tenantContext.TenantId);
        return Today(settings?.TimeZone);
    }

    private DateOnly Today(string? timeZone)
    {
        if (!TimeZoneInfo.TryFindSystemTimeZoneById(timeZone ?? DefaultTimeZone, out var zone))
            zone = TimeZoneInfo.TryFindSystemTimeZoneById(DefaultTimeZone, out var fallback) ? fallback : TimeZoneInfo.Utc;
        return DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(_timeProvider.GetUtcNow(), zone).DateTime);
    }

    private static AcademicTermDto ToDto(AcademicTerm term, DateOnly today) => new()
    {
        Id = term.Id,
        Name = term.Name,
        StartDate = term.StartDate,
        EndDate = term.EndDate,
        Notes = term.Notes,
        State = today < term.StartDate ? "upcoming" : today > term.EndDate ? "past" : "current"
    };

    private static CalendarEventDto ToDto(CalendarEvent e) => new()
    {
        Id = e.Id,
        TermId = e.TermId,
        Title = e.Title,
        EventType = e.EventType,
        StartDate = e.StartDate,
        EndDate = e.EndDate,
        IsDayOff = e.IsDayOff,
        Description = e.Description,
        HolidayKey = e.HolidayKey
    };
}
