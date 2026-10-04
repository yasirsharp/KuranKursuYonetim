using HafizTakip.Core.Utilities.Results;
using HafizTakip.Entities.Dtos;

namespace HafizTakip.Business.Abstract;

public interface IStudentParentService
{
    Task<IDataResult<List<StudentParentDto>>> GetParentsByStudentIdAsync(Guid studentId);
    Task<IDataResult<List<StudentParentDto>>> GetStudentsByParentIdAsync(Guid parentId);
    Task<IResult> AddAsync(CreateStudentParentDto dto);
    Task<IResult> DeleteAsync(Guid id);
}
