using FluentValidation;
using HafizTakip.Core.Enums;
using HafizTakip.Entities.Dtos;

namespace HafizTakip.Business.ValidationRules.FluentValidation;

public class CreateLessonValidator : AbstractValidator<CreateLessonDto>
{
    public CreateLessonValidator()
    {
        RuleFor(l => l.StudentId).NotEmpty().WithMessage("Öğrenci seçimi zorunludur.");
        RuleFor(l => l.LessonDate).NotEmpty().WithMessage("Ders tarihi boş olamaz.").MustNotBeInFuture();
        RuleFor(l => l.PageNo).NotNull().WithMessage("Sayfa numarası zorunludur.").MustBeValidPage();
        RuleFor(l => l.CuzNo).MustBeValidJuz().When(l => l.CuzNo.HasValue);
        RuleFor(l => l.Performance).MustBeOneOf(PerformanceLevel.All, "performans değerlendirmesi");
        RuleFor(l => l.HomeworkStatus).MustBeOneOf(HomeworkStatus.All, "ödev durumu");
        RuleFor(l => l.TeacherNotes).MaximumLength(2000);
    }
}
