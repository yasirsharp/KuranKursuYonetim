using FluentValidation;
using HafizTakip.Entities.Dtos;

namespace HafizTakip.Business.ValidationRules.FluentValidation;

public class CreateAcademicTermValidator : AbstractValidator<CreateAcademicTermDto>
{
    public CreateAcademicTermValidator()
    {
        RuleFor(t => t.Name).NotEmpty().WithMessage("Dönem adı zorunludur.").MaximumLength(100);
        RuleFor(t => t.StartDate).NotEmpty().WithMessage("Dönem başlangıç tarihi zorunludur.");
        RuleFor(t => t.EndDate)
            .GreaterThan(t => t.StartDate).WithMessage("Dönem bitiş tarihi başlangıç tarihinden sonra olmalıdır.")
            .Must((t, end) => end <= t.StartDate.AddMonths(AcademicTermRules.MaxMonths))
            .WithMessage($"Bir dönem en fazla {AcademicTermRules.MaxMonths} ay sürebilir.");
        RuleFor(t => t.Notes).MaximumLength(1000);
    }
}

public class UpdateAcademicTermValidator : AbstractValidator<UpdateAcademicTermDto>
{
    public UpdateAcademicTermValidator()
    {
        RuleFor(t => t.Id).NotEmpty();
        RuleFor(t => t.Name).NotEmpty().WithMessage("Dönem adı zorunludur.").MaximumLength(100);
        RuleFor(t => t.StartDate).NotEmpty().WithMessage("Dönem başlangıç tarihi zorunludur.");
        RuleFor(t => t.EndDate)
            .GreaterThan(t => t.StartDate).WithMessage("Dönem bitiş tarihi başlangıç tarihinden sonra olmalıdır.")
            .Must((t, end) => end <= t.StartDate.AddMonths(AcademicTermRules.MaxMonths))
            .WithMessage($"Bir dönem en fazla {AcademicTermRules.MaxMonths} ay sürebilir.");
        RuleFor(t => t.Notes).MaximumLength(1000);
    }
}

internal static class AcademicTermRules
{
    public const int MaxMonths = 18;
}
