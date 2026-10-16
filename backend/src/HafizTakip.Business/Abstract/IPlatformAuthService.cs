using HafizTakip.Core.Utilities.Results;
using HafizTakip.Entities.Dtos;

namespace HafizTakip.Business.Abstract;

public interface IPlatformAuthService
{
    Task<IDataResult<PlatformTokenDto>> LoginAsync(PlatformLoginDto dto);
}
