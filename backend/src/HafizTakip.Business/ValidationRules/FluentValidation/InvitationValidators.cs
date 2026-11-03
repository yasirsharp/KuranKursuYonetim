using FluentValidation;
using HafizTakip.Core.Enums;
using HafizTakip.Core.Utilities.Helpers;
using HafizTakip.Entities.Dtos;

namespace HafizTakip.Business.ValidationRules.FluentValidation;

public class CreateInvitationValidator : AbstractValidator<CreateInvitationDto>
{
    public CreateInvitationValidator()
    {
        RuleFor(x => x.Role)
            .Must(r => InvitationRules.Roles.Contains(r)).WithMessage("Davet yalnızca hoca veya yardımcı hoca için oluşturulabilir.");
        RuleFor(x => x.ValidDays)
            .InclusiveBetween(1, InvitationRules.MaxValidDays)
            .WithMessage($"Davet 1 ile {InvitationRules.MaxValidDays} gün arasında geçerli olabilir.")
            .When(x => x.ValidDays.HasValue);
    }
}

public class JoinWithInvitationValidator : AbstractValidator<JoinWithInvitationDto>
{
    public JoinWithInvitationValidator()
    {
        RuleFor(x => x.FirstName).NotEmpty().WithMessage("Ad boş olamaz.").MaximumLength(100);
        RuleFor(x => x.LastName).NotEmpty().WithMessage("Soyad boş olamaz.").MaximumLength(100);
        RuleFor(x => x.PhoneNumber)
            .Must(PhoneNumberHelper.IsValidMobile).WithMessage("Geçerli bir cep telefonu numarası giriniz (5XX XXX XX XX).");
        RuleFor(x => x.Email)
            .EmailAddress().WithMessage("Geçerli bir e-posta adresi giriniz.").MaximumLength(255)
            .When(x => !string.IsNullOrWhiteSpace(x.Email));
        RuleFor(x => x.Code)
            .NotEmpty().WithMessage("Doğrulama kodu boş olamaz.")
            .Matches("^[0-9]{4,8}$").WithMessage("Doğrulama kodu yalnızca rakamlardan oluşmalıdır.");
    }
}

public class RejectJoinRequestValidator : AbstractValidator<RejectJoinRequestDto>
{
    public RejectJoinRequestValidator()
    {
        RuleFor(x => x.Reason).MaximumLength(500).WithMessage("Gerekçe en fazla 500 karakter olabilir.");
    }
}
