using FluentValidation;
using HafizTakip.Entities.Dtos;

namespace HafizTakip.Business.ValidationRules.FluentValidation;

public class CreateEtutNoteValidator : AbstractValidator<CreateEtutNoteDto>
{
    public CreateEtutNoteValidator()
    {
        RuleFor(n => n.StudentId).NotEmpty().WithMessage("Öğrenci seçimi zorunludur.");
        RuleFor(n => n.TeacherId).NotEmpty().WithMessage("Hoca seçimi zorunludur.");
        RuleFor(n => n.NoteDate).NotEmpty().WithMessage("Tarih zorunludur.");
        RuleFor(n => n.HomeworkStatus).NotEmpty().WithMessage("Ödev durumu zorunludur.");
        RuleFor(n => n.HomeworkStatus).Must(BeAValidStatus).WithMessage("Geçersiz ödev durumu.");
    }

    private bool BeAValidStatus(string arg)
    {
        return arg == "yapıldı" || arg == "eksik" || arg == "yapılmadı" || arg == "mazeretli";
    }
}
