using FluentValidation;
using HafizTakip.Entities.Dtos;

namespace HafizTakip.Business.ValidationRules.FluentValidation;

public class CreateAnnouncementValidator : AbstractValidator<CreateAnnouncementDto>
{
    public CreateAnnouncementValidator()
    {
        RuleFor(a => a.Title).NotEmpty().WithMessage("Başlık zorunludur.");
        RuleFor(a => a.Content).NotEmpty().WithMessage("İçerik zorunludur.");
        RuleFor(a => a.TargetAudience).NotEmpty().WithMessage("Hedef kitle zorunludur.");
    }
}
