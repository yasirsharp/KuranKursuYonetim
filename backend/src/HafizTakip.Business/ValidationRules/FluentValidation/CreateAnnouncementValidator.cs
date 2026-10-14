using FluentValidation;
using HafizTakip.Entities.Dtos;

namespace HafizTakip.Business.ValidationRules.FluentValidation;

public class CreateAnnouncementValidator : AbstractValidator<CreateAnnouncementDto>
{
    public CreateAnnouncementValidator()
    {
        RuleFor(a => a.Title).NotEmpty().WithMessage("Başlık zorunludur.").MaximumLength(200);
        RuleFor(a => a.Content).NotEmpty().WithMessage("İçerik zorunludur.").MaximumLength(10000);
        RuleFor(a => a.TargetAudience).NotEmpty().WithMessage("Hedef kitle zorunludur.").MaximumLength(100);
    }
}
