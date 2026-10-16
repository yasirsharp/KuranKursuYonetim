using FluentValidation;
using HafizTakip.Entities.Dtos;

namespace HafizTakip.Business.ValidationRules.FluentValidation;

public class CourseApplicationInfoValidator : AbstractValidator<CourseApplicationInfoDto>
{
    public CourseApplicationInfoValidator()
    {
        RuleFor(x => x.CourseName).NotEmpty().WithMessage("Kurs adı boş olamaz.").MinimumLength(3).MaximumLength(255);
        RuleFor(x => x.City).NotEmpty().WithMessage("İl boş olamaz.").MaximumLength(100);
        RuleFor(x => x.District).NotEmpty().WithMessage("İlçe boş olamaz.").MaximumLength(100);
        RuleFor(x => x.Address).MaximumLength(500);
        RuleFor(x => x.ContactEmail).MaximumLength(255)
            .EmailAddress().WithMessage("Geçerli bir e-posta adresi giriniz.")
            .When(x => !string.IsNullOrWhiteSpace(x.ContactEmail));
    }
}
