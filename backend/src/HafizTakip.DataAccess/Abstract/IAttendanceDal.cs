using HafizTakip.Core.DataAccess;
using HafizTakip.Core.Utilities.Requests;
using HafizTakip.Core.Utilities.Results;
using HafizTakip.Entities.Concrete;
using HafizTakip.Entities.Dtos;

namespace HafizTakip.DataAccess.Abstract;

public interface IAttendanceDal : IEntityRepository<Attendance>
{
    /// <summary>Öğrenci adıyla, tarihe göre azalan, veritabanında sayfalanmış yoklama listesi.</summary>
    Task<PagedResult<AttendanceDto>> GetAttendanceDetailsPagedAsync(AttendanceFilter filter, IReadOnlyCollection<Guid>? scopeStudentIds, PaginationParams paging);

    Task<AttendanceDto?> GetAttendanceDetailAsync(Guid id);
}
