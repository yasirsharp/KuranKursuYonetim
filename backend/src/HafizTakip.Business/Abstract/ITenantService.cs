using HafizTakip.Core.Utilities.Requests;
using HafizTakip.Core.Utilities.Results;
using HafizTakip.Entities.Dtos;

namespace HafizTakip.Business.Abstract;

public interface ITenantService
{
    Task<IDataResult<PagedResult<TenantDto>>> GetAllAsync(TenantFilterDto filter, PaginationParams paginationParams);
    Task<IDataResult<TenantDto>> GetByIdAsync(Guid id);
    Task<IResult> AddAsync(CreateTenantDto createTenantDto);
    Task<IResult> UpdateAsync(UpdateTenantDto updateTenantDto);
    Task<IResult> DeleteAsync(Guid id);

    /// <summary>Onay bekleyen kurs başvurusunu onaylar; kurs yöneticisine SMS gider.</summary>
    Task<IResult> ApproveAsync(Guid id);

    /// <summary>Onay bekleyen kurs başvurusunu gerekçesiyle reddeder; kurs yöneticisine SMS gider.</summary>
    Task<IResult> RejectAsync(Guid id, RejectCourseApplicationDto dto);
}
