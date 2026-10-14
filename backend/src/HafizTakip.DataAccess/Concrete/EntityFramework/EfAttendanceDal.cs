using HafizTakip.Core.DataAccess.EntityFramework;
using HafizTakip.Core.Utilities.Requests;
using HafizTakip.Core.Utilities.Results;
using HafizTakip.DataAccess.Abstract;
using HafizTakip.Entities.Concrete;
using HafizTakip.Entities.Dtos;
using Microsoft.EntityFrameworkCore;

namespace HafizTakip.DataAccess.Concrete.EntityFramework;

public class EfAttendanceDal : EfEntityRepositoryBase<Attendance, HafizTakipDbContext>, IAttendanceDal
{
    public EfAttendanceDal(HafizTakipDbContext context) : base(context)
    {
    }

    public Task<PagedResult<AttendanceDto>> GetAttendanceDetailsPagedAsync(
        AttendanceFilter filter, IReadOnlyCollection<Guid>? scopeStudentIds, PaginationParams paging)
    {
        var attendances = Context.Attendances.AsNoTracking();

        if (scopeStudentIds != null)
            attendances = attendances.Where(a => scopeStudentIds.Contains(a.StudentId));
        if (filter.StudentId.HasValue)
            attendances = attendances.Where(a => a.StudentId == filter.StudentId);
        if (filter.From.HasValue)
        {
            var from = DateTime.SpecifyKind(filter.From.Value.Date, DateTimeKind.Utc);
            attendances = attendances.Where(a => a.Date >= from);
        }
        if (filter.To.HasValue)
        {
            var toExclusive = DateTime.SpecifyKind(filter.To.Value.Date.AddDays(1), DateTimeKind.Utc);
            attendances = attendances.Where(a => a.Date < toExclusive);
        }

        return Details(attendances.OrderByDescending(a => a.Date).ThenBy(a => a.StudentId))
            .ToPagedResultAsync(paging);
    }

    public Task<AttendanceDto?> GetAttendanceDetailAsync(Guid id) =>
        Details(Context.Attendances.AsNoTracking().Where(a => a.Id == id)).FirstOrDefaultAsync();

    private IQueryable<AttendanceDto> Details(IQueryable<Attendance> attendances) =>
        from a in attendances
        join s in Context.Students on a.StudentId equals s.Id into students
        from s in students.DefaultIfEmpty()
        select new AttendanceDto
        {
            Id = a.Id,
            TenantId = a.TenantId,
            StudentId = a.StudentId,
            StudentName = s != null ? s.FirstName + " " + s.LastName : QueryHelpers.Unknown,
            Date = a.Date,
            Status = a.Status,
            Notes = a.Notes ?? string.Empty
        };
}
