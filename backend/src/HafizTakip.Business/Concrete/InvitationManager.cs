using System.Security.Cryptography;
using HafizTakip.Business.Abstract;
using HafizTakip.Business.Security;
using HafizTakip.Business.ValidationRules.FluentValidation;
using HafizTakip.Core.Aspects.Autofac.Validation;
using HafizTakip.Core.DataAccess;
using HafizTakip.Core.Entities.Platform;
using HafizTakip.Core.Enums;
using HafizTakip.Core.Utilities.Email;
using HafizTakip.Core.Utilities.Helpers;
using HafizTakip.Core.Utilities.IoC;
using HafizTakip.Core.Utilities.Results;
using HafizTakip.Core.Utilities.Security;
using HafizTakip.Core.Utilities.Security.Otp;
using HafizTakip.Core.Utilities.Sms;
using HafizTakip.DataAccess.Abstract;
using HafizTakip.Entities.Concrete;
using HafizTakip.Entities.Dtos;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace HafizTakip.Business.Concrete;

public class InvitationManager : IInvitationService
{
    private const string InvalidInvitationMessage =
        "Davet bağlantısı geçersiz veya süresi dolmuş. Kurs yöneticinizden yeni bir bağlantı isteyin.";

    private readonly IInvitationDal _invitationDal;
    private readonly IJoinRequestDal _joinRequestDal;
    private readonly IUserDal _userDal;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ITenantContext _tenantContext;
    private readonly ICurrentUser _currentUser;
    private readonly ISmsSender _smsSender;
    private readonly IEmailSender _emailSender;
    private readonly OtpChallenge _otp;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<InvitationManager> _logger;

    public InvitationManager(
        IInvitationDal invitationDal,
        IJoinRequestDal joinRequestDal,
        IUserDal userDal,
        IUnitOfWork unitOfWork,
        ITenantContext tenantContext,
        ICurrentUser currentUser,
        ISmsSender smsSender,
        IEmailSender emailSender,
        IMemoryCache cache,
        OtpOptions otpOptions,
        TimeProvider timeProvider,
        ILogger<InvitationManager> logger)
    {
        _invitationDal = invitationDal;
        _joinRequestDal = joinRequestDal;
        _userDal = userDal;
        _unitOfWork = unitOfWork;
        _tenantContext = tenantContext;
        _currentUser = currentUser;
        _smsSender = smsSender;
        _emailSender = emailSender;
        _otp = new OtpChallenge(cache, otpOptions, timeProvider);
        _timeProvider = timeProvider;
        _logger = logger;
    }

    private DateTime UtcNow => _timeProvider.GetUtcNow().UtcDateTime;

    // ── Yönetici: davetler ─────────────────────────────────────────

    public async Task<IDataResult<List<InvitationDto>>> GetActiveAsync()
    {
        var now = UtcNow;
        var invitations = await _invitationDal.GetListAsync(i => i.RevokedAt == null && i.ExpiresAt > now);
        var visible = invitations
            .Where(i => RoleHierarchy.CanAssign(_currentUser.Role, i.Role))
            .OrderBy(i => i.Role == UserRole.HeadTeacher ? 0 : 1)
            .Select(ToDto)
            .ToList();
        return new SuccessDataResult<List<InvitationDto>>(visible, "Davetler listelendi.");
    }

    [ValidationAspect(typeof(CreateInvitationValidator))]
    public async Task<IDataResult<InvitationDto>> CreateAsync(CreateInvitationDto dto)
    {
        if (!RoleHierarchy.CanAssign(_currentUser.Role, dto.Role))
            return new ErrorDataResult<InvitationDto>("Bu rol için davet oluşturma yetkiniz yok.", ResultErrorType.Forbidden);

        var now = UtcNow;
        // Rol başına tek geçerli davet: yenisi oluşturulunca eski bağlantı çalışmaz
        foreach (var old in await _invitationDal.GetListAsync(i => i.Role == dto.Role && i.RevokedAt == null && i.ExpiresAt > now))
        {
            old.RevokedAt = now;
            _invitationDal.Update(old);
        }

        var invitation = new Invitation
        {
            TenantId = _tenantContext.TenantId,
            Code = await NewUniqueCodeAsync(),
            Role = dto.Role,
            ExpiresAt = now.AddDays(dto.ValidDays ?? InvitationRules.DefaultValidDays),
            CreatedBy = _currentUser.UserId
        };
        _invitationDal.Add(invitation);
        await _unitOfWork.SaveChangesAsync();

        return new SuccessDataResult<InvitationDto>(ToDto(invitation), "Davet bağlantısı oluşturuldu.");
    }

