using HafizTakip.Business.Abstract;
using HafizTakip.Business.Security;
using HafizTakip.Business.ValidationRules.FluentValidation;
using HafizTakip.Core.Aspects.Autofac.Validation;
using HafizTakip.Core.Constants;
using HafizTakip.Core.DataAccess;
using HafizTakip.Core.Entities.Platform;
using HafizTakip.Core.Enums;
using HafizTakip.Core.Utilities.Helpers;
using HafizTakip.Core.Utilities.IoC;
using HafizTakip.Core.Utilities.Results;
using HafizTakip.Core.Utilities.Security.Otp;
using HafizTakip.Core.Utilities.Sms;
using HafizTakip.DataAccess.Abstract;
using HafizTakip.Entities.Concrete;
using HafizTakip.Entities.Dtos;
using Microsoft.Extensions.Caching.Memory;

namespace HafizTakip.Business.Concrete;

public class CourseApplicationManager : ICourseApplicationService
{
    private readonly ITenantDal _tenantDal;
    private readonly ITenantSettingDal _settingDal;
    private readonly IUserDal _userDal;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAuthService _authService;
    private readonly ITenantContext _tenantContext;
    private readonly ISmsSender _smsSender;
    private readonly IMemoryCache _cache;
    private readonly OtpChallenge _otp;
    private readonly TimeProvider _timeProvider;

    public CourseApplicationManager(
        ITenantDal tenantDal,
        ITenantSettingDal settingDal,
        IUserDal userDal,
        IUnitOfWork unitOfWork,
        IAuthService authService,
        ITenantContext tenantContext,
        ISmsSender smsSender,
        IMemoryCache cache,
        OtpOptions otpOptions,
        TimeProvider timeProvider)
    {
        _tenantDal = tenantDal;
        _settingDal = settingDal;
        _userDal = userDal;
        _unitOfWork = unitOfWork;
        _authService = authService;
        _tenantContext = tenantContext;
        _smsSender = smsSender;
        _cache = cache;
        _otp = new OtpChallenge(cache, otpOptions, timeProvider);
        _timeProvider = timeProvider;
    }

    private DateTime UtcNow => _timeProvider.GetUtcNow().UtcDateTime;

    [ValidationAspect(typeof(SendOtpValidator))]
    public async Task<IResult> SendCodeAsync(SendOtpDto dto)
    {
        var phone = PhoneNumberHelper.Normalize(dto.PhoneNumber);

        var throttle = _otp.CheckAndRegisterSendAttempt(phone);
        if (throttle != null) return throttle;

        var code = _otp.Issue(OtpPurpose.CourseApplication, phone);
        var sent = await _smsSender.SendAsync(phone,
            $"Hafız Takip kurs başvurusu doğrulama kodunuz: {code}. Kod {_otp.ExpiryMinutes} dakika geçerlidir. Kimseyle paylaşmayın.");

        if (!sent)
        {
            _otp.Consume(OtpPurpose.CourseApplication, phone);
            return new ErrorResult("Doğrulama kodu şu anda gönderilemedi. Lütfen biraz sonra tekrar deneyin.");
        }

        return new SuccessResult("Doğrulama kodu gönderildi.");
    }

