using System.Security.Cryptography;
using System.Text;
using HafizTakip.Business.Abstract;
using HafizTakip.Business.Security;
using HafizTakip.Business.ValidationRules.FluentValidation;
using HafizTakip.Core.Aspects.Autofac.Validation;
using HafizTakip.Core.Entities.Platform;
using HafizTakip.Core.Enums;
using HafizTakip.Core.Utilities.Email;
using HafizTakip.Core.Utilities.Helpers;
using HafizTakip.Core.Utilities.Results;
using HafizTakip.Core.Utilities.Security.Jwt;
using HafizTakip.Core.Utilities.Security.Otp;
using HafizTakip.Core.Utilities.Sms;
using HafizTakip.DataAccess.Abstract;
using HafizTakip.Entities.Concrete;
using HafizTakip.Entities.Dtos;
using Microsoft.Extensions.Caching.Memory;

namespace HafizTakip.Business.Concrete;

/// <summary>
/// Telefon veya e-posta + tek kullanımlık kod ile giriş, refresh token rotasyonu ve çıkış.
/// Güvenlik notları:
/// - OTP kodu cache'te yalnızca SHA-256 özeti olarak tutulur; hatalı deneme sayısı sınırlıdır.
/// - Numara kayıtlı olsun olmasın send-otp aynı yanıtı döner (numara keşfi engellenir).
/// - Refresh token'ın kendisi saklanmaz; her kullanımda döndürülür, iptal edilmiş bir token
///   tekrar gelirse kullanıcının tüm oturumları kapatılır.
/// </summary>
public class AuthService : IAuthService
{
    private const string GenericSendMessage = "Telefon numarası kayıtlıysa doğrulama kodu gönderildi.";
    private const string GenericEmailSendMessage = "E-posta adresi kayıtlıysa doğrulama kodu gönderildi.";
    private const string InvalidCodeMessage = "Doğrulama kodu hatalı veya süresi dolmuş.";
    private const string InvalidSessionMessage = "Oturum geçersiz veya süresi dolmuş. Lütfen tekrar giriş yapın.";

    private readonly IUserDal _userDal;
    private readonly ITenantDal _tenantDal;
    private readonly IRefreshTokenDal _refreshTokenDal;
    private readonly ITokenHelper _tokenHelper;
    private readonly ISmsSender _smsSender;
    private readonly IEmailSender _emailSender;
    private readonly OtpChallenge _otp;
    private readonly TokenOptions _tokenOptions;
    private readonly TimeProvider _timeProvider;

    public AuthService(
        IUserDal userDal,
        ITenantDal tenantDal,
        IRefreshTokenDal refreshTokenDal,
        ITokenHelper tokenHelper,
        IMemoryCache cache,
        ISmsSender smsSender,
        IEmailSender emailSender,
        OtpOptions otpOptions,
        TokenOptions tokenOptions,
        TimeProvider timeProvider)
    {
        _userDal = userDal;
        _tenantDal = tenantDal;
        _refreshTokenDal = refreshTokenDal;
        _tokenHelper = tokenHelper;
        _smsSender = smsSender;
        _emailSender = emailSender;
        _otp = new OtpChallenge(cache, otpOptions, timeProvider);
        _tokenOptions = tokenOptions;
        _timeProvider = timeProvider;
    }

    private DateTime UtcNow => _timeProvider.GetUtcNow().UtcDateTime;

    // ── OTP gönderimi ───────────────────────────────────────────────

    [ValidationAspect(typeof(SendOtpValidator))]
    public async Task<IResult> SendOtpAsync(SendOtpDto sendOtpDto)
    {
        var phone = PhoneNumberHelper.Normalize(sendOtpDto.PhoneNumber);

        // Hız sınırı numara kayıtlı olsun olmasın uygulanır; yanıt farkından numara keşfedilemez
        var throttle = _otp.CheckAndRegisterSendAttempt(phone);
        if (throttle != null) return throttle;

        var candidates = await _userDal.GetLoginCandidatesByPhoneAsync(phone);
        if (candidates.Count == 0)
        {
            return new SuccessResult(GenericSendMessage);
        }

        var code = _otp.Issue(OtpPurpose.Login, phone);

        var sent = await _smsSender.SendAsync(phone,
            $"Hafız Takip giriş kodunuz: {code}. Kod {_otp.ExpiryMinutes} dakika geçerlidir. Kimseyle paylaşmayın.");

        if (!sent)
        {
            _otp.Consume(OtpPurpose.Login, phone);
            return new ErrorResult("Doğrulama kodu şu anda gönderilemedi. Lütfen biraz sonra tekrar deneyin.");
        }

        return new SuccessResult(GenericSendMessage);
    }

    // ── OTP doğrulama ───────────────────────────────────────────────

