using HafizTakip.Entities.Dtos;

namespace HafizTakip.Business.Abstract;

public interface IAuthService
{
    Task SendOtpAsync(SendOtpDto sendOtpDto);
    Task<TokenResponseDto> VerifyOtpAsync(VerifyOtpDto verifyOtpDto);
}
