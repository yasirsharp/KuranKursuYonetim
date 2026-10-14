using FluentValidation;
using HafizTakip.Core.Enums;
using HafizTakip.Entities.Dtos;

namespace HafizTakip.Business.ValidationRules.FluentValidation;

public class UpdateSubscriptionValidator : AbstractValidator<UpdateSubscriptionDto>
{
    public UpdateSubscriptionValidator()
    {
        RuleFor(s => s.Id).NotEmpty();
        RuleFor(s => s.PlanName).NotEmpty().WithMessage("Plan adı zorunludur.").MaximumLength(100);
        RuleFor(s => s.Status).MustBeOneOf(SubscriptionStatus.All, "abonelik durumu");
    }
}
