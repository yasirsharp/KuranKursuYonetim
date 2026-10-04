using System;
using System.Threading.Tasks;
using HafizTakip.Core.Utilities.Requests;
using HafizTakip.Core.Utilities.Results;
using HafizTakip.Entities.Dtos;

namespace HafizTakip.Business.Abstract;

public interface ILessonService
{
    Task<IDataResult<PagedResult<LessonDto>>> GetAllAsync(PaginationParams paginationParams);
    Task<IDataResult<PagedResult<LessonDto>>> GetByStudentIdAsync(Guid studentId, PaginationParams paginationParams);
    Task<IDataResult<LessonDto>> GetByIdAsync(Guid id);
    Task<IResult> AddAsync(CreateLessonDto createLessonDto);
    Task<IResult> UpdateAsync(UpdateLessonDto updateLessonDto);
    Task<IResult> DeleteAsync(Guid id);
}
