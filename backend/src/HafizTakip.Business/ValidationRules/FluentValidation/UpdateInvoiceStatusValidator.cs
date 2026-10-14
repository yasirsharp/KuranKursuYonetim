using FluentValidation;
using HafizTakip.Core.Enums;
using HafizTakip.Entities.Dtos;

namespace HafizTakip.Business.ValidationRules.FluentValidation;

public class UpdateInvoiceStatusValidator : AbstractValidator<UpdateInvoiceStatusDto>
{
    public UpdateInvoiceStatusValidator()
    {
        RuleFor(i => i.Id).NotEmpty();
        RuleFor(i => i.Status).MustBeOneOf(InvoiceStatus.All, "fatura durumu");
    }
}
