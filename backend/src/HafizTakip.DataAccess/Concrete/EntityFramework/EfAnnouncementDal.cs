using HafizTakip.Core.DataAccess.EntityFramework;
using HafizTakip.Core.Utilities.Requests;
using HafizTakip.Core.Utilities.Results;
using HafizTakip.DataAccess.Abstract;
using HafizTakip.Entities.Concrete;
using HafizTakip.Entities.Dtos;
using Microsoft.EntityFrameworkCore;

namespace HafizTakip.DataAccess.Concrete.EntityFramework;

public class EfAnnouncementDal : EfEntityRepositoryBase<Announcement, HafizTakipDbContext>, IAnnouncementDal
{
    public EfAnnouncementDal(HafizTakipDbContext context) : base(context)
    {
    }

    public Task<PagedResult<AnnouncementDto>> GetAnnouncementDetailsPagedAsync(PaginationParams paging) =>
        Details(Context.Announcements.AsNoTracking()
                .OrderByDescending(a => a.IsPinned)
                .ThenByDescending(a => a.PublishedAt))
            .ToPagedResultAsync(paging);

    public Task<AnnouncementDto?> GetAnnouncementDetailAsync(Guid id) =>
        Details(Context.Announcements.AsNoTracking().Where(a => a.Id == id)).FirstOrDefaultAsync();

    private IQueryable<AnnouncementDto> Details(IQueryable<Announcement> announcements) =>
        from a in announcements
        join u in Context.Users on a.AuthorId equals u.Id into authors
        from u in authors.DefaultIfEmpty()
        select new AnnouncementDto
        {
            Id = a.Id,
            TenantId = a.TenantId,
            AuthorId = a.AuthorId,
            AuthorName = u != null ? u.FirstName + " " + u.LastName : QueryHelpers.Unknown,
            Title = a.Title,
            Content = a.Content,
            TargetAudience = a.TargetAudience,
            IsPinned = a.IsPinned,
            PublishedAt = a.PublishedAt
        };
}
