using HafizTakip.Core.DataAccess.EntityFramework;
using HafizTakip.Core.Utilities.Requests;
using HafizTakip.Core.Utilities.Results;
using HafizTakip.DataAccess.Abstract;
using HafizTakip.Entities.Concrete;
using HafizTakip.Entities.Dtos;
using Microsoft.EntityFrameworkCore;

namespace HafizTakip.DataAccess.Concrete.EntityFramework;

public class EfParentNotificationDal : EfEntityRepositoryBase<ParentNotification, HafizTakipDbContext>, IParentNotificationDal
{
    public EfParentNotificationDal(HafizTakipDbContext context) : base(context)
    {
    }

    public Task<PagedResult<ParentNotificationDto>> GetNotificationDetailsPagedAsync(
        ParentNotificationFilter filter, PaginationParams paging)
    {
        var notifications = Context.ParentNotifications.AsNoTracking();
        if (filter.StudentId.HasValue)
            notifications = notifications.Where(n => n.StudentId == filter.StudentId);

        return Details(notifications.OrderByDescending(n => n.CreatedAt)).ToPagedResultAsync(paging);
    }

    public Task<ParentNotificationDto?> GetNotificationDetailAsync(Guid id) =>
        Details(Context.ParentNotifications.AsNoTracking().Where(n => n.Id == id)).FirstOrDefaultAsync();

    private IQueryable<ParentNotificationDto> Details(IQueryable<ParentNotification> notifications) =>
        from n in notifications
        join s in Context.Students on n.StudentId equals s.Id into students
        from s in students.DefaultIfEmpty()
        select new ParentNotificationDto
        {
            Id = n.Id,
            TenantId = n.TenantId,
            StudentId = n.StudentId,
            StudentName = s != null ? s.FirstName + " " + s.LastName : QueryHelpers.Unknown,
            NotificationType = n.NotificationType,
            Title = n.Title,
            Message = n.Message,
            RecipientPhone = n.RecipientPhone,
            DeliveryChannel = n.DeliveryChannel,
            DeliveryStatus = n.DeliveryStatus,
            SentAt = n.SentAt,
            CreatedAt = n.CreatedAt
        };
}
