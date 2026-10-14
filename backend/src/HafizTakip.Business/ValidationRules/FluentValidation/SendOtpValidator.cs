using FluentValidation;
using HafizTakip.Core.Utilities.Helpers;
using HafizTakip.Entities.Dtos;

namespace HafizTakip.Business.ValidationRules.FluentValidation;

public class SendOtpValidator : AbstractValidator<SendOtpDto>
{
    public SendOtpValidator()
    {
        RuleFor(x => x.PhoneNumber)
            .Must(PhoneNumberHelper.IsValidMobile).WithMessage("Geçerli bir cep telefonu numarası giriniz (5XX XXX XX XX).");
    }
}