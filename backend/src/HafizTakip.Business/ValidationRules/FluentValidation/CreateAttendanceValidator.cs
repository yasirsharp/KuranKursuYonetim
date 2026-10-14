using FluentValidation;
using HafizTakip.Entities.Dtos;

namespace HafizTakip.Business.ValidationRules.FluentValidation;

public class CreateAttendanceValidator : AbstractValidator<CreateAttendanceDto>
{
    public CreateAttendanceValidator()
    {
        RuleFor(a => a.StudentId).NotEmpty().WithMessage("Öğrenci seçimi zorunludur.");
        RuleFor(a => a.Date).NotEmpty().WithMessage("Yoklama tarihi boş olamaz.");
        RuleFor(a => a.Status).NotEmpty().WithMessage("Yoklama durumu (present, absent vb.) boş olamaz.");
    }
}
