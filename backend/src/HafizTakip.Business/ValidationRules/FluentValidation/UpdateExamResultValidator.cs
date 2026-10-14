using FluentValidation;
using HafizTakip.Entities.Dtos;

namespace HafizTakip.Business.ValidationRules.FluentValidation;

public class UpdateExamResultValidator : AbstractValidator<UpdateExamResultDto>
{
    public UpdateExamResultValidator()
    {
        RuleFor(r => r.Id).NotEmpty().WithMessage("Sınav sonucu seçilmelidir.");
        RuleFor(r => r.Score).InclusiveBetween(0, 100).When(r => r.Score.HasValue).WithMessage("Puan 0-100 arasında olmalıdır.");
        RuleFor(r => r.Grade).MaximumLength(50);
        RuleFor(r => r.TeacherNotes).MaximumLength(2000);
    }
}