    public async Task<IResult> RevokeAsync(Guid id)
    {
        var invitation = await _invitationDal.GetAsync(i => i.Id == id);
        if (invitation == null) return new ErrorResult("Davet bulunamadı.", ResultErrorType.NotFound);
        if (!RoleHierarchy.CanAssign(_currentUser.Role, invitation.Role))
            return new ErrorResult("Bu daveti iptal etme yetkiniz yok.", ResultErrorType.Forbidden);

        if (invitation.RevokedAt == null)
        {
            invitation.RevokedAt = UtcNow;
            await _invitationDal.UpdateAsync(invitation);
        }
        return new SuccessResult("Davet bağlantısı iptal edildi; artık bu bağlantıyla katılınamaz.");
    }

    // ── Yönetici: katılım istekleri ────────────────────────────────

    public async Task<IDataResult<List<JoinRequestDto>>> GetJoinRequestsAsync(string? status)
    {
        status = string.IsNullOrWhiteSpace(status) ? JoinRequestStatus.Pending : status.Trim();
        if (!JoinRequestStatus.All.Contains(status))
            return new ErrorDataResult<List<JoinRequestDto>>("Geçersiz durum.", ResultErrorType.BadRequest);

        var requests = await _joinRequestDal.GetListAsync(r => r.Status == status);
        var visible = requests
            .Where(r => RoleHierarchy.CanAssign(_currentUser.Role, r.Role))
            .OrderByDescending(r => status == JoinRequestStatus.Pending ? DateTime.MinValue : r.ReviewedAt)
            .ThenBy(r => r.CreatedAt)
            .Take(200)
            .Select(ToDto)
            .ToList();
        return new SuccessDataResult<List<JoinRequestDto>>(visible, "Katılım istekleri listelendi.");
    }

    public async Task<IResult> ApproveAsync(Guid joinRequestId)
    {
        var (request, error) = await GetReviewableAsync(joinRequestId);
        if (request == null) return error!;

        if (request.Email != null && await _userDal.AnyAsync(u => u.Email == request.Email))
            return new ErrorResult(
                $"{request.Email} adresi kursta başka bir kullanıcıya ait. İsteği reddedip kişiden e-postasız yeniden katılmasını isteyin.",
                ResultErrorType.Conflict);

        var user = await _userDal.GetByPhoneIncludingDeletedAsync(request.TenantId, request.PhoneNumber);
        if (user is { IsDeleted: false })
            return new ErrorResult("Bu numara kursta zaten kayıtlı bir kullanıcıya ait.", ResultErrorType.Conflict);

        var now = UtcNow;
        if (user == null)
        {
            user = new User { TenantId = request.TenantId, PhoneNumber = request.PhoneNumber };
            _userDal.Add(user);
        }
        else
        {
            // Daha önce silinmiş personel yeniden katılıyor: aynı kayıt yeniden etkinleşir
            user.IsDeleted = false;
            user.DeletedAt = null;
            _userDal.Update(user);
        }
        user.FirstName = request.FirstName;
        user.LastName = request.LastName;
        user.Email = request.Email;
        user.Role = request.Role;

        request.Status = JoinRequestStatus.Approved;
        request.ReviewedAt = now;
        request.ReviewedBy = _currentUser.UserId;
        request.UserId = user.Id;
        _joinRequestDal.Update(request);
        await _unitOfWork.SaveChangesAsync();

        await TryEmailAsync(request.Email, "Hafız Takip: kursa katılımınız onaylandı",
            $"Merhaba {request.FirstName},\n\nKursa katılım isteğiniz onaylandı. Hafız Takip uygulamasında veya " +
            "sitesinde giriş ekranından telefon numaranızla giriş yapabilirsiniz.\n\nHafız Takip");

        return new SuccessResult($"{request.FirstName} {request.LastName} personel olarak eklendi; artık telefon numarasıyla giriş yapabilir.");
    }

    [ValidationAspect(typeof(RejectJoinRequestValidator))]
    public async Task<IResult> RejectAsync(Guid joinRequestId, RejectJoinRequestDto dto)
    {
        var (request, error) = await GetReviewableAsync(joinRequestId);
        if (request == null) return error!;

        request.Status = JoinRequestStatus.Rejected;
        request.ReviewedAt = UtcNow;
        request.ReviewedBy = _currentUser.UserId;
        request.RejectionReason = string.IsNullOrWhiteSpace(dto.Reason) ? null : dto.Reason.Trim();
        await _joinRequestDal.UpdateAsync(request);

        await TryEmailAsync(request.Email, "Hafız Takip: kursa katılım isteğiniz onaylanmadı",
            $"Merhaba {request.FirstName},\n\nKursa katılım isteğiniz kurs yöneticisi tarafından onaylanmadı." +
            (request.RejectionReason != null ? $"\nGerekçe: {request.RejectionReason}" : string.Empty) +
            "\n\nHafız Takip");

        return new SuccessResult("Katılım isteği reddedildi.");
    }

