using FluentValidation;
using HafizTakip.Core.Utilities.Helpers;
using HafizTakip.Entities.Dtos;

namespace HafizTakip.Business.ValidationRules.FluentValidation;

public class SendEmailCodeValidator : AbstractValidator<SendEmailCodeDto>
{
    public SendEmailCodeValidator()
    {
        RuleFor(x => x.Email).Must(EmailHelper.IsValid).WithMessage("Geçerli bir e-posta adresi giriniz.");
    }
}

public class VerifyEmailCodeValidator : AbstractValidator<VerifyEmailCodeDto>
{
    public VerifyEmailCodeValidator()
    {
        RuleFor(x => x.Email).Must(EmailHelper.IsValid).WithMessage("Geçerli bir e-posta adresi giriniz.");
        RuleFor(x => x.Code)
            .NotEmpty().WithMessage("Doğrulama kodu boş olamaz.")
            .Matches("^[0-9]{4,8}$").WithMessage("Doğrulama kodu yalnızca rakamlardan oluşmalıdır.");
    }
}
