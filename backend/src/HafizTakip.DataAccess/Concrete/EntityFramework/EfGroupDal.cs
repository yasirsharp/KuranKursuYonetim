using HafizTakip.Core.DataAccess.EntityFramework;
using HafizTakip.Core.Utilities.Requests;
using HafizTakip.Core.Utilities.Results;
using HafizTakip.DataAccess.Abstract;
using HafizTakip.Entities.Concrete;
using HafizTakip.Entities.Dtos;
using Microsoft.EntityFrameworkCore;

namespace HafizTakip.DataAccess.Concrete.EntityFramework;

public class EfGroupDal : EfEntityRepositoryBase<Group, HafizTakipDbContext>, IGroupDal
{
    public EfGroupDal(HafizTakipDbContext context) : base(context)
    {
    }

    public Task<PagedResult<GroupDto>> GetGroupDetailsPagedAsync(PaginationParams paging) =>
        Details(Context.Groups.AsNoTracking().OrderBy(g => g.Name)).ToPagedResultAsync(paging);

    public Task<GroupDto?> GetGroupDetailAsync(Guid id) =>
        Details(Context.Groups.AsNoTracking().Where(g => g.Id == id)).FirstOrDefaultAsync();

    private IQueryable<GroupDto> Details(IQueryable<Group> groups) =>
        from g in groups
        join u in Context.Users on g.TeacherId equals u.Id into teachers
        from u in teachers.DefaultIfEmpty()
        select new GroupDto
        {
            Id = g.Id,
            TenantId = g.TenantId,
            TeacherId = g.TeacherId,
            TeacherName = u != null ? u.FirstName + " " + u.LastName : QueryHelpers.Unknown,
            Name = g.Name,
            Description = g.Description
        };
}