    [ValidationAspect(typeof(VerifyOtpValidator))]
    public async Task<IDataResult<TokenResponseDto>> VerifyOtpAsync(VerifyOtpDto verifyOtpDto)
    {
        var phone = PhoneNumberHelper.Normalize(verifyOtpDto.PhoneNumber);

        switch (_otp.Verify(OtpPurpose.Login, phone, verifyOtpDto.Code))
        {
            case OtpVerification.TooManyAttempts:
                return Unauthorized<TokenResponseDto>("Çok fazla hatalı deneme yapıldı. Lütfen yeni kod isteyin.");
            case OtpVerification.InvalidOrExpired:
                return Unauthorized<TokenResponseDto>(InvalidCodeMessage);
        }

        var candidates = await _userDal.GetLoginCandidatesByPhoneAsync(phone);
        return await CompleteLoginAsync(candidates, verifyOtpDto.TenantId, () => _otp.Consume(OtpPurpose.Login, phone));
    }

    // ── E-posta ile giriş ───────────────────────────────────────────

    [ValidationAspect(typeof(SendEmailCodeValidator))]
    public async Task<IResult> SendEmailCodeAsync(SendEmailCodeDto sendEmailCodeDto)
    {
        var email = EmailHelper.Normalize(sendEmailCodeDto.Email);

        // Hız sınırı adres kayıtlı olsun olmasın uygulanır; yanıt farkından adres keşfedilemez
        var throttle = _otp.CheckAndRegisterSendAttempt(email);
        if (throttle != null) return throttle;

        var candidates = await _userDal.GetLoginCandidatesByEmailAsync(email);
        if (candidates.Count == 0)
        {
            return new SuccessResult(GenericEmailSendMessage);
        }

        var code = _otp.Issue(OtpPurpose.EmailLogin, email);

        var sent = await _emailSender.SendAsync(email,
            $"Hafız Takip giriş kodunuz: {code}",
            $"Hafız Takip giriş kodunuz: {code}\n\nKod {_otp.ExpiryMinutes} dakika geçerlidir. Kimseyle paylaşmayın.\n" +
            "Bu isteği siz yapmadıysanız bu e-postayı dikkate almayın.");

        if (!sent)
        {
            _otp.Consume(OtpPurpose.EmailLogin, email);
            return new ErrorResult("Doğrulama kodu şu anda gönderilemedi. Lütfen biraz sonra tekrar deneyin.");
        }

        return new SuccessResult(GenericEmailSendMessage);
    }

    [ValidationAspect(typeof(VerifyEmailCodeValidator))]
    public async Task<IDataResult<TokenResponseDto>> VerifyEmailCodeAsync(VerifyEmailCodeDto verifyEmailCodeDto)
    {
        var email = EmailHelper.Normalize(verifyEmailCodeDto.Email);

        switch (_otp.Verify(OtpPurpose.EmailLogin, email, verifyEmailCodeDto.Code))
        {
            case OtpVerification.TooManyAttempts:
                return Unauthorized<TokenResponseDto>("Çok fazla hatalı deneme yapıldı. Lütfen yeni kod isteyin.");
            case OtpVerification.InvalidOrExpired:
                return Unauthorized<TokenResponseDto>(InvalidCodeMessage);
        }

        var candidates = await _userDal.GetLoginCandidatesByEmailAsync(email);
        return await CompleteLoginAsync(candidates, verifyEmailCodeDto.TenantId, () => _otp.Consume(OtpPurpose.EmailLogin, email));
    }

    /// <summary>
    /// Kod doğrulandıktan sonra ortak adım: kurs seçimi gerekiyorsa liste döner (kod tüketilmez),
    /// aksi halde kod tüketilir ve oturum açılır.
    /// </summary>
    private async Task<IDataResult<TokenResponseDto>> CompleteLoginAsync(List<User> candidates, Guid? tenantId, Action consumeCode)
    {
        if (candidates.Count == 0)
        {
            consumeCode();
            return Unauthorized<TokenResponseDto>(InvalidCodeMessage);
        }

        User? user;
        if (tenantId is { } selectedTenantId)
        {
            user = candidates.FirstOrDefault(u => u.TenantId == selectedTenantId);
            if (user == null)
                return Unauthorized<TokenResponseDto>("Bu kurumda kaydınız bulunamadı.");
        }
        else if (candidates.Count > 1)
        {
            // Kod doğru ama hangi kursa giriş yapılacağı belli değil: kod tüketilmez, kurs listesi döner
            return new SuccessDataResult<TokenResponseDto>(
                new TokenResponseDto
                {
                    RequiresTenantSelection = true,
                    Tenants = await BuildTenantOptionsAsync(candidates)
                },
                "Birden fazla kurumda kaydınız var. Lütfen giriş yapacağınız kurumu seçin.");
        }
        else
        {
            user = candidates[0];
        }

        consumeCode();

        var response = await CreateSessionAsync(user);
        return new SuccessDataResult<TokenResponseDto>(response, "Giriş başarılı.");
    }

    // ── Refresh ─────────────────────────────────────────────────────

