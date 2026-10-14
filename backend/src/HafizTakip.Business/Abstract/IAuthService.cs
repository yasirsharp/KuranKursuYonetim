using HafizTakip.Core.Utilities.Results;
using HafizTakip.Entities.Dtos;

namespace HafizTakip.Business.Abstract;

public interface IAuthService
{
    Task<IResult> SendOtpAsync(SendOtpDto sendOtpDto);
    Task<IDataResult<TokenResponseDto>> VerifyOtpAsync(VerifyOtpDto verifyOtpDto);
    Task<IDataResult<TokenResponseDto>> RefreshAsync(RefreshTokenRequestDto request);
    Task<IResult> LogoutAsync(RefreshTokenRequestDto request);
}
