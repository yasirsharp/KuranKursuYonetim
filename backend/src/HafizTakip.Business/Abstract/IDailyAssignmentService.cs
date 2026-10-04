using HafizTakip.Core.Utilities.Requests;
using HafizTakip.Core.Utilities.Results;
using HafizTakip.Entities.Dtos;

namespace HafizTakip.Business.Abstract;

public interface IDailyAssignmentService
{
    Task<IDataResult<PagedResult<DailyAssignmentDto>>> GetAllAsync(PaginationParams paginationParams);
    Task<IDataResult<PagedResult<DailyAssignmentDto>>> GetByStudentIdAsync(Guid studentId, PaginationParams paginationParams);
    Task<IDataResult<PagedResult<DailyAssignmentDto>>> GetEtutAssignmentsAsync(DateTime date, PaginationParams paginationParams);
    Task<IDataResult<DailyAssignmentDto>> GetByIdAsync(Guid id);
    
    Task<Core.Utilities.Results.IResult> AddAsync(CreateDailyAssignmentDto createDailyAssignmentDto);
    Task<Core.Utilities.Results.IResult> CompleteEtutAsync(CompleteEtutAssignmentDto dto);
    Task<Core.Utilities.Results.IResult> CompleteHomeAsync(CompleteHomeAssignmentDto dto);
}
