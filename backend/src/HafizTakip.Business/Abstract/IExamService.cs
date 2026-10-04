using HafizTakip.Core.Utilities.Requests;
using HafizTakip.Core.Utilities.Results;
using HafizTakip.Entities.Dtos;

namespace HafizTakip.Business.Abstract;

public interface IExamService
{
    Task<IDataResult<PagedResult<ExamDto>>> GetAllAsync(PaginationParams paginationParams);
    Task<IDataResult<PagedResult<ExamDto>>> GetByGroupIdAsync(Guid groupId, PaginationParams paginationParams);
    Task<IDataResult<ExamDto>> GetByIdAsync(Guid id);
    Task<IResult> AddAsync(CreateExamDto createExamDto);
    Task<IResult> UpdateAsync(UpdateExamDto updateExamDto);
    Task<IResult> DeleteAsync(Guid id);
}
