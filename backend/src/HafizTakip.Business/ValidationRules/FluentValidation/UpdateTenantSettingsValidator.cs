using FluentValidation;
using HafizTakip.Core.Enums;
using HafizTakip.Entities.Dtos;

namespace HafizTakip.Business.ValidationRules.FluentValidation;

public class UpdateTenantSettingsValidator : AbstractValidator<TenantSettingsDto>
{
    public UpdateTenantSettingsValidator()
    {
        RuleFor(s => s.WorkingDays)
            .NotEmpty().WithMessage("En az bir çalışma günü seçilmelidir.")
            .Must(days => days.All(d => d is >= 1 and <= 7)).WithMessage("Çalışma günleri 1 (Pazartesi) ile 7 (Pazar) arasında olmalıdır.")
            .Must(days => days.Distinct().Count() == days.Count).WithMessage("Aynı gün birden fazla seçilemez.");

        RuleFor(s => s.DayEndTime)
            .GreaterThan(s => s.DayStartTime).WithMessage("Gün bitiş saati başlangıç saatinden sonra olmalıdır.");

        RuleFor(s => s.DefaultSlotMinutes).InclusiveBetween(5, 240).WithMessage("Ders süresi 5-240 dakika arasında olmalıdır.");
        RuleFor(s => s.TimeZone).NotEmpty().MaximumLength(64)
            .Must(BeKnownTimeZone).WithMessage("Geçersiz saat dilimi.");

        RuleFor(s => s.AbsenceThreshold).InclusiveBetween(1, 365);
        RuleFor(s => s.AbsencePeriodDays).InclusiveBetween(1, 365);

        RuleFor(s => s.ReviewMode).MustBeOneOf(ReviewMode.All, "tekrar yöntemi");
        RuleFor(s => s.DailyReviewTargetPages).InclusiveBetween(1, 604);
    }

    private static bool BeKnownTimeZone(string? timeZone)
    {
        if (string.IsNullOrWhiteSpace(timeZone)) return false;
        return TimeZoneInfo.TryFindSystemTimeZoneById(timeZone, out _);
    }
}
