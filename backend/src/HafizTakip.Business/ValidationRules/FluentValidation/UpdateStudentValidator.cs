using FluentValidation;
using HafizTakip.Core.Enums;
using HafizTakip.Entities.Dtos;

namespace HafizTakip.Business.ValidationRules.FluentValidation;

public class UpdateStudentValidator : AbstractValidator<UpdateStudentDto>
{
    public UpdateStudentValidator()
    {
        RuleFor(s => s.Id).NotEmpty().WithMessage("Öğrenci seçilmelidir.");
        RuleFor(s => s.FirstName).NotEmpty().WithMessage("Öğrenci adı boş olamaz.").MaximumLength(100);
        RuleFor(s => s.LastName).NotEmpty().WithMessage("Öğrenci soyadı boş olamaz.").MaximumLength(100);
        RuleFor(s => s.IdentificationNumber)
            .Matches("^[0-9]{11}$").WithMessage("T.C. kimlik numarası 11 haneli olmalıdır.")
            .When(s => !string.IsNullOrWhiteSpace(s.IdentificationNumber));
        RuleFor(s => s.Status).MustBeOneOf(StudentStatus.All, "öğrenci durumu");
    }
}
