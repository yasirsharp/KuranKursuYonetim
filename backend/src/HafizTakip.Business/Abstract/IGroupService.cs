using HafizTakip.Core.Utilities.Requests;
using HafizTakip.Core.Utilities.Results;
using HafizTakip.Entities.Dtos;

namespace HafizTakip.Business.Abstract;

public interface IGroupService
{
    Task<IDataResult<PagedResult<GroupDto>>> GetAllAsync(PaginationParams paginationParams);
    Task<IDataResult<GroupDto>> GetByIdAsync(Guid id);
    Task<IResult> AddAsync(CreateGroupDto createGroupDto);
    Task<IResult> UpdateAsync(UpdateGroupDto updateGroupDto);
    Task<IResult> DeleteAsync(Guid id);
}
