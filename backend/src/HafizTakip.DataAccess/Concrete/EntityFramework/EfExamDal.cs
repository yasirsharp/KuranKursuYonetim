using HafizTakip.Core.DataAccess.EntityFramework;
using HafizTakip.Core.Utilities.Requests;
using HafizTakip.Core.Utilities.Results;
using HafizTakip.DataAccess.Abstract;
using HafizTakip.Entities.Concrete;
using HafizTakip.Entities.Dtos;
using Microsoft.EntityFrameworkCore;

namespace HafizTakip.DataAccess.Concrete.EntityFramework;

public class EfExamDal : EfEntityRepositoryBase<Exam, HafizTakipDbContext>, IExamDal
{
    public EfExamDal(HafizTakipDbContext context) : base(context)
    {
    }

    public Task<PagedResult<ExamDto>> GetExamDetailsPagedAsync(ExamFilter filter, PaginationParams paging)
    {
        var exams = Context.Exams.AsNoTracking();
        if (filter.GroupId.HasValue)
            exams = exams.Where(e => e.GroupId == filter.GroupId);

        return Details(exams.OrderByDescending(e => e.ExamDate).ThenBy(e => e.Title)).ToPagedResultAsync(paging);
    }

    public Task<ExamDto?> GetExamDetailAsync(Guid id) =>
        Details(Context.Exams.AsNoTracking().Where(e => e.Id == id)).FirstOrDefaultAsync();

    private IQueryable<ExamDto> Details(IQueryable<Exam> exams) =>
        from e in exams
        join g in Context.Groups on e.GroupId equals g.Id into groups
        from g in groups.DefaultIfEmpty()
        select new ExamDto
        {
            Id = e.Id,
            TenantId = e.TenantId,
            GroupId = e.GroupId,
            GroupName = g != null ? g.Name : QueryHelpers.Unknown,
            ExamType = e.ExamType,
            ExamDate = e.ExamDate,
            Title = e.Title,
            Description = e.Description
        };
}
