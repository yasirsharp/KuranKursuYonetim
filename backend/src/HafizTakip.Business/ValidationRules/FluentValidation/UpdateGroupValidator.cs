using FluentValidation;
using HafizTakip.Entities.Dtos;

namespace HafizTakip.Business.ValidationRules.FluentValidation;

public class UpdateGroupValidator : AbstractValidator<UpdateGroupDto>
{
    public UpdateGroupValidator()
    {
        RuleFor(g => g.Id).NotEmpty().WithMessage("Sınıf seçilmelidir.");
        RuleFor(g => g.Name).NotEmpty().WithMessage("Sınıf/Grup adı boş olamaz.").MaximumLength(100);
        RuleFor(g => g.TeacherId).NotEmpty().WithMessage("Sınıfın öğretmeni seçilmelidir.");
        RuleFor(g => g.Description).MaximumLength(1000);
    }
}
