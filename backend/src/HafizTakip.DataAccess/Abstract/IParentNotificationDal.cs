using HafizTakip.Core.DataAccess;
using HafizTakip.Core.Utilities.Requests;
using HafizTakip.Core.Utilities.Results;
using HafizTakip.Entities.Concrete;
using HafizTakip.Entities.Dtos;

namespace HafizTakip.DataAccess.Abstract;

public interface IParentNotificationDal : IEntityRepository<ParentNotification>
{
    Task<PagedResult<ParentNotificationDto>> GetNotificationDetailsPagedAsync(ParentNotificationFilter filter, PaginationParams paging);

    Task<ParentNotificationDto?> GetNotificationDetailAsync(Guid id);
}
