using System.Security.Cryptography;
using System.Text;
using HafizTakip.Business.Abstract;
using HafizTakip.Business.ValidationRules.FluentValidation;
using HafizTakip.Core.Aspects.Autofac.Validation;
using HafizTakip.Core.Entities.Platform;
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
/// Telefon + OTP ile giriş, refresh token rotasyonu ve çıkış.
/// Güvenlik notları:
/// - OTP kodu cache'te yalnızca SHA-256 özeti olarak tutulur; hatalı deneme sayısı sınırlıdır.
/// - Numara kayıtlı olsun olmasın send-otp aynı yanıtı döner (numara keşfi engellenir).
/// - Refresh token'ın kendisi saklanmaz; her kullanımda döndürülür, iptal edilmiş bir token
///   tekrar gelirse kullanıcının tüm oturumları kapatılır.
/// </summary>
public class AuthService : IAuthService
{
    private const string GenericSendMessage = "Telefon numarası kayıtlıysa doğrulama kodu gönderildi.";
    private const string InvalidCodeMessage = "Doğrulama kodu hatalı veya süresi dolmuş.";
    private const string InvalidSessionMessage = "Oturum geçersiz veya süresi dolmuş. Lütfen tekrar giriş yapın.";

    private readonly IUserDal _userDal;
    private readonly ITenantDal _tenantDal;
    private readonly IRefreshTokenDal _refreshTokenDal;
    private readonly ITokenHelper _tokenHelper;
    private readonly IMemoryCache _cache;
    private readonly ISmsSender _smsSender;
    private readonly OtpOptions _otpOptions;
    private readonly TokenOptions _tokenOptions;
    private readonly TimeProvider _timeProvider;

    public AuthService(
        IUserDal userDal,
        ITenantDal tenantDal,
        IRefreshTokenDal refreshTokenDal,
        ITokenHelper tokenHelper,
        IMemoryCache cache,
        ISmsSender smsSender,
        OtpOptions otpOptions,
        TokenOptions tokenOptions,
        TimeProvider timeProvider)
    {
        _userDal = userDal;
        _tenantDal = tenantDal;
        _refreshTokenDal = refreshTokenDal;
        _tokenHelper = tokenHelper;
        _cache = cache;
        _smsSender = smsSender;
        _otpOptions = otpOptions;
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
        var throttle = CheckAndRegisterSendAttempt(phone);
        if (throttle != null) return throttle;

        var candidates = await _userDal.GetLoginCandidatesByPhoneAsync(phone);
        if (candidates.Count == 0)
        {
            return new SuccessResult(GenericSendMessage);
        }

        var code = GenerateCode();
        _cache.Set(OtpKey(phone), new OtpEntry(HashCode(phone, code)), TimeSpan.FromMinutes(_otpOptions.ExpiryMinutes));

        var sent = await _smsSender.SendAsync(phone,
            $"Hafız Takip giriş kodunuz: {code}. Kod {_otpOptions.ExpiryMinutes} dakika geçerlidir. Kimseyle paylaşmayın.");

        if (!sent)
        {
            _cache.Remove(OtpKey(phone));
            return new ErrorResult("Doğrulama kodu şu anda gönderilemedi. Lütfen biraz sonra tekrar deneyin.");
        }

        return new SuccessResult(GenericSendMessage);
    }

    // ── OTP doğrulama ───────────────────────────────────────────────

    [ValidationAspect(typeof(VerifyOtpValidator))]
    public async Task<IDataResult<TokenResponseDto>> VerifyOtpAsync(VerifyOtpDto verifyOtpDto)
    {
        var phone = PhoneNumberHelper.Normalize(verifyOtpDto.PhoneNumber);

        if (!_cache.TryGetValue(OtpKey(phone), out OtpEntry? entry) || entry == null)
        {
            return Unauthorized<TokenResponseDto>(InvalidCodeMessage);
        }

        if (entry.FailedAttempts >= _otpOptions.MaxVerifyAttempts)
        {
            _cache.Remove(OtpKey(phone));
            return Unauthorized<TokenResponseDto>("Çok fazla hatalı deneme yapıldı. Lütfen yeni kod isteyin.");
        }

        if (!CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(entry.CodeHash),
                Encoding.UTF8.GetBytes(HashCode(phone, verifyOtpDto.Code))))
        {
            entry.FailedAttempts++;
            return Unauthorized<TokenResponseDto>(InvalidCodeMessage);
        }

