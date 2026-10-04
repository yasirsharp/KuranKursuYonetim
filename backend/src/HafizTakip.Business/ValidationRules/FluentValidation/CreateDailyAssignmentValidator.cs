using FluentValidation;
using HafizTakip.Entities.Dtos;

namespace HafizTakip.Business.ValidationRules.FluentValidation;

public class CreateDailyAssignmentValidator : AbstractValidator<CreateDailyAssignmentDto>
{
    public CreateDailyAssignmentValidator()
    {
        RuleFor(d => d.StudentId).NotEmpty().WithMessage("Öğrenci ID boş olamaz.");
        RuleFor(d => d.AssignmentDate).NotEmpty().WithMessage("Tarih boş olamaz.");
    }
}
