using FluentValidation;
using HafizTakip.Entities.Dtos;

namespace HafizTakip.Business.ValidationRules.FluentValidation;

public class CreateExamResultValidator : AbstractValidator<CreateExamResultDto>
{
    public CreateExamResultValidator()
    {
        RuleFor(r => r.ExamId).NotEmpty().WithMessage("Sınav seçimi zorunludur.");
        RuleFor(r => r.StudentId).NotEmpty().WithMessage("Öğrenci seçimi zorunludur.");
        RuleFor(r => r.Score).InclusiveBetween(0, 100).When(r => r.Score.HasValue).WithMessage("Puan 0-100 arasında olmalıdır.");
        RuleFor(r => r.Grade).MaximumLength(50);
        RuleFor(r => r.TeacherNotes).MaximumLength(2000);
    }
}
