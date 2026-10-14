using FluentValidation;
using HafizTakip.Entities.Dtos;

namespace HafizTakip.Business.ValidationRules.FluentValidation;

public class CreateStudentParentValidator : AbstractValidator<CreateStudentParentDto>
{
    public CreateStudentParentValidator()
    {
        RuleFor(sp => sp.StudentId).NotEmpty().WithMessage("Öğrenci seçimi zorunludur.");
        RuleFor(sp => sp.ParentId).NotEmpty().WithMessage("Veli seçimi zorunludur.");
    }
}
