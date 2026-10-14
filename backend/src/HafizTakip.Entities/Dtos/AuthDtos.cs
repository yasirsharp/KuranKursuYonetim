using HafizTakip.Core.Entities;

namespace HafizTakip.Entities.Dtos;

public class SendOtpDto : IDto
{
    public string PhoneNumber { get; set; } = string.Empty;
}

public class VerifyOtpDto : IDto
{
    public string PhoneNumber { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// Numara birden fazla kursta kayıtlıysa ilk doğrulama kurs listesi döner;
    /// istemci seçilen kursla aynı kodu tekrar gönderir.
    /// </summary>
    public Guid? TenantId { get; set; }
}

public class RefreshTokenRequestDto : IDto
{
    public string RefreshToken { get; set; } = string.Empty;
}

public class TenantOptionDto : IDto
{
    public Guid TenantId { get; set; }
    public string TenantName { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
}

public class TokenResponseDto : IDto
{
    public string Token { get; set; } = string.Empty;
    public DateTime Expiration { get; set; }
    public string RefreshToken { get; set; } = string.Empty;
    public DateTime RefreshTokenExpiration { get; set; }

    public Guid UserId { get; set; }
    public Guid TenantId { get; set; }
    public string Role { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;

    /// <summary>true ise token üretilmemiştir; istemci Tenants içinden seçim yapıp verify-otp'yi TenantId ile tekrarlar.</summary>
    public bool RequiresTenantSelection { get; set; }
    public List<TenantOptionDto> Tenants { get; set; } = new();
}
