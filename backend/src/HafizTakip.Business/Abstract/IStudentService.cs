using HafizTakip.Core.Utilities.Requests;
using HafizTakip.Core.Utilities.Results;
using HafizTakip.Entities.Dtos;

namespace HafizTakip.Business.Abstract;

public interface IStudentService
{
    Task<IDataResult<PagedResult<StudentDto>>> GetAllAsync(PaginationParams paginationParams);
    Task<IDataResult<StudentDto>> GetByIdAsync(Guid id);
    Task<IResult> AddAsync(CreateStudentDto createStudentDto);
    Task<IResult> UpdateAsync(UpdateStudentDto updateStudentDto);
    Task<IResult> DeleteAsync(Guid id);
}
