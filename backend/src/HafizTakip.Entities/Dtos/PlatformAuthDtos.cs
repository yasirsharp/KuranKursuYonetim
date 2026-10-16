using HafizTakip.Core.Entities;

namespace HafizTakip.Entities.Dtos;

public class PlatformLoginDto : IDto
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

/// <summary>Platform yöneticisi oturumu: yalnızca access token (refresh yok; süre dolunca yeniden giriş).</summary>
public class PlatformTokenDto : IDto
{
    public string Token { get; set; } = string.Empty;
    public DateTime Expiration { get; set; }
    public Guid UserId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
}
