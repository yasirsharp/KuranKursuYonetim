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
}

public class TokenResponseDto : IDto
{
    public string Token { get; set; } = string.Empty;
    public DateTime Expiration { get; set; }
    public Guid TenantId { get; set; }
    public string Role { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
}
