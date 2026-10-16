using FluentValidation;
using HafizTakip.Core.Utilities.Helpers;
using HafizTakip.Entities.Dtos;

namespace HafizTakip.Business.ValidationRules.FluentValidation;

public class CourseApplicationValidator : AbstractValidator<CourseApplicationDto>
{
    public CourseApplicationValidator()
    {
        Include(new CourseApplicationInfoValidator());

        RuleFor(x => x.PhoneNumber)
            .Must(PhoneNumberHelper.IsValidMobile).WithMessage("Geçerli bir cep telefonu numarası giriniz (5XX XXX XX XX).");
        RuleFor(x => x.Code)
            .NotEmpty().WithMessage("Doğrulama kodu boş olamaz.")
            .Matches("^[0-9]{4,8}$").WithMessage("Doğrulama kodu yalnızca rakamlardan oluşmalıdır.");
        RuleFor(x => x.AdminFirstName).NotEmpty().WithMessage("Ad boş olamaz.").MaximumLength(100);
        RuleFor(x => x.AdminLastName).NotEmpty().WithMessage("Soyad boş olamaz.").MaximumLength(100);
    }
}
