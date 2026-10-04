using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using HafizTakip.Core.Utilities.Requests;
using HafizTakip.Core.Utilities.Results;
using HafizTakip.Entities.Dtos;

namespace HafizTakip.Business.Abstract;

public interface IAttendanceService
{
    Task<IDataResult<PagedResult<AttendanceDto>>> GetAllAsync(PaginationParams paginationParams);
    Task<IDataResult<PagedResult<AttendanceDto>>> GetByStudentIdAsync(Guid studentId, PaginationParams paginationParams);
    Task<IDataResult<AttendanceDto>> GetByIdAsync(Guid id);
    Task<IResult> AddAsync(CreateAttendanceDto createAttendanceDto);
    Task<IResult> AddBulkAsync(BulkAttendanceDto bulkAttendanceDto);
    Task<IResult> UpdateAsync(AttendanceDto updateAttendanceDto); // Şimdilik basit DTO
    Task<IResult> DeleteAsync(Guid id);
}
