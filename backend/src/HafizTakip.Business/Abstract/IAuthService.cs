using HafizTakip.Core.Utilities.Results;
using HafizTakip.Entities.Concrete;
using HafizTakip.Entities.Dtos;

namespace HafizTakip.Business.Abstract;

public interface IAuthService
{
    Task<IResult> SendOtpAsync(SendOtpDto sendOtpDto);
    Task<IDataResult<TokenResponseDto>> VerifyOtpAsync(VerifyOtpDto verifyOtpDto);
    Task<IResult> SendEmailCodeAsync(SendEmailCodeDto sendEmailCodeDto);
    Task<IDataResult<TokenResponseDto>> VerifyEmailCodeAsync(VerifyEmailCodeDto verifyEmailCodeDto);
    Task<IDataResult<TokenResponseDto>> RefreshAsync(RefreshTokenRequestDto request);
    Task<IResult> LogoutAsync(RefreshTokenRequestDto request);

    /// <summary>
    /// Kimliği başka bir akışta doğrulanmış kullanıcı için oturum (access + refresh token) açar.
    /// Yalnızca iş katmanından çağrılır (ör. telefonu doğrulanmış kurs başvurusu); HTTP'ye açılmaz.
    /// </summary>
    Task<TokenResponseDto> CreateSessionAsync(User user);
}
