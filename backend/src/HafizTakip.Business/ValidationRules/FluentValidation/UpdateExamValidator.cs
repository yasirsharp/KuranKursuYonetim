using FluentValidation;
using HafizTakip.Core.Enums;
using HafizTakip.Entities.Dtos;

namespace HafizTakip.Business.ValidationRules.FluentValidation;

public class UpdateExamValidator : AbstractValidator<UpdateExamDto>
{
    public UpdateExamValidator()
    {
        RuleFor(e => e.Id).NotEmpty().WithMessage("Sınav seçilmelidir.");
        RuleFor(e => e.ExamType).MustBeOneOf(ExamType.All, "sınav türü");
        RuleFor(e => e.ExamDate).NotEmpty().WithMessage("Sınav tarihi zorunludur.");
        RuleFor(e => e.Title).NotEmpty().WithMessage("Sınav başlığı zorunludur.").MaximumLength(200);
        RuleFor(e => e.Description).MaximumLength(2000);
    }
}
