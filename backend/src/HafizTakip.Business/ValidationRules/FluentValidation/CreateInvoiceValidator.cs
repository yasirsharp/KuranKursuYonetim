using FluentValidation;
using HafizTakip.Entities.Dtos;

namespace HafizTakip.Business.ValidationRules.FluentValidation;

public class CreateInvoiceValidator : AbstractValidator<CreateInvoiceDto>
{
    public CreateInvoiceValidator()
    {
        RuleFor(i => i.TenantId).NotEmpty().WithMessage("Kurs seçimi zorunludur.");
        RuleFor(i => i.SubscriptionId).NotEmpty().WithMessage("Abonelik seçimi zorunludur.");
        RuleFor(i => i.InvoiceNo).NotEmpty().WithMessage("Fatura numarası zorunludur.").MaximumLength(50);
        RuleFor(i => i.Amount).GreaterThan(0).WithMessage("Tutar sıfırdan büyük olmalıdır.");
        RuleFor(i => i.DueDate).NotEmpty();
    }
}
