using System.Text.Json;
using HafizTakip.Core.DataAccess.EntityFramework;
using HafizTakip.Core.Utilities.Requests;
using HafizTakip.Core.Utilities.Results;
using HafizTakip.DataAccess.Abstract;
using HafizTakip.Entities.Concrete;
using HafizTakip.Entities.Dtos;
using Microsoft.EntityFrameworkCore;

namespace HafizTakip.DataAccess.Concrete.EntityFramework;

public class EfDailyAssignmentDal : EfEntityRepositoryBase<DailyAssignment, HafizTakipDbContext>, IDailyAssignmentDal
{
    public EfDailyAssignmentDal(HafizTakipDbContext context) : base(context)
    {
    }

    public Task<PagedResult<DailyAssignmentDto>> GetAssignmentDetailsPagedAsync(
        DailyAssignmentFilter filter, IReadOnlyCollection<Guid>? scopeStudentIds, PaginationParams paging)
    {
        var assignments = Context.DailyAssignments.AsNoTracking();

        if (scopeStudentIds != null)
            assignments = assignments.Where(a => scopeStudentIds.Contains(a.StudentId));
        if (filter.StudentId.HasValue)
            assignments = assignments.Where(a => a.StudentId == filter.StudentId);
        if (filter.Date.HasValue)
        {
            var day = DateTime.SpecifyKind(filter.Date.Value.Date, DateTimeKind.Utc);
            var nextDay = day.AddDays(1);
            assignments = assignments.Where(a => a.AssignmentDate >= day && a.AssignmentDate < nextDay);
        }
        if (filter.IsBoarderToday.HasValue)
            assignments = assignments.Where(a => a.IsBoarderToday == filter.IsBoarderToday);
        if (!string.IsNullOrWhiteSpace(filter.Status))
            assignments = assignments.Where(a => a.Status == filter.Status);

        return Rows(assignments.OrderByDescending(a => a.AssignmentDate).ThenByDescending(a => a.CreatedAt))
            .ToPagedResultAsync(paging, ToDto);
    }

    public async Task<DailyAssignmentDto?> GetAssignmentDetailAsync(Guid id)
    {
        var row = await Rows(Context.DailyAssignments.AsNoTracking().Where(a => a.Id == id)).FirstOrDefaultAsync();
        return row == null ? null : ToDto(row);
    }

    /// <summary>Adlar SQL'de join ile alınır; JSON görev listeleri sayfalamadan sonra bellekte çözülür.</summary>
    private IQueryable<AssignmentRow> Rows(IQueryable<DailyAssignment> assignments) =>
        from a in assignments
        join s in Context.Students on a.StudentId equals s.Id into students
        from s in students.DefaultIfEmpty()
        join u in Context.Users on a.TeacherId equals u.Id into teachers
        from u in teachers.DefaultIfEmpty()
        select new AssignmentRow(
            a,
            s != null ? s.FirstName + " " + s.LastName : QueryHelpers.Unknown,
            u != null ? u.FirstName + " " + u.LastName : QueryHelpers.Unknown);

    private static DailyAssignmentDto ToDto(AssignmentRow row) => new()
    {
        Id = row.Assignment.Id,
        TenantId = row.Assignment.TenantId,
        StudentId = row.Assignment.StudentId,
        StudentName = row.StudentName,
        TeacherId = row.Assignment.TeacherId,
        TeacherName = row.TeacherName,
        AssignmentDate = row.Assignment.AssignmentDate,
        IsBoarderToday = row.Assignment.IsBoarderToday,
        AssignedTasks = ParseList<AssignmentTaskItem>(row.Assignment.AssignedTasks),
        CompletedInEtut = ParseList<string>(row.Assignment.CompletedInEtut),
        CompletedAtHome = ParseList<string>(row.Assignment.CompletedAtHome),
        Status = row.Assignment.Status,
        EtutTeacherNote = row.Assignment.EtutTeacherNote,
        ParentNote = row.Assignment.ParentNote
    };

    private static List<T> ParseList<T>(string? json) =>
        string.IsNullOrWhiteSpace(json) ? new() : JsonSerializer.Deserialize<List<T>>(json) ?? new();

    private sealed record AssignmentRow(DailyAssignment Assignment, string StudentName, string TeacherName);
}
