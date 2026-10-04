using HafizTakip.Business.Abstract;
using HafizTakip.Core.Utilities.Security.Jwt;
using HafizTakip.DataAccess.Concrete.EntityFramework;
using HafizTakip.Entities.Dtos;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using HafizTakip.Core.Entities.Platform;
using System.Security.Authentication;

namespace HafizTakip.Business.Concrete;

public class AuthService : IAuthService
{
    private readonly HafizTakipDbContext _dbContext;
    private readonly ITokenHelper _tokenHelper;
    private readonly IMemoryCache _memoryCache;

    public AuthService(HafizTakipDbContext dbContext, ITokenHelper tokenHelper, IMemoryCache memoryCache)
    {
        _dbContext = dbContext;
        _tokenHelper = tokenHelper;
        _memoryCache = memoryCache;
    }

    public async Task SendOtpAsync(SendOtpDto sendOtpDto)
    {
        var phone = sendOtpDto.PhoneNumber.Trim().Replace(" ", "");
        if (phone.StartsWith("0"))
        {
            phone = phone.Substring(1);
        }
        
        // TODO: Telefon tenant başına benzersiz; tenant filtresiz arama aynı numara birden fazla kursta varsa belirsiz kalır. Girişte tenant seçimi veya global benzersizlik belirle.
        var user = await _dbContext.Users
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.PhoneNumber == phone && !u.IsDeleted);
        
        if (user == null)
        {
            throw new Exception("Bu telefon numarasina ait kullanici bulunamadi.");
        }

        // TODO: Sabit OTP'yi yalnızca geliştirme ortamında tut; üretimde rastgele/tek kullanımlık kodu SMS sağlayıcısıyla ilet, deneme ve istek hızını sınırla, kodu loglama.
        string otpCode = "123456";
        
        _memoryCache.Set($"OTP_{phone}", otpCode, TimeSpan.FromMinutes(3));
        
        // TODO: OTP kodunu ve telefon numarasını stdout'a yazma; teslimat sonucunu güvenli biçimde kaydet.
        Console.WriteLine($"[AUTH] OTP gonderildi -> {phone} -> Kod: {otpCode}");
    }

    public async Task<TokenResponseDto> VerifyOtpAsync(VerifyOtpDto verifyOtpDto)
    {
        var phone = verifyOtpDto.PhoneNumber.Trim().Replace(" ", "");
        if (phone.StartsWith("0"))
        {
            phone = phone.Substring(1);
        }
        
        if (!_memoryCache.TryGetValue($"OTP_{phone}", out string? cachedOtp))
        {
            throw new AuthenticationException("OTP kodunun suresi dolmus veya gecersiz.");
        }
        
        if (cachedOtp != verifyOtpDto.Code)
        {
            throw new AuthenticationException("Hatali OTP kodu.");
        }

        // TODO: PlatformUsers için ayrı kimlik doğrulama akışı ekle; şu an yalnızca tenant Users kayıtları giriş yapabiliyor.
        var user = await _dbContext.Users
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.PhoneNumber == phone && !u.IsDeleted);
        
        if (user == null)
        {
            throw new AuthenticationException("Kullanici bulunamadi.");
        }

        var platformUser = new PlatformUser
        {
            Id = user.Id,
            Email = user.Email ?? string.Empty
        };
        
        var roles = new List<string> { user.Role };
        var accessToken = _tokenHelper.CreateToken(platformUser, roles, user.TenantId);
        
        _memoryCache.Remove($"OTP_{phone}");

        return new TokenResponseDto
        {
            Token = accessToken.Token,
            Expiration = accessToken.Expiration,
            TenantId = user.TenantId,
            Role = user.Role,
            FirstName = user.FirstName,
            LastName = user.LastName
        };
    }
}
