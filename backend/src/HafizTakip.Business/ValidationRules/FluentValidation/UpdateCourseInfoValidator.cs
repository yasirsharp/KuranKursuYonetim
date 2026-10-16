using FluentValidation;
using HafizTakip.Entities.Dtos;

namespace HafizTakip.Business.ValidationRules.FluentValidation;

public class UpdateCourseInfoValidator : AbstractValidator<UpdateCourseInfoDto>
{
    public UpdateCourseInfoValidator()
    {
        RuleFor(c => c.Name).NotEmpty().WithMessage("Kurs adı boş olamaz.").MinimumLength(3).MaximumLength(255);
        RuleFor(c => c.ContactEmail).EmailAddress().WithMessage("Geçerli bir e-posta adresi giriniz.")
            .When(c => !string.IsNullOrWhiteSpace(c.ContactEmail));
        RuleFor(c => c.ContactPhone).MaximumLength(50);
    }
}
