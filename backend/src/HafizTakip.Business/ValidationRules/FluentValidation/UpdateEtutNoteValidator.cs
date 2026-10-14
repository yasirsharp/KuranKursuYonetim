using FluentValidation;
using HafizTakip.Core.Enums;
using HafizTakip.Entities.Dtos;

namespace HafizTakip.Business.ValidationRules.FluentValidation;

public class UpdateEtutNoteValidator : AbstractValidator<UpdateEtutNoteDto>
{
    public UpdateEtutNoteValidator()
    {
        RuleFor(n => n.Id).NotEmpty().WithMessage("Etüt notu seçilmelidir.");
        RuleFor(n => n.HomeworkStatus).MustBeOneOf(HomeworkStatus.All, "ödev durumu");
        RuleFor(n => n.TeacherNotes).MaximumLength(2000);
    }
}
