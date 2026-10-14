using FluentValidation;
using HafizTakip.Core.Enums;
using HafizTakip.Entities.Dtos;

namespace HafizTakip.Business.ValidationRules.FluentValidation;

public class UpdateTenantValidator : AbstractValidator<UpdateTenantDto>
{
    public UpdateTenantValidator()
    {
        RuleFor(t => t.Id).NotEmpty();
        RuleFor(t => t.Name).NotEmpty().WithMessage("Kurs adı boş olamaz.").MinimumLength(3).MaximumLength(255);
        RuleFor(t => t.Status).MustBeOneOf(TenantStatus.All, "kurs durumu");
        RuleFor(t => t.ContactEmail).EmailAddress().When(t => !string.IsNullOrWhiteSpace(t.ContactEmail));
        RuleFor(t => t.ContactPhone).MaximumLength(50);
    }
}
