using FluentValidation;
using HafizTakip.Core.Utilities.Helpers;
using HafizTakip.Core.Utilities.Security;
using HafizTakip.Entities.Dtos;

namespace HafizTakip.Business.ValidationRules.FluentValidation;

public class CreateUserValidator : AbstractValidator<CreateUserDto>
{
    public CreateUserValidator()
    {
        RuleFor(u => u.FirstName).NotEmpty().WithMessage("Ad boş olamaz.").MaximumLength(100);
        RuleFor(u => u.LastName).NotEmpty().WithMessage("Soyad boş olamaz.").MaximumLength(100);
        RuleFor(u => u.PhoneNumber).NotEmpty().WithMessage("Telefon numarası boş olamaz.");
        RuleFor(u => u.PhoneNumber)
            .Must(PhoneNumberHelper.IsValidMobile).WithMessage("Geçerli bir cep telefonu numarası giriniz (5XX XXX XX XX).")
            .When(u => !string.IsNullOrWhiteSpace(u.PhoneNumber));
        RuleFor(u => u.Email).EmailAddress().WithMessage("Geçerli bir e-posta adresi giriniz.")
            .When(u => !string.IsNullOrWhiteSpace(u.Email));
        RuleFor(u => u.Role)
            .Must(RoleHierarchy.IsTenantRole).WithMessage("Geçersiz rol.");
    }
}
