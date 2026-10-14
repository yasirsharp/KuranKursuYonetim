using FluentValidation;
using HafizTakip.Entities.Dtos;

namespace HafizTakip.Business.ValidationRules.FluentValidation;

public class CreateSubscriptionValidator : AbstractValidator<CreateSubscriptionDto>
{
    public CreateSubscriptionValidator()
    {
        RuleFor(s => s.TenantId).NotEmpty().WithMessage("Kurs seçimi zorunludur.");
        RuleFor(s => s.PlanName).NotEmpty().WithMessage("Plan adı zorunludur.").MaximumLength(100);
        RuleFor(s => s.StartDate).NotEmpty();
        RuleFor(s => s.EndDate).GreaterThan(s => s.StartDate).WithMessage("Bitiş tarihi başlangıçtan sonra olmalıdır.");
    }
}
