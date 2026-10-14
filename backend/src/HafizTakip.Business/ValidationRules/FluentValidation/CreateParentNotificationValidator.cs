using FluentValidation;
using HafizTakip.Entities.Dtos;

namespace HafizTakip.Business.ValidationRules.FluentValidation;

public class CreateParentNotificationValidator : AbstractValidator<CreateParentNotificationDto>
{
    public CreateParentNotificationValidator()
    {
        RuleFor(n => n.StudentId).NotEmpty().WithMessage("Öğrenci seçimi zorunludur.");
        RuleFor(n => n.NotificationType).NotEmpty().MaximumLength(50);
        RuleFor(n => n.Title).NotEmpty().WithMessage("Başlık zorunludur.").MaximumLength(200);
        RuleFor(n => n.Message).NotEmpty().WithMessage("Mesaj zorunludur.").MaximumLength(2000);
        RuleFor(n => n.RecipientPhone).NotEmpty().MaximumLength(20);
        RuleFor(n => n.DeliveryChannel).NotEmpty().MaximumLength(50);
    }
}
