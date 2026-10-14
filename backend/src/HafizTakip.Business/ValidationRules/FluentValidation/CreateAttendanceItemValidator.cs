using FluentValidation;
using HafizTakip.Core.Enums;
using HafizTakip.Entities.Dtos;

namespace HafizTakip.Business.ValidationRules.FluentValidation;

public class CreateAttendanceItemValidator : AbstractValidator<CreateAttendanceItemDto>
{
    public CreateAttendanceItemValidator()
    {
        RuleFor(a => a.StudentId).NotEmpty().WithMessage("Öğrenci seçimi zorunludur.");
        RuleFor(a => a.Status).MustBeOneOf(AttendanceStatus.All, "yoklama durumu");
        RuleFor(a => a.Notes).MaximumLength(1000);
    }
}
