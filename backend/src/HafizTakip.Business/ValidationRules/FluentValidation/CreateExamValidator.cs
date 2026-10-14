using FluentValidation;
using HafizTakip.Core.Enums;
using HafizTakip.Entities.Dtos;

namespace HafizTakip.Business.ValidationRules.FluentValidation;

public class CreateExamValidator : AbstractValidator<CreateExamDto>
{
    public CreateExamValidator()
    {
        RuleFor(e => e.GroupId).NotEmpty().WithMessage("Sınıf seçimi zorunludur.");
        RuleFor(e => e.ExamType).MustBeOneOf(ExamType.All, "sınav türü");
        RuleFor(e => e.ExamDate).NotEmpty().WithMessage("Sınav tarihi zorunludur.");
        RuleFor(e => e.Title).NotEmpty().WithMessage("Sınav başlığı zorunludur.").MaximumLength(200);
        RuleFor(e => e.Description).MaximumLength(2000);
    }
}
