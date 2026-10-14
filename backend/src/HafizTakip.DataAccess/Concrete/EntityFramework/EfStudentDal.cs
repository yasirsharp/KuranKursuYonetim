using HafizTakip.Core.DataAccess.EntityFramework;
using HafizTakip.Core.Utilities.Requests;
using HafizTakip.Core.Utilities.Results;
using HafizTakip.DataAccess.Abstract;
using HafizTakip.Entities.Concrete;
using HafizTakip.Entities.Dtos;
using Microsoft.EntityFrameworkCore;

namespace HafizTakip.DataAccess.Concrete.EntityFramework;

public class EfStudentDal : EfEntityRepositoryBase<Student, HafizTakipDbContext>, IStudentDal
{
    public EfStudentDal(HafizTakipDbContext context) : base(context)
    {
    }

    public Task<PagedResult<StudentDto>> GetStudentDetailsPagedAsync(
        StudentFilter filter, IReadOnlyCollection<Guid>? scopeStudentIds, PaginationParams paging)
    {
        var students = Context.Students.AsNoTracking();

        if (scopeStudentIds != null)
            students = students.Where(s => scopeStudentIds.Contains(s.Id));
        if (filter.GroupId.HasValue)
            students = students.Where(s => s.GroupId == filter.GroupId);
        if (!string.IsNullOrWhiteSpace(filter.Status))
            students = students.Where(s => s.Status == filter.Status);
        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var pattern = $"%{QueryHelpers.EscapeLike(filter.Search.Trim())}%";
            students = students.Where(s =>
                EF.Functions.ILike(s.FirstName + " " + s.LastName, pattern) ||
                EF.Functions.ILike(s.IdentificationNumber, pattern));
        }

        return Details(students.OrderBy(s => s.FirstName).ThenBy(s => s.LastName)).ToPagedResultAsync(paging);
    }

    public Task<StudentDto?> GetStudentDetailAsync(Guid id) =>
        Details(Context.Students.AsNoTracking().Where(s => s.Id == id)).FirstOrDefaultAsync();

    public async Task<double> GetAverageCompletedPagesAsync(Guid? groupId = null)
    {
        var students = Context.Students.AsNoTracking();
        if (groupId.HasValue) students = students.Where(s => s.GroupId == groupId);
        return await students.Select(s => (double?)s.CompletedPages).AverageAsync() ?? 0;
    }

    private IQueryable<StudentDto> Details(IQueryable<Student> students) =>
        from s in students
        join g in Context.Groups on s.GroupId equals (Guid?)g.Id into groups
        from g in groups.DefaultIfEmpty()
        select new StudentDto
        {
            Id = s.Id,
            TenantId = s.TenantId,
            GroupId = s.GroupId,
            GroupName = g != null ? g.Name : "Sınıfsız",
            FirstName = s.FirstName,
            LastName = s.LastName,
            IdentificationNumber = s.IdentificationNumber,
            CurrentPage = s.CurrentPage,
            CompletedPages = s.CompletedPages,
            IsHafiz = s.IsHafiz,
            IsBoarder = s.IsBoarder,
            Status = s.Status
        };
}
