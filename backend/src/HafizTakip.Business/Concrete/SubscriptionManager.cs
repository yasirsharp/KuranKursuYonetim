using HafizTakip.Business.Abstract;
using HafizTakip.Core.Utilities.Requests;
using HafizTakip.Core.Utilities.Results;
using HafizTakip.DataAccess.Abstract;
using HafizTakip.Core.Entities.Platform;
using HafizTakip.Entities.Dtos;

namespace HafizTakip.Business.Concrete;

public class SubscriptionManager : ISubscriptionService
{
    private readonly ISubscriptionDal _subscriptionDal;
    private readonly ITenantDal _tenantDal;

    public SubscriptionManager(ISubscriptionDal subscriptionDal, ITenantDal tenantDal)
    {
        _subscriptionDal = subscriptionDal;
        _tenantDal = tenantDal;
    }

    public async Task<IDataResult<PagedResult<SubscriptionDto>>> GetAllAsync(PaginationParams paginationParams)
    {
        var subscriptions = await _subscriptionDal.GetListAsync();
        
        var totalCount = subscriptions.Count;
        var pagedList = subscriptions
            .OrderByDescending(x => x.CreatedAt)
            .Skip((paginationParams.Page - 1) * paginationParams.PageSize)
            .Take(paginationParams.PageSize)
            .ToList();

        var dtoList = new List<SubscriptionDto>();
        foreach (var subscription in pagedList)
        {
            var tenant = await _tenantDal.GetAsync(t => t.Id == subscription.TenantId);
            
            dtoList.Add(new SubscriptionDto
            {
                Id = subscription.Id,
                TenantId = subscription.TenantId,
                TenantName = tenant != null ? tenant.Name : "Bilinmiyor",
                PlanName = subscription.PlanName,
                StartDate = subscription.StartDate,
                EndDate = subscription.EndDate,
                Status = subscription.Status
            });
        }

        var result = new PagedResult<SubscriptionDto>(dtoList, totalCount, paginationParams.Page, paginationParams.PageSize);
        return new SuccessDataResult<PagedResult<SubscriptionDto>>(result, "Abonelikler listelendi.");
    }

    public async Task<IDataResult<SubscriptionDto>> GetByTenantIdAsync(Guid tenantId)
    {
        var subscription = await _subscriptionDal.GetAsync(s => s.TenantId == tenantId && s.Status == "active");
        if (subscription == null) return new ErrorDataResult<SubscriptionDto>("Aktif abonelik bulunamadı.");

        var tenant = await _tenantDal.GetAsync(t => t.Id == subscription.TenantId);

        var dto = new SubscriptionDto
        {
            Id = subscription.Id,
            TenantId = subscription.TenantId,
            TenantName = tenant != null ? tenant.Name : "Bilinmiyor",
            PlanName = subscription.PlanName,
            StartDate = subscription.StartDate,
            EndDate = subscription.EndDate,
            Status = subscription.Status
        };

        return new SuccessDataResult<SubscriptionDto>(dto, "Abonelik getirildi.");
    }

    public async Task<IDataResult<SubscriptionDto>> GetByIdAsync(Guid id)
    {
        var subscription = await _subscriptionDal.GetAsync(s => s.Id == id);
        if (subscription == null) return new ErrorDataResult<SubscriptionDto>("Abonelik bulunamadı.");

        var tenant = await _tenantDal.GetAsync(t => t.Id == subscription.TenantId);

        var dto = new SubscriptionDto
        {
            Id = subscription.Id,
            TenantId = subscription.TenantId,
            TenantName = tenant != null ? tenant.Name : "Bilinmiyor",
            PlanName = subscription.PlanName,
            StartDate = subscription.StartDate,
            EndDate = subscription.EndDate,
            Status = subscription.Status
        };

        return new SuccessDataResult<SubscriptionDto>(dto, "Abonelik getirildi.");
    }

    public async Task<IResult> AddAsync(CreateSubscriptionDto createSubscriptionDto)
    {
        var tenant = await _tenantDal.GetAsync(t => t.Id == createSubscriptionDto.TenantId);
        if (tenant == null) return new ErrorResult("Kurs bulunamadı.");

        // Eski aktif abonelikleri iptal et
        var existingActive = await _subscriptionDal.GetListAsync(s => s.TenantId == createSubscriptionDto.TenantId && s.Status == "active");
        foreach (var existing in existingActive)
        {
            existing.Status = "cancelled";
            existing.UpdatedAt = DateTime.UtcNow;
            await _subscriptionDal.UpdateAsync(existing);
        }

        var subscription = new Subscription
        {
            TenantId = createSubscriptionDto.TenantId,
            PlanName = createSubscriptionDto.PlanName,
            StartDate = createSubscriptionDto.StartDate.Date,
            EndDate = createSubscriptionDto.EndDate.Date,
            Status = "active"
        };

        await _subscriptionDal.AddAsync(subscription);
        return new SuccessResult("Abonelik oluşturuldu.");
    }

    public async Task<IResult> UpdateAsync(UpdateSubscriptionDto updateSubscriptionDto)
    {
        var subscription = await _subscriptionDal.GetAsync(s => s.Id == updateSubscriptionDto.Id);
        if (subscription == null) return new ErrorResult("Abonelik bulunamadı.");

        subscription.PlanName = updateSubscriptionDto.PlanName;
        subscription.EndDate = updateSubscriptionDto.EndDate.Date;
        subscription.Status = updateSubscriptionDto.Status;
        subscription.UpdatedAt = DateTime.UtcNow;

        await _subscriptionDal.UpdateAsync(subscription);
        return new SuccessResult("Abonelik güncellendi.");
    }

    public async Task<IResult> DeleteAsync(Guid id)
    {
        var subscription = await _subscriptionDal.GetAsync(s => s.Id == id);
        if (subscription == null) return new ErrorResult("Abonelik bulunamadı.");

        subscription.IsDeleted = true;
        subscription.DeletedAt = DateTime.UtcNow;

        await _subscriptionDal.UpdateAsync(subscription);
        return new SuccessResult("Abonelik silindi.");
    }
}
