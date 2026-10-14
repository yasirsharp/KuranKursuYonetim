using HafizTakip.Core.DataAccess.EntityFramework;
using HafizTakip.Core.Utilities.Requests;
using HafizTakip.Core.Utilities.Results;
using HafizTakip.DataAccess.Abstract;
using HafizTakip.Entities.Concrete;
using HafizTakip.Entities.Dtos;
using Microsoft.EntityFrameworkCore;

namespace HafizTakip.DataAccess.Concrete.EntityFramework;

public class EfLessonDal : EfEntityRepositoryBase<Lesson, HafizTakipDbContext>, ILessonDal
{
    public EfLessonDal(HafizTakipDbContext context) : base(context)
    {
    }

    public Task<PagedResult<LessonDto>> GetLessonDetailsPagedAsync(
        LessonFilter filter, IReadOnlyCollection<Guid>? scopeStudentIds, PaginationParams paging)
    {
        var lessons = Context.Lessons.AsNoTracking();

        if (scopeStudentIds != null)
            lessons = lessons.Where(l => scopeStudentIds.Contains(l.StudentId));
        if (filter.StudentId.HasValue)
            lessons = lessons.Where(l => l.StudentId == filter.StudentId);
        if (filter.From.HasValue)
        {
            var from = DateTime.SpecifyKind(filter.From.Value.Date, DateTimeKind.Utc);
            lessons = lessons.Where(l => l.LessonDate >= from);
        }
        if (filter.To.HasValue)
        {
            var toExclusive = DateTime.SpecifyKind(filter.To.Value.Date.AddDays(1), DateTimeKind.Utc);
            lessons = lessons.Where(l => l.LessonDate < toExclusive);
        }

        return Details(lessons.OrderByDescending(l => l.LessonDate).ThenByDescending(l => l.CreatedAt))
            .ToPagedResultAsync(paging);
    }

    public Task<LessonDto?> GetLessonDetailAsync(Guid id) =>
        Details(Context.Lessons.AsNoTracking().Where(l => l.Id == id)).FirstOrDefaultAsync();

    public Task<List<LessonDto>> GetRecentLessonsAsync(Guid studentId, int count) =>
        Details(Context.Lessons.AsNoTracking()
                .Where(l => l.StudentId == studentId)
                .OrderByDescending(l => l.LessonDate)
                .ThenByDescending(l => l.CreatedAt)
                .Take(count))
            .ToListAsync();

    private IQueryable<LessonDto> Details(IQueryable<Lesson> lessons) =>
        from l in lessons
        join s in Context.Students on l.StudentId equals s.Id into students
        from s in students.DefaultIfEmpty()
        join u in Context.Users on l.TeacherId equals u.Id into teachers
        from u in teachers.DefaultIfEmpty()
        join q in Context.QuranSurahs on l.SuraId equals q.Id into surahs
        from q in surahs.DefaultIfEmpty()
        select new LessonDto
        {
            Id = l.Id,
            TenantId = l.TenantId,
            StudentId = l.StudentId,
            TeacherId = l.TeacherId,
            StudentName = s != null ? s.FirstName + " " + s.LastName : QueryHelpers.Unknown,
            TeacherName = u != null ? u.FirstName + " " + u.LastName : QueryHelpers.Unknown,
            LessonDate = l.LessonDate,
            CuzNo = l.JuzNumber,
            PageNo = l.PageNumber,
            SurahName = q != null ? q.NameTr : string.Empty,
            IsNewPage = l.IsNewPage,
            Performance = l.Performance,
            HomeworkStatus = l.HomeworkStatus,
            TeacherNotes = l.Notes
        };
}
