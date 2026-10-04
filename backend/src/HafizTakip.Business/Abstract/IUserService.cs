using HafizTakip.Core.Utilities.Requests;
using HafizTakip.Core.Utilities.Results;
using HafizTakip.Entities.Dtos;

namespace HafizTakip.Business.Abstract;

public interface IUserService
{
    Task<IDataResult<PagedResult<UserDto>>> GetAllAsync(PaginationParams paginationParams);
    Task<IDataResult<UserDto>> GetByIdAsync(Guid id);
    Task<IResult> AddAsync(CreateUserDto createUserDto);
    Task<IResult> UpdateAsync(UpdateUserDto updateUserDto);
    Task<IResult> DeleteAsync(Guid id);
}
