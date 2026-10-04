using FluentValidation;
using HafizTakip.Entities.Dtos;
using System;

namespace HafizTakip.Business.ValidationRules.FluentValidation;

public class CreateLessonValidator : AbstractValidator<CreateLessonDto>
{
    public CreateLessonValidator()
    {
        RuleFor(l => l.StudentId).NotEmpty().WithMessage("Öğrenci seçimi zorunludur.");
        RuleFor(l => l.TeacherId).NotEmpty().WithMessage("Öğretmen seçimi zorunludur.");
        RuleFor(l => l.LessonDate).NotEmpty().WithMessage("Ders tarihi boş olamaz.");
        RuleFor(l => l.Performance).NotEmpty().WithMessage("Performans değerlendirmesi boş olamaz.");
    }
}
