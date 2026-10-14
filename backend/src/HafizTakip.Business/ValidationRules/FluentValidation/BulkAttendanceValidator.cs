using FluentValidation;
using HafizTakip.Entities.Dtos;

namespace HafizTakip.Business.ValidationRules.FluentValidation;

public class BulkAttendanceValidator : AbstractValidator<BulkAttendanceDto>
{
    public BulkAttendanceValidator()
    {
        RuleFor(a => a.Date).NotEmpty().WithMessage("Yoklama tarihi boş olamaz.").MustNotBeInFuture();
        RuleFor(a => a.Attendances).NotEmpty().WithMessage("Yoklama listesi boş olamaz.");
        RuleFor(a => a.Attendances.Count).LessThanOrEqualTo(500).WithMessage("Tek seferde en fazla 500 yoklama kaydı gönderilebilir.")
            .When(a => a.Attendances != null);
        RuleForEach(a => a.Attendances).SetValidator(new CreateAttendanceItemValidator());
    }
}