    [ValidationAspect(typeof(CourseApplicationValidator))]
    public async Task<IDataResult<TokenResponseDto>> SubmitAsync(CourseApplicationDto dto)
    {
        var phone = PhoneNumberHelper.Normalize(dto.PhoneNumber);

        switch (_otp.Verify(OtpPurpose.CourseApplication, phone, dto.Code))
        {
            case OtpVerification.TooManyAttempts:
                return new ErrorDataResult<TokenResponseDto>("Çok fazla hatalı deneme yapıldı. Lütfen yeni kod isteyin.");
            case OtpVerification.InvalidOrExpired:
                return new ErrorDataResult<TokenResponseDto>("Doğrulama kodu hatalı veya süresi dolmuş.");
        }

        if (await _userDal.HasPendingApplicationAsync(phone))
        {
            return new ErrorDataResult<TokenResponseDto>(
                "Bu telefon numarasıyla onay bekleyen bir kurs başvurunuz zaten var. Sonuç SMS ile bildirilecek.",
                ResultErrorType.Conflict);
        }

        var baseSlug = SlugHelper.From(dto.CourseName);
        var slug = SlugHelper.MakeUnique(baseSlug, await _tenantDal.GetTakenSlugsAsync(baseSlug));

        var result = await _unitOfWork.ExecuteInTransactionAsync<IDataResult<TokenResponseDto>>(async () =>
        {
            var tenant = new Tenant
            {
                Id = Guid.NewGuid(),
                Name = dto.CourseName.Trim(),
                Slug = slug,
                Status = TenantStatus.Pending,
                IsActive = true,
                City = dto.City.Trim(),
                District = dto.District.Trim(),
                Address = Clean(dto.Address),
                ContactEmail = Clean(dto.ContactEmail),
                ContactPhone = phone,
                SubmittedAt = UtcNow
            };
            var admin = new User
            {
                Id = Guid.NewGuid(),
                TenantId = tenant.Id,
                FirstName = dto.AdminFirstName.Trim(),
                LastName = dto.AdminLastName.Trim(),
                PhoneNumber = phone,
                Role = UserRole.TenantAdmin
            };

            // Kurs, varsayılan ayarları ve yöneticisi aynı transaction'da kaydedilir; oturum açılamazsa hepsi geri alınır.
            // EF modelinde kurs ile bağlı tablolar arasında ilişki tanımlı olmadığından insert sırası garanti değildir:
            // önce kurs yazılır.
            _tenantDal.Add(tenant);
            await _unitOfWork.SaveChangesAsync();
            _settingDal.Add(new TenantSetting { TenantId = tenant.Id });
            _userDal.Add(admin);
            await _unitOfWork.SaveChangesAsync();

            var session = await _authService.CreateSessionAsync(admin);
            return new SuccessDataResult<TokenResponseDto>(session,
                "Kurs başvurunuz alındı. Platform yöneticisi onayladığında SMS ile bilgilendirileceksiniz.");
        });

        if (result.Success) _otp.Consume(OtpPurpose.CourseApplication, phone);
        return result;
    }

    public async Task<IDataResult<CourseApplicationStatusDto>> GetMineAsync()
    {
        var tenant = await _tenantDal.GetAsync(t => t.Id == _tenantContext.TenantId);
        if (tenant == null)
            return new ErrorDataResult<CourseApplicationStatusDto>("Kurs bulunamadı.", ResultErrorType.NotFound);

        return new SuccessDataResult<CourseApplicationStatusDto>(new CourseApplicationStatusDto
        {
            TenantId = tenant.Id,
            CourseName = tenant.Name,
            Status = tenant.Status,
            City = tenant.City,
            District = tenant.District,
            Address = tenant.Address,
            ContactEmail = tenant.ContactEmail,
            SubmittedAt = tenant.SubmittedAt,
            ReviewedAt = tenant.ReviewedAt,
            RejectionReason = tenant.RejectionReason
        }, StatusMessage(tenant.Status));
    }

    [ValidationAspect(typeof(CourseApplicationInfoValidator))]
    public async Task<IResult> ResubmitAsync(CourseApplicationInfoDto dto)
    {
        var tenant = await _tenantDal.GetAsync(t => t.Id == _tenantContext.TenantId);
        if (tenant == null) return new ErrorResult("Kurs bulunamadı.", ResultErrorType.NotFound);
        if (tenant.Status != TenantStatus.Rejected)
            return new ErrorResult("Yalnızca reddedilen başvurular yeniden gönderilebilir.", ResultErrorType.Conflict);

        tenant.Name = dto.CourseName.Trim();
        tenant.City = dto.City.Trim();
        tenant.District = dto.District.Trim();
        tenant.Address = Clean(dto.Address);
        tenant.ContactEmail = Clean(dto.ContactEmail);
        tenant.Status = TenantStatus.Pending;
        tenant.SubmittedAt = UtcNow;
        tenant.ReviewedAt = null;
        tenant.ReviewedBy = null;
        tenant.RejectionReason = null;

        await _tenantDal.UpdateAsync(tenant);
        _cache.Remove(TenantCacheKeys.Status(tenant.Id));
        return new SuccessResult("Başvurunuz yeniden onaya gönderildi.");
    }

    private static string StatusMessage(string status) => status switch
    {
        TenantStatus.Pending => "Kursunuz onay bekliyor.",
        TenantStatus.Rejected => "Kurs başvurunuz onaylanmadı. Bilgilerinizi düzenleyip yeniden gönderebilirsiniz.",
        _ => "Kursunuz onaylandı."
    };

    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
