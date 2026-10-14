using FluentValidation;
using HafizTakip.Core.Enums;
using HafizTakip.Entities.Dtos;

namespace HafizTakip.Business.ValidationRules.FluentValidation;

public class UpdateLessonValidator : AbstractValidator<UpdateLessonDto>
{
    public UpdateLessonValidator()
    {
        RuleFor(l => l.Id).NotEmpty().WithMessage("Ders seçilmelidir.");
        RuleFor(l => l.PageNo).MustBeValidPage().When(l => l.PageNo.HasValue);
        RuleFor(l => l.CuzNo).MustBeValidJuz().When(l => l.CuzNo.HasValue);
        RuleFor(l => l.Performance).MustBeOneOf(PerformanceLevel.All, "performans değerlendirmesi");
        RuleFor(l => l.HomeworkStatus).MustBeOneOf(HomeworkStatus.All, "ödev durumu");
        RuleFor(l => l.TeacherNotes).MaximumLength(2000);
    }
}