    [ValidationAspect(typeof(RefreshTokenRequestValidator))]
    public async Task<IDataResult<TokenResponseDto>> RefreshAsync(RefreshTokenRequestDto request)
    {
        var now = UtcNow;
        var stored = await _refreshTokenDal.GetByHashAcrossTenantsAsync(HashToken(request.RefreshToken));
        if (stored == null)
            return Unauthorized<TokenResponseDto>(InvalidSessionMessage);

        if (stored.RevokedAt != null)
        {
            // İptal edilmiş token tekrar kullanıldı: token çalınmış olabilir, tüm oturumları kapat
            await _refreshTokenDal.RevokeAllForUserAsync(stored.UserId, now);
            return Unauthorized<TokenResponseDto>(InvalidSessionMessage);
        }

        if (stored.ExpiresAt <= now)
            return Unauthorized<TokenResponseDto>(InvalidSessionMessage);

        var user = await _userDal.GetByIdAcrossTenantsAsync(stored.UserId);
        var tenant = user == null ? null : await _tenantDal.GetAsync(t => t.Id == user.TenantId);
        if (user == null || tenant == null || !TenantStatus.CanSignIn(tenant.Status, tenant.IsActive))
        {
            await _refreshTokenDal.RevokeAllForUserAsync(stored.UserId, now);
            return Unauthorized<TokenResponseDto>(InvalidSessionMessage);
        }

        var (newToken, replacement) = CreateRefreshToken(user, now);
        stored.RevokedAt = now;
        stored.ReplacedByTokenHash = replacement.TokenHash;
        stored.UpdatedAt = now;
        await _refreshTokenDal.RotateAsync(stored, replacement);

        var response = BuildResponse(user, tenant.Status, newToken, replacement);
        return new SuccessDataResult<TokenResponseDto>(response, "Oturum yenilendi.");
    }

    // ── Çıkış ───────────────────────────────────────────────────────

    [ValidationAspect(typeof(RefreshTokenRequestValidator))]
    public async Task<IResult> LogoutAsync(RefreshTokenRequestDto request)
    {
        var stored = await _refreshTokenDal.GetByHashAcrossTenantsAsync(HashToken(request.RefreshToken));
        if (stored != null && stored.RevokedAt == null)
        {
            stored.RevokedAt = UtcNow;
            stored.UpdatedAt = stored.RevokedAt;
            await _refreshTokenDal.UpdateAsync(stored);
        }

        // Token bulunamasa da sonuç aynıdır (idempotent çıkış)
        return new SuccessResult("Çıkış yapıldı.");
    }

    // ── Yardımcılar ─────────────────────────────────────────────────

    public async Task<TokenResponseDto> CreateSessionAsync(User user)
    {
        var tenant = await _tenantDal.GetAsync(t => t.Id == user.TenantId);
        var (token, entity) = CreateRefreshToken(user, UtcNow);
        await _refreshTokenDal.AddAsync(entity);
        return BuildResponse(user, tenant?.Status ?? string.Empty, token, entity);
    }

    private (string Token, RefreshToken Entity) CreateRefreshToken(User user, DateTime now)
    {
        var token = Base64UrlEncode(RandomNumberGenerator.GetBytes(64));
        var entity = new RefreshToken
        {
            TenantId = user.TenantId,
            UserId = user.Id,
            TokenHash = HashToken(token),
            ExpiresAt = now.AddDays(_tokenOptions.RefreshTokenExpirationDays),
            CreatedAt = now
        };
        return (token, entity);
    }

    private TokenResponseDto BuildResponse(User user, string tenantStatus, string refreshToken, RefreshToken refreshEntity)
    {
        var accessToken = _tokenHelper.CreateToken(
            new PlatformUser { Id = user.Id, Email = user.Email ?? string.Empty },
            new List<string> { user.Role },
            user.TenantId);

        return new TokenResponseDto
        {
            Token = accessToken.Token,
            Expiration = accessToken.Expiration,
            RefreshToken = refreshToken,
            RefreshTokenExpiration = refreshEntity.ExpiresAt,
            UserId = user.Id,
            TenantId = user.TenantId,
            TenantStatus = tenantStatus,
            Role = user.Role,
            FirstName = user.FirstName,
            LastName = user.LastName
        };
    }

    private async Task<List<TenantOptionDto>> BuildTenantOptionsAsync(List<User> candidates)
    {
        var tenantIds = candidates.Select(u => u.TenantId).Distinct().ToList();
        var tenants = await _tenantDal.GetListAsync(t => tenantIds.Contains(t.Id));
        return candidates
            .Select(u => new TenantOptionDto
            {
                TenantId = u.TenantId,
                TenantName = tenants.FirstOrDefault(t => t.Id == u.TenantId)?.Name ?? string.Empty,
                TenantStatus = tenants.FirstOrDefault(t => t.Id == u.TenantId)?.Status ?? string.Empty,
                Role = u.Role
            })
            .OrderBy(t => t.TenantName)
            .ToList();
    }

    private static string HashToken(string token) => Sha256Hex(token);

    private static string Sha256Hex(string value) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));

    private static string Base64UrlEncode(byte[] bytes) =>
        Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    private static ErrorDataResult<T> Unauthorized<T>(string message) =>
        new(message, ResultErrorType.Unauthorized);
}
