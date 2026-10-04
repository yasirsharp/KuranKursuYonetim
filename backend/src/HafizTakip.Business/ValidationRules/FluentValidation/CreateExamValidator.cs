using FluentValidation;
using HafizTakip.Entities.Dtos;

namespace HafizTakip.Business.ValidationRules.FluentValidation;

public class CreateExamValidator : AbstractValidator<CreateExamDto>
{
    public CreateExamValidator()
    {
        RuleFor(e => e.GroupId).NotEmpty().WithMessage("Sınıf seçimi zorunludur.");
        RuleFor(e => e.ExamType).NotEmpty().WithMessage("Sınav türü zorunludur.");
        RuleFor(e => e.ExamType).Must(BeAValidExamType).WithMessage("Geçersiz sınav türü.");
        RuleFor(e => e.ExamDate).NotEmpty().WithMessage("Sınav tarihi zorunludur.");
        RuleFor(e => e.Title).NotEmpty().WithMessage("Sınav başlığı zorunludur.");
    }

    private bool BeAValidExamType(string arg)
    {
        return arg == "hafizlik" || arg == "tecvid" || arg == "okuma";
    }
}
