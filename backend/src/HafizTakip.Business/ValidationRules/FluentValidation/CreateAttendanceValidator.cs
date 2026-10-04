using FluentValidation;
using HafizTakip.Entities.Dtos;
using System;

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

public class CreateAttendanceItemValidator : AbstractValidator<CreateAttendanceItemDto>
{
    public CreateAttendanceItemValidator()
    {
        RuleFor(a => a.StudentId).NotEmpty().WithMessage("Öğrenci seçimi zorunludur.");
        RuleFor(a => a.Status).NotEmpty().WithMessage("Yoklama durumu boş olamaz.");
    }
}

public class BulkAttendanceValidator : AbstractValidator<BulkAttendanceDto>
{
    public BulkAttendanceValidator()
    {
        RuleFor(a => a.Date).NotEmpty().WithMessage("Yoklama tarihi boş olamaz.");
        RuleFor(a => a.Attendances).NotEmpty().WithMessage("Yoklama listesi boş olamaz.");
        RuleForEach(a => a.Attendances).SetValidator(new CreateAttendanceItemValidator());
    }
}