        var candidates = await _userDal.GetLoginCandidatesByPhoneAsync(phone);
        if (candidates.Count == 0)
        {
            _cache.Remove(OtpKey(phone));
            return Unauthorized<TokenResponseDto>(InvalidCodeMessage);
        }

        User? user;
        if (verifyOtpDto.TenantId is { } tenantId)
        {
            user = candidates.FirstOrDefault(u => u.TenantId == tenantId);
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

        _cache.Remove(OtpKey(phone));

        var response = await IssueTokensAsync(user);
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
        if (user == null || tenant == null || !tenant.IsActive)
        {
            await _refreshTokenDal.RevokeAllForUserAsync(stored.UserId, now);
            return Unauthorized<TokenResponseDto>(InvalidSessionMessage);
        }

        var (newToken, replacement) = CreateRefreshToken(user, now);
        stored.RevokedAt = now;
        stored.ReplacedByTokenHash = replacement.TokenHash;
        stored.UpdatedAt = now;
        await _refreshTokenDal.RotateAsync(stored, replacement);

        var response = BuildResponse(user, newToken, replacement);
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

    private async Task<TokenResponseDto> IssueTokensAsync(User user)
    {
        var (token, entity) = CreateRefreshToken(user, UtcNow);
        await _refreshTokenDal.AddAsync(entity);
        return BuildResponse(user, token, entity);
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

    private TokenResponseDto BuildResponse(User user, string refreshToken, RefreshToken refreshEntity)
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
                Role = u.Role
            })
            .OrderBy(t => t.TenantName)
            .ToList();
    }

    /// <summary>Bekleme süresi veya saatlik limit aşıldıysa hata, aksi halde null döner ve denemeyi kaydeder.</summary>
    private IResult? CheckAndRegisterSendAttempt(string phone)
    {
        var cooldownKey = $"otp:cooldown:{phone}";
        if (_cache.TryGetValue(cooldownKey, out DateTimeOffset retryAt))
        {
            var seconds = Math.Max(1, (int)Math.Ceiling((retryAt - _timeProvider.GetUtcNow()).TotalSeconds));
            return new ErrorResult($"Yeni kod istemek için {seconds} saniye bekleyin.", ResultErrorType.TooManyRequests);
        }

        var hourlyKey = $"otp:hourly:{phone}";
        var counter = _cache.GetOrCreate(hourlyKey, e =>
        {
            e.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1);
            return new SendCounter();
        })!;

        if (counter.Count >= _otpOptions.MaxSendsPerHour)
        {
            return new ErrorResult("Bu numara için çok fazla kod istendi. Lütfen daha sonra tekrar deneyin.", ResultErrorType.TooManyRequests);
        }

        counter.Count++;
        if (_otpOptions.ResendCooldownSeconds > 0)
        {
            var cooldown = TimeSpan.FromSeconds(_otpOptions.ResendCooldownSeconds);
            _cache.Set(cooldownKey, _timeProvider.GetUtcNow().Add(cooldown), cooldown);
        }
        return null;
    }

    private string GenerateCode()
    {
        if (!string.IsNullOrEmpty(_otpOptions.FixedCode))
            return _otpOptions.FixedCode;

        var max = (int)Math.Pow(10, _otpOptions.CodeLength);
        return RandomNumberGenerator.GetInt32(0, max).ToString(new string('0', _otpOptions.CodeLength));
    }

    private static string OtpKey(string phone) => $"otp:code:{phone}";

    private static string HashCode(string phone, string code) => Sha256Hex($"{phone}:{code}");

    private static string HashToken(string token) => Sha256Hex(token);

    private static string Sha256Hex(string value) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));

    private static string Base64UrlEncode(byte[] bytes) =>
        Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    private static ErrorDataResult<T> Unauthorized<T>(string message) =>
        new(message, ResultErrorType.Unauthorized);

    private sealed class OtpEntry
    {
        public OtpEntry(string codeHash) => CodeHash = codeHash;
        public string CodeHash { get; }
        public int FailedAttempts { get; set; }
    }

    private sealed class SendCounter
    {
        public int Count { get; set; }
    }
}
