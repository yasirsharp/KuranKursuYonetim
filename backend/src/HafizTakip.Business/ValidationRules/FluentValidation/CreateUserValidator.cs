using FluentValidation;
using HafizTakip.Entities.Dtos;

namespace HafizTakip.Business.ValidationRules.FluentValidation;

public class CreateUserValidator : AbstractValidator<CreateUserDto>
{
    public CreateUserValidator()
    {
        RuleFor(u => u.FirstName).NotEmpty().WithMessage("Ad boş olamaz.");
        RuleFor(u => u.LastName).NotEmpty().WithMessage("Soyad boş olamaz.");
        RuleFor(u => u.PhoneNumber).NotEmpty().WithMessage("Telefon numarası boş olamaz.");
        RuleFor(u => u.PhoneNumber).MinimumLength(10).WithMessage("Geçerli bir telefon numarası giriniz.");
    }
}
