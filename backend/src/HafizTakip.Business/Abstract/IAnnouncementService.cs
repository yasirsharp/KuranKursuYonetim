using HafizTakip.Core.Utilities.Requests;
using HafizTakip.Core.Utilities.Results;
using HafizTakip.Entities.Dtos;

namespace HafizTakip.Business.Abstract;

public interface IAnnouncementService
{
    Task<IDataResult<PagedResult<AnnouncementDto>>> GetAllAsync(PaginationParams paginationParams);
    Task<IDataResult<AnnouncementDto>> GetByIdAsync(Guid id);
    Task<IResult> AddAsync(CreateAnnouncementDto createAnnouncementDto);
    Task<IResult> UpdateAsync(UpdateAnnouncementDto updateAnnouncementDto);
    Task<IResult> DeleteAsync(Guid id);
}
