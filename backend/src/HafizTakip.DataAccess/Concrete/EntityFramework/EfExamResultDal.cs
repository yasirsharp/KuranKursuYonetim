using HafizTakip.Core.DataAccess.EntityFramework;
using HafizTakip.Core.Utilities.Requests;
using HafizTakip.Core.Utilities.Results;
using HafizTakip.DataAccess.Abstract;
using HafizTakip.Entities.Concrete;
using HafizTakip.Entities.Dtos;
using Microsoft.EntityFrameworkCore;

namespace HafizTakip.DataAccess.Concrete.EntityFramework;

public class EfExamResultDal : EfEntityRepositoryBase<ExamResult, HafizTakipDbContext>, IExamResultDal
{
    public EfExamResultDal(HafizTakipDbContext context) : base(context)
    {
    }

    public Task<PagedResult<ExamResultDto>> GetExamResultDetailsPagedAsync(
        ExamResultFilter filter, IReadOnlyCollection<Guid>? scopeStudentIds, PaginationParams paging)
    {
        var results = Context.ExamResults.AsNoTracking();

        if (scopeStudentIds != null)
            results = results.Where(r => scopeStudentIds.Contains(r.StudentId));
        if (filter.ExamId.HasValue)
            results = results.Where(r => r.ExamId == filter.ExamId);
        if (filter.StudentId.HasValue)
            results = results.Where(r => r.StudentId == filter.StudentId);

        return Details(results.OrderByDescending(r => r.Score).ThenBy(r => r.StudentId))
            .ToPagedResultAsync(paging);
    }

    public Task<ExamResultDto?> GetExamResultDetailAsync(Guid id) =>
        Details(Context.ExamResults.AsNoTracking().Where(r => r.Id == id)).FirstOrDefaultAsync();

    private IQueryable<ExamResultDto> Details(IQueryable<ExamResult> results) =>
        from r in results
        join e in Context.Exams on r.ExamId equals e.Id into exams
        from e in exams.DefaultIfEmpty()
        join s in Context.Students on r.StudentId equals s.Id into students
        from s in students.DefaultIfEmpty()
        select new ExamResultDto
        {
            Id = r.Id,
            TenantId = r.TenantId,
            ExamId = r.ExamId,
            ExamTitle = e != null ? e.Title : QueryHelpers.Unknown,
            StudentId = r.StudentId,
            StudentName = s != null ? s.FirstName + " " + s.LastName : QueryHelpers.Unknown,
            Score = r.Score,
            Grade = r.Grade,
            TeacherNotes = r.TeacherNotes
        };
}
