using HafizTakip.Core.Entities;

namespace HafizTakip.Entities.Dtos;

/// <summary>Eğitim dönemi. State: "current" (devam ediyor) | "upcoming" (başlamadı) | "past" (bitti).</summary>
public class AcademicTermDto : IDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public string? Notes { get; set; }
    public string State { get; set; } = string.Empty;
}

public class CreateAcademicTermDto : IDto
{
    public string Name { get; set; } = string.Empty;
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public string? Notes { get; set; }
}

public class UpdateAcademicTermDto : IDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public string? Notes { get; set; }
}

/// <summary>
/// Dönem planlama özeti: devam eden ve sıradaki dönem. Devam eden dönemin bitmesine
/// <see cref="DaysUntilCurrentTermEnds"/> gün kaldıysa ve sıradaki dönem açılmamışsa
/// ShouldPlanNextTerm true olur ve bir öneri döner.
/// </summary>
public class TermPlanningDto : IDto
{
    public AcademicTermDto? CurrentTerm { get; set; }
    public AcademicTermDto? NextTerm { get; set; }
    public int? DaysUntilCurrentTermEnds { get; set; }
    public bool ShouldPlanNextTerm { get; set; }
    public CreateAcademicTermDto? SuggestedNextTerm { get; set; }
}

public class CalendarEventDto : IDto
{
    public Guid Id { get; set; }
    public Guid? TermId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string EventType { get; set; } = string.Empty;
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public bool IsDayOff { get; set; }
    public string? Description { get; set; }
    public string? HolidayKey { get; set; }
}

public class CreateCalendarEventDto : IDto
{
    public string Title { get; set; } = string.Empty;
    public string EventType { get; set; } = string.Empty;
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public bool IsDayOff { get; set; } = true;
    public string? Description { get; set; }
}

public class UpdateCalendarEventDto : IDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string EventType { get; set; } = string.Empty;
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public bool IsDayOff { get; set; }
    public string? Description { get; set; }
}

/// <summary>Hazır listeden önerilen tatil (resmi tatil veya dini bayram).</summary>
public class HolidaySuggestionDto : IDto
{
    public string Key { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string EventType { get; set; } = string.Empty;
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public bool IsDayOff { get; set; }
    public string? Description { get; set; }

    /// <summary>Kurs takviminde zaten var mı?</summary>
    public bool AlreadyAdded { get; set; }
}

public class HolidaySuggestionListDto : IDto
{
    public DateOnly From { get; set; }
    public DateOnly To { get; set; }
    public List<HolidaySuggestionDto> Holidays { get; set; } = new();

    /// <summary>Dini bayram tarihleri tabloda bulunmayan yıllar; bu yılların bayramları elle eklenmelidir.</summary>
    public List<int> MissingReligiousHolidayYears { get; set; } = new();
}

public class ImportHolidaysDto : IDto
{
    public DateOnly From { get; set; }
    public DateOnly To { get; set; }

    /// <summary>Eklenecek tatillerin anahtarları; boşsa aralıktaki tüm tatiller eklenir.</summary>
    public List<string> Keys { get; set; } = new();
}

public class ImportHolidaysResultDto : IDto
{
    public int Added { get; set; }
    public int Skipped { get; set; }
}

public class OffDayDto : IDto
{
    public DateOnly Date { get; set; }

    /// <summary>"not_working_day" | "outside_term" | takvim kaydının türü.</summary>
    public string Reason { get; set; } = string.Empty;
    public string? Title { get; set; }
}

/// <summary>Bir tarih aralığındaki ders günleri ve ders yapılmayan günler (gerekçesiyle).</summary>
public class SchoolDaysDto : IDto
{
    public DateOnly From { get; set; }
    public DateOnly To { get; set; }
    public int SchoolDayCount { get; set; }
    public List<DateOnly> SchoolDays { get; set; } = new();
    public List<OffDayDto> OffDays { get; set; } = new();
}
