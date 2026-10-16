using HafizTakip.Business.Abstract;
using HafizTakip.Business.ValidationRules.FluentValidation;
using HafizTakip.Core.Aspects.Autofac.Validation;
using HafizTakip.Core.Enums;
using HafizTakip.Core.Utilities.Results;
using HafizTakip.Core.Utilities.Security.Hashing;
using HafizTakip.Core.Utilities.Security.Jwt;
using HafizTakip.DataAccess.Abstract;
using HafizTakip.Entities.Dtos;
using Microsoft.Extensions.Caching.Memory;

namespace HafizTakip.Business.Concrete;

/// <summary>
/// Platform yöneticisi girişi (e-posta + şifre). Kurs kullanıcılarından ayrıdır; token'da tenant bilgisi yoktur.
/// Güvenlik notları:
/// - Hesap bulunamasa da parola özeti hesaplanır; yanıt süresinden e-posta keşfedilemez.
/// - E-posta başına art arda hatalı denemeler geçici olarak kilitlenir (IP sınırına ek olarak).
/// </summary>
public class PlatformAuthService : IPlatformAuthService
{
    public const int MaxFailedAttempts = 5;
    public static readonly TimeSpan LockoutDuration = TimeSpan.FromMinutes(15);

    private const string InvalidCredentialsMessage = "E-posta veya şifre hatalı.";

    // Var olmayan hesaplar için zamanlama eşitleyici (gerçek bir parolayla eşleşmez)
    private static readonly string DummyHash = PasswordHasher.Hash(Guid.NewGuid().ToString("N"));

    private readonly IPlatformUserDal _platformUserDal;
    private readonly ITokenHelper _tokenHelper;
    private readonly IMemoryCache _cache;

    public PlatformAuthService(IPlatformUserDal platformUserDal, ITokenHelper tokenHelper, IMemoryCache cache)
    {
        _platformUserDal = platformUserDal;
        _tokenHelper = tokenHelper;
        _cache = cache;
    }

    [ValidationAspect(typeof(PlatformLoginValidator))]
    public async Task<IDataResult<PlatformTokenDto>> LoginAsync(PlatformLoginDto dto)
    {
        var email = dto.Email.Trim().ToLowerInvariant();
        var failKey = $"platform-login:fail:{email}";

        if (_cache.TryGetValue(failKey, out FailCounter? failures) && failures!.Count >= MaxFailedAttempts)
        {
            return new ErrorDataResult<PlatformTokenDto>(
                $"Çok fazla hatalı deneme yapıldı. {LockoutDuration.TotalMinutes:0} dakika sonra tekrar deneyin.",
                ResultErrorType.TooManyRequests);
        }

        var user = await _platformUserDal.GetByEmailAsync(email);
        var passwordOk = PasswordHasher.Verify(dto.Password, user?.PasswordHash ?? DummyHash);

        if (user == null || !passwordOk || user.Role != UserRole.SuperAdmin)
        {
            var counter = _cache.GetOrCreate(failKey, e =>
            {
                e.AbsoluteExpirationRelativeToNow = LockoutDuration;
                return new FailCounter();
            })!;
            counter.Count++;
            return new ErrorDataResult<PlatformTokenDto>(InvalidCredentialsMessage, ResultErrorType.Unauthorized);
        }

        _cache.Remove(failKey);

        var token = _tokenHelper.CreateToken(user, new List<string> { user.Role }, Guid.Empty);
        return new SuccessDataResult<PlatformTokenDto>(new PlatformTokenDto
        {
            Token = token.Token,
            Expiration = token.Expiration,
            UserId = user.Id,
            Email = user.Email,
            Role = user.Role
        }, "Giriş başarılı.");
    }

    private sealed class FailCounter
    {
        public int Count { get; set; }
    }
}
