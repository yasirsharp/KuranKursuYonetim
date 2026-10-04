using FluentValidation;
using HafizTakip.Entities.Dtos;

namespace HafizTakip.Business.ValidationRules.FluentValidation;

public class CreateTenantValidator : AbstractValidator<CreateTenantDto>
{
    public CreateTenantValidator()
    {
        RuleFor(t => t.Name).NotEmpty().WithMessage("Kurs adı boş olamaz.");
        RuleFor(t => t.Name).MinimumLength(3).WithMessage("Kurs adı en az 3 karakter olmalıdır.");
        RuleFor(t => t.Slug).NotEmpty().WithMessage("Kısa ad (Slug) boş olamaz.");
    }
}
