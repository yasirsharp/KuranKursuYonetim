using HafizTakip.Core.Utilities.Requests;
using HafizTakip.Core.Utilities.Results;
using HafizTakip.Entities.Dtos;

namespace HafizTakip.Business.Abstract;

public interface IParentNotificationService
{
    Task<IDataResult<PagedResult<ParentNotificationDto>>> GetAllAsync(PaginationParams paginationParams);
    Task<IDataResult<PagedResult<ParentNotificationDto>>> GetByStudentIdAsync(Guid studentId, PaginationParams paginationParams);
    Task<IDataResult<ParentNotificationDto>> GetByIdAsync(Guid id);
    Task<IResult> AddAsync(CreateParentNotificationDto createParentNotificationDto);
    Task<IResult> SendNotificationAsync(Guid id);
}