    private async Task<(JoinRequest? Request, IResult? Error)> GetReviewableAsync(Guid id)
    {
        var request = await _joinRequestDal.GetAsync(r => r.Id == id);
        if (request == null) return (null, new ErrorResult("Katılım isteği bulunamadı.", ResultErrorType.NotFound));
        if (!RoleHierarchy.CanAssign(_currentUser.Role, request.Role))
            return (null, new ErrorResult("Bu katılım isteğini değerlendirme yetkiniz yok.", ResultErrorType.Forbidden));
        if (request.Status != JoinRequestStatus.Pending)
            return (null, new ErrorResult("Bu katılım isteği zaten değerlendirilmiş.", ResultErrorType.Conflict));
        return (request, null);
    }

    // ── Herkese açık: davetle katılım ──────────────────────────────

    public async Task<IDataResult<InvitationPublicDto>> GetPublicAsync(string code)
    {
        var found = await FindUsableAsync(code);
        if (found == null) return new ErrorDataResult<InvitationPublicDto>(InvalidInvitationMessage, ResultErrorType.NotFound);

        var (invitation, tenant) = found.Value;
        return new SuccessDataResult<InvitationPublicDto>(new InvitationPublicDto
        {
            Code = invitation.Code,
            CourseName = tenant.Name,
            City = tenant.City,
            District = tenant.District,
            Role = invitation.Role,
            ExpiresAt = invitation.ExpiresAt
        }, "Davet geçerli.");
    }

    [ValidationAspect(typeof(SendOtpValidator))]
    public async Task<IResult> SendJoinCodeAsync(string code, SendOtpDto dto)
    {
        var found = await FindUsableAsync(code);
        if (found == null) return new ErrorResult(InvalidInvitationMessage, ResultErrorType.NotFound);
        var (invitation, _) = found.Value;
        _tenantContext.SetTenantId(invitation.TenantId);

        var phone = PhoneNumberHelper.Normalize(dto.PhoneNumber);
        var conflict = await CheckCanJoinAsync(phone);
        if (conflict != null) return conflict;

        var throttle = _otp.CheckAndRegisterSendAttempt(phone);
        if (throttle != null) return throttle;

        var otp = _otp.Issue(OtpPurpose.Invitation, phone);
        var sent = await _smsSender.SendAsync(phone,
            $"Hafız Takip kursa katılım doğrulama kodunuz: {otp}. Kod {_otp.ExpiryMinutes} dakika geçerlidir. Kimseyle paylaşmayın.");
        if (!sent)
        {
            _otp.Consume(OtpPurpose.Invitation, phone);
            return new ErrorResult("Doğrulama kodu şu anda gönderilemedi. Lütfen biraz sonra tekrar deneyin.");
        }

        return new SuccessResult("Doğrulama kodu telefonunuza gönderildi.");
    }

    [ValidationAspect(typeof(JoinWithInvitationValidator))]
    public async Task<IResult> JoinAsync(string code, JoinWithInvitationDto dto)
    {
        var found = await FindUsableAsync(code);
        if (found == null) return new ErrorResult(InvalidInvitationMessage, ResultErrorType.NotFound);
        var (invitation, tenant) = found.Value;
        _tenantContext.SetTenantId(invitation.TenantId);

        var phone = PhoneNumberHelper.Normalize(dto.PhoneNumber);
        switch (_otp.Verify(OtpPurpose.Invitation, phone, dto.Code))
        {
            case OtpVerification.TooManyAttempts:
                return new ErrorResult("Çok fazla hatalı deneme yapıldı. Lütfen yeni kod isteyin.");
            case OtpVerification.InvalidOrExpired:
                return new ErrorResult("Doğrulama kodu hatalı veya süresi dolmuş.");
        }

        var conflict = await CheckCanJoinAsync(phone);
        if (conflict != null) return conflict;

        var request = new JoinRequest
        {
            TenantId = invitation.TenantId,
            InvitationId = invitation.Id,
            FirstName = dto.FirstName.Trim(),
            LastName = dto.LastName.Trim(),
            PhoneNumber = phone,
            Email = EmailHelper.NormalizeOrNull(dto.Email),
            Role = invitation.Role,
            Status = JoinRequestStatus.Pending
        };
        await _joinRequestDal.AddAsync(request);
        _otp.Consume(OtpPurpose.Invitation, phone);

        await NotifyAdminsAsync(tenant, request);
        return new SuccessResult(
            "Katılım isteğiniz kurs yöneticisine iletildi. Onaylandığında bu telefon numarasıyla giriş yapabilirsiniz.");
    }

