using HafizTakip.Core.DataAccess.EntityFramework;
using HafizTakip.Core.Entities.Platform;
using HafizTakip.Core.Enums;
using HafizTakip.Core.Utilities.Requests;
using HafizTakip.Core.Utilities.Results;
using HafizTakip.DataAccess.Abstract;
using HafizTakip.Entities.Dtos;
using Microsoft.EntityFrameworkCore;

namespace HafizTakip.DataAccess.Concrete.EntityFramework;

public class EfSubscriptionDal : EfEntityRepositoryBase<Subscription, HafizTakipDbContext>, ISubscriptionDal
{
    public EfSubscriptionDal(HafizTakipDbContext context) : base(context)
    {
    }

    public Task<PagedResult<SubscriptionDto>> GetSubscriptionDetailsPagedAsync(SubscriptionFilter filter, PaginationParams paging)
    {
        var subscriptions = Context.Subscriptions.AsNoTracking();
        if (filter.TenantId.HasValue)
            subscriptions = subscriptions.Where(s => s.TenantId == filter.TenantId);

        return Details(subscriptions.OrderByDescending(s => s.StartDate)).ToPagedResultAsync(paging);
    }

    public Task<SubscriptionDto?> GetSubscriptionDetailAsync(Guid id) =>
        Details(Context.Subscriptions.AsNoTracking().Where(s => s.Id == id)).FirstOrDefaultAsync();

    public Task<SubscriptionDto?> GetActiveSubscriptionDetailAsync(Guid tenantId) =>
        Details(Context.Subscriptions.AsNoTracking()
                .Where(s => s.TenantId == tenantId && s.Status == SubscriptionStatus.Active)
                .OrderByDescending(s => s.StartDate))
            .FirstOrDefaultAsync();

    private IQueryable<SubscriptionDto> Details(IQueryable<Subscription> subscriptions) =>
        from s in subscriptions
        join t in Context.Tenants on s.TenantId equals t.Id into tenants
        from t in tenants.DefaultIfEmpty()
        select new SubscriptionDto
        {
            Id = s.Id,
            TenantId = s.TenantId,
            TenantName = t != null ? t.Name : QueryHelpers.Unknown,
            PlanName = s.PlanName,
            StartDate = s.StartDate,
            EndDate = s.EndDate,
            Status = s.Status
        };
}
