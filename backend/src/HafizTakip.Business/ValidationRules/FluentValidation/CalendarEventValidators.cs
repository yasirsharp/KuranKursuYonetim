using FluentValidation;
using HafizTakip.Core.Enums;
using HafizTakip.Entities.Dtos;

namespace HafizTakip.Business.ValidationRules.FluentValidation;

public class CreateCalendarEventValidator : AbstractValidator<CreateCalendarEventDto>
{
    public CreateCalendarEventValidator()
    {
        RuleFor(e => e.Title).NotEmpty().WithMessage("Başlık zorunludur.").MaximumLength(200);
        RuleFor(e => e.EventType).MustBeOneOf(CalendarEventType.All, "takvim kaydı türü");
        RuleFor(e => e.StartDate).NotEmpty().WithMessage("Başlangıç tarihi zorunludur.");
        RuleFor(e => e.EndDate)
            .GreaterThanOrEqualTo(e => e.StartDate).WithMessage("Bitiş tarihi başlangıç tarihinden önce olamaz.")
            .Must((e, end) => end.DayNumber - e.StartDate.DayNumber < CalendarRules.MaxEventDays)
            .WithMessage($"Bir takvim kaydı en fazla {CalendarRules.MaxEventDays} gün sürebilir.");
        RuleFor(e => e.Description).MaximumLength(1000);
    }
}

public class UpdateCalendarEventValidator : AbstractValidator<UpdateCalendarEventDto>
{
    public UpdateCalendarEventValidator()
    {
        RuleFor(e => e.Id).NotEmpty();
        RuleFor(e => e.Title).NotEmpty().WithMessage("Başlık zorunludur.").MaximumLength(200);
        RuleFor(e => e.EventType).MustBeOneOf(CalendarEventType.All, "takvim kaydı türü");
        RuleFor(e => e.StartDate).NotEmpty().WithMessage("Başlangıç tarihi zorunludur.");
        RuleFor(e => e.EndDate)
            .GreaterThanOrEqualTo(e => e.StartDate).WithMessage("Bitiş tarihi başlangıç tarihinden önce olamaz.")
            .Must((e, end) => end.DayNumber - e.StartDate.DayNumber < CalendarRules.MaxEventDays)
            .WithMessage($"Bir takvim kaydı en fazla {CalendarRules.MaxEventDays} gün sürebilir.");
        RuleFor(e => e.Description).MaximumLength(1000);
    }
}

public class ImportHolidaysValidator : AbstractValidator<ImportHolidaysDto>
{
    public ImportHolidaysValidator()
    {
        RuleFor(i => i.To)
            .GreaterThanOrEqualTo(i => i.From).WithMessage("Bitiş tarihi başlangıç tarihinden önce olamaz.")
            .Must((i, to) => to.DayNumber - i.From.DayNumber < CalendarRules.MaxRangeDays)
            .WithMessage($"Tarih aralığı en fazla {CalendarRules.MaxRangeDays} gün olabilir.");
        RuleForEach(i => i.Keys).NotEmpty().MaximumLength(60);
    }
}

public static class CalendarRules
{
    /// <summary>Tek bir takvim kaydının (ör. yarıyıl tatili) en uzun süresi.</summary>
    public const int MaxEventDays = 120;

    /// <summary>Tatil önerisi, ders günü hesabı vb. sorgularda en geniş tarih aralığı (18 aylık bir dönemi kapsar).</summary>
    public const int MaxRangeDays = 560;
}
