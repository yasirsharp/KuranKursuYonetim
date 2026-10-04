using HafizTakip.Core.Utilities.Requests;
using HafizTakip.Core.Utilities.Results;
using HafizTakip.Entities.Dtos;

namespace HafizTakip.Business.Abstract;

public interface IEtutNoteService
{
    Task<IDataResult<PagedResult<EtutNoteDto>>> GetAllAsync(PaginationParams paginationParams);
    Task<IDataResult<PagedResult<EtutNoteDto>>> GetByStudentIdAsync(Guid studentId, PaginationParams paginationParams);
    Task<IDataResult<EtutNoteDto>> GetByIdAsync(Guid id);
    Task<IResult> AddAsync(CreateEtutNoteDto createEtutNoteDto);
    Task<IResult> UpdateAsync(UpdateEtutNoteDto updateEtutNoteDto);
    Task<IResult> DeleteAsync(Guid id);
}
