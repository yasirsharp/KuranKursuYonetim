using HafizTakip.Core.Utilities.Requests;
using HafizTakip.Core.Utilities.Results;
using HafizTakip.Entities.Dtos;

namespace HafizTakip.Business.Abstract;

public interface IExamResultService
{
    Task<IDataResult<PagedResult<ExamResultDto>>> GetByExamIdAsync(Guid examId, PaginationParams paginationParams);
    Task<IDataResult<PagedResult<ExamResultDto>>> GetByStudentIdAsync(Guid studentId, PaginationParams paginationParams);
    Task<IDataResult<ExamResultDto>> GetByIdAsync(Guid id);
    Task<IResult> AddAsync(CreateExamResultDto createExamResultDto);
    Task<IResult> UpdateAsync(UpdateExamResultDto updateExamResultDto);
    Task<IResult> DeleteAsync(Guid id);
}
