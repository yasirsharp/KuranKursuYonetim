using HafizTakip.Core.Utilities.Requests;
using HafizTakip.Core.Utilities.Results;
using HafizTakip.Entities.Dtos;

namespace HafizTakip.Business.Abstract;

public interface ITenantService
{
    Task<IDataResult<PagedResult<TenantDto>>> GetAllAsync(PaginationParams paginationParams);
    Task<IDataResult<TenantDto>> GetByIdAsync(Guid id);
    Task<IResult> AddAsync(CreateTenantDto createTenantDto);
    Task<IResult> UpdateAsync(UpdateTenantDto updateTenantDto);
    Task<IResult> DeleteAsync(Guid id);
}
