using FluentValidation;
using HafizTakip.Entities.Dtos;

namespace HafizTakip.Business.ValidationRules.FluentValidation;

public class CreateStudentValidator : AbstractValidator<CreateStudentDto>
{
    public CreateStudentValidator()
    {
        RuleFor(s => s.FirstName).NotEmpty().WithMessage("Öğrenci adı boş olamaz.");
        RuleFor(s => s.LastName).NotEmpty().WithMessage("Öğrenci soyadı boş olamaz.");
        
        RuleFor(s => s.IdentificationNumber)
            .Length(11).When(s => !string.IsNullOrEmpty(s.IdentificationNumber))
            .WithMessage("T.C. Kimlik numarası 11 haneli olmalıdır.");
    }
}