    /// <summary>Kod geçerli, iptal edilmemiş, süresi dolmamış ve kurs kullanımda mı?</summary>
    private async Task<(Invitation Invitation, Tenant Tenant)?> FindUsableAsync(string code)
    {
        var normalized = NormalizeCode(code);
        if (normalized.Length != InvitationRules.CodeLength) return null;

        var found = await _invitationDal.GetByCodeAcrossTenantsAsync(normalized);
        if (found == null) return null;

        var (invitation, tenant) = found.Value;
        if (invitation.RevokedAt != null || invitation.ExpiresAt <= UtcNow) return null;
        if (!TenantStatus.HasFullAccess(tenant.Status, tenant.IsActive)) return null;
        return found;
    }

    /// <summary>Tenant bağlamı davetin kursuna ayarlanmış olmalıdır.</summary>
    private async Task<IResult?> CheckCanJoinAsync(string phone)
    {
        if (await _userDal.AnyAsync(u => u.PhoneNumber == phone))
            return new ErrorResult(
                "Bu numara bu kursta zaten kayıtlı. Giriş ekranından telefon numaranızla giriş yapabilirsiniz.",
                ResultErrorType.Conflict);
        if (await _joinRequestDal.AnyAsync(r => r.PhoneNumber == phone && r.Status == JoinRequestStatus.Pending))
            return new ErrorResult(
                "Bu numarayla gönderilmiş, onay bekleyen bir katılım isteğiniz var. Kurs yöneticisi onayladığında giriş yapabilirsiniz.",
                ResultErrorType.Conflict);
        return null;
    }

    private async Task NotifyAdminsAsync(Tenant tenant, JoinRequest request)
    {
        try
        {
            var roleLabel = request.Role == UserRole.HeadTeacher ? "hoca" : "yardımcı hoca";
            var admins = await _userDal.GetTenantAdminsAcrossTenantsAsync(tenant.Id);
            foreach (var email in admins.Select(a => EmailHelper.NormalizeOrNull(a.Email)).Where(e => e != null).Distinct())
            {
                await TryEmailAsync(email, "Hafız Takip: yeni katılım isteği",
                    $"{request.FirstName} {request.LastName}, {tenant.Name} kursuna {roleLabel} olarak katılmak istiyor.\n" +
                    "Onaylamak için Hafız Takip'te Personel ekranındaki \"Katılım istekleri\" bölümüne bakın.\n\nHafız Takip");
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Katılım isteği bildirimi gönderilemedi (kurs {TenantId}).", tenant.Id);
        }
    }

    private async Task TryEmailAsync(string? to, string subject, string body)
    {
        if (to == null || !EmailHelper.IsValid(to)) return;
        try
        {
            if (!await _emailSender.SendAsync(to, subject, body))
                _logger.LogWarning("E-posta gönderilemedi: {Subject}", subject);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "E-posta gönderilemedi: {Subject}", subject);
        }
    }

    private async Task<string> NewUniqueCodeAsync()
    {
        for (var attempt = 0; attempt < 5; attempt++)
        {
            var code = RandomNumberGenerator.GetString(InvitationRules.CodeAlphabet, InvitationRules.CodeLength);
            if (!await _invitationDal.CodeExistsAsync(code)) return code;
        }
        throw new InvalidOperationException("Benzersiz davet kodu üretilemedi.");
    }

    /// <summary>Kullanıcının elle yazdığı kod: boşluk/tire atılır, büyük harfe çevrilir.</summary>
    public static string NormalizeCode(string? code) =>
        new string((code ?? string.Empty).Where(char.IsLetterOrDigit).ToArray()).ToUpperInvariant();

    private static InvitationDto ToDto(Invitation i) => new()
    {
        Id = i.Id,
        Code = i.Code,
        Role = i.Role,
        ExpiresAt = i.ExpiresAt,
        CreatedAt = i.CreatedAt
    };

    private static JoinRequestDto ToDto(JoinRequest r) => new()
    {
        Id = r.Id,
        FirstName = r.FirstName,
        LastName = r.LastName,
        PhoneNumber = r.PhoneNumber,
        Email = r.Email,
        Role = r.Role,
        Status = r.Status,
        CreatedAt = r.CreatedAt,
        ReviewedAt = r.ReviewedAt,
        RejectionReason = r.RejectionReason
    };
}
