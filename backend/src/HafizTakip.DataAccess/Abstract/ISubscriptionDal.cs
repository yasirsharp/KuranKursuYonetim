using HafizTakip.Core.DataAccess;
using HafizTakip.Core.Entities.Platform;
using HafizTakip.Core.Utilities.Requests;
using HafizTakip.Core.Utilities.Results;
using HafizTakip.Entities.Dtos;

namespace HafizTakip.DataAccess.Abstract;

public interface ISubscriptionDal : IEntityRepository<Subscription>
{
    Task<PagedResult<SubscriptionDto>> GetSubscriptionDetailsPagedAsync(SubscriptionFilter filter, PaginationParams paging);

    Task<SubscriptionDto?> GetSubscriptionDetailAsync(Guid id);

    /// <summary>Kursun aktif aboneliği (kurs adıyla).</summary>
    Task<SubscriptionDto?> GetActiveSubscriptionDetailAsync(Guid tenantId);
}
