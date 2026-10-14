using FluentValidation;
using HafizTakip.Entities.Dtos;

namespace HafizTakip.Business.ValidationRules.FluentValidation;

public class BulkAttendanceValidator : AbstractValidator<BulkAttendanceDto>
{
    public BulkAttendanceValidator()
    {
        RuleFor(a => a.Date).NotEmpty().WithMessage("Yoklama tarihi boş olamaz.");
        RuleFor(a => a.Attendances).NotEmpty().WithMessage("Yoklama listesi boş olamaz.");
        RuleForEach(a => a.Attendances).SetValidator(new CreateAttendanceItemValidator());
    }
}
