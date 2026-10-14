using FluentValidation;
using HafizTakip.Entities.Dtos;

namespace HafizTakip.Business.ValidationRules.FluentValidation;

public class CreateAttendanceItemValidator : AbstractValidator<CreateAttendanceItemDto>
{
    public CreateAttendanceItemValidator()
    {
        RuleFor(a => a.StudentId).NotEmpty().WithMessage("Öğrenci seçimi zorunludur.");
        RuleFor(a => a.Status).NotEmpty().WithMessage("Yoklama durumu boş olamaz.");
    }
}
