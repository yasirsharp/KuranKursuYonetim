using FluentValidation;
using HafizTakip.Entities.Dtos;

namespace HafizTakip.Business.ValidationRules.FluentValidation;

public class PlatformLoginValidator : AbstractValidator<PlatformLoginDto>
{
    public PlatformLoginValidator()
    {
        RuleFor(x => x.Email).NotEmpty().WithMessage("E-posta boş olamaz.").EmailAddress().MaximumLength(255);
        RuleFor(x => x.Password).NotEmpty().WithMessage("Şifre boş olamaz.").MaximumLength(200);
    }
}
