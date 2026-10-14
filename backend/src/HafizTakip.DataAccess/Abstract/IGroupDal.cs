using HafizTakip.Core.DataAccess;
using HafizTakip.Core.Utilities.Requests;
using HafizTakip.Core.Utilities.Results;
using HafizTakip.Entities.Concrete;
using HafizTakip.Entities.Dtos;

namespace HafizTakip.DataAccess.Abstract;

public interface IGroupDal : IEntityRepository<Group>
{
    Task<PagedResult<GroupDto>> GetGroupDetailsPagedAsync(PaginationParams paging);

    Task<GroupDto?> GetGroupDetailAsync(Guid id);
}
