using FluentValidation;
using HafizTakip.Entities.Dtos;

namespace HafizTakip.Business.ValidationRules.FluentValidation;

public class RejectCourseApplicationValidator : AbstractValidator<RejectCourseApplicationDto>
{
    public RejectCourseApplicationValidator()
    {
        RuleFor(x => x.Reason)
            .NotEmpty().WithMessage("Ret gerekçesi boş olamaz; başvuru sahibine SMS ile iletilir.")
            .MinimumLength(5).MaximumLength(500);
    }
}
