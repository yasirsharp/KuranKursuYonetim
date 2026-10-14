using FluentValidation;
using HafizTakip.Core.Enums;
using HafizTakip.Entities.Dtos;

namespace HafizTakip.Business.ValidationRules.FluentValidation;

public class CreateEtutNoteValidator : AbstractValidator<CreateEtutNoteDto>
{
    public CreateEtutNoteValidator()
    {
        RuleFor(n => n.StudentId).NotEmpty().WithMessage("Öğrenci seçimi zorunludur.");
        RuleFor(n => n.NoteDate).NotEmpty().WithMessage("Tarih zorunludur.");
        RuleFor(n => n.HomeworkStatus).MustBeOneOf(HomeworkStatus.All, "ödev durumu");
        RuleFor(n => n.TeacherNotes).MaximumLength(2000);
    }
}
