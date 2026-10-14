using FluentValidation;
using HafizTakip.Core.Enums;
using HafizTakip.Entities.Dtos;

namespace HafizTakip.Business.ValidationRules.FluentValidation;

/// <summary>Yoklama güncellemesi (PUT /api/attendance, gövde AttendanceDto).</summary>
public class UpdateAttendanceValidator : AbstractValidator<AttendanceDto>
{
    public UpdateAttendanceValidator()
    {
        RuleFor(a => a.Id).NotEmpty().WithMessage("Yoklama kaydı seçilmelidir.");
        RuleFor(a => a.Status).MustBeOneOf(AttendanceStatus.All, "yoklama durumu");
        RuleFor(a => a.Notes).MaximumLength(1000);
    }
}
