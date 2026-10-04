using HafizTakip.Core.Utilities.Requests;
using HafizTakip.Core.Utilities.Results;
using HafizTakip.Entities.Dtos;

namespace HafizTakip.Business.Abstract;

public interface ISubscriptionService
{
    Task<IDataResult<PagedResult<SubscriptionDto>>> GetAllAsync(PaginationParams paginationParams);
    Task<IDataResult<SubscriptionDto>> GetByTenantIdAsync(Guid tenantId);
    Task<IDataResult<SubscriptionDto>> GetByIdAsync(Guid id);
    Task<IResult> AddAsync(CreateSubscriptionDto createSubscriptionDto);
    Task<IResult> UpdateAsync(UpdateSubscriptionDto updateSubscriptionDto);
    Task<IResult> DeleteAsync(Guid id);
}
