using HafizTakip.Core.Enums;
using HafizTakip.Core.Aspects.Autofac.Validation;
using HafizTakip.Business.ValidationRules.FluentValidation;
using HafizTakip.Core.DataAccess;
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
    private readonly IUnitOfWork _unitOfWork;

    public SubscriptionManager(ISubscriptionDal subscriptionDal, ITenantDal tenantDal, IUnitOfWork unitOfWork)
    {
        _subscriptionDal = subscriptionDal;
        _tenantDal = tenantDal;
        _unitOfWork = unitOfWork;
    }

    public async Task<IDataResult<PagedResult<SubscriptionDto>>> GetAllAsync(PaginationParams paginationParams)
    {
        var result = await _subscriptionDal.GetSubscriptionDetailsPagedAsync(new SubscriptionFilter(), paginationParams);
        return new SuccessDataResult<PagedResult<SubscriptionDto>>(result, "Abonelikler listelendi.");
    }

    public async Task<IDataResult<SubscriptionDto>> GetByTenantIdAsync(Guid tenantId)
    {
        var dto = await _subscriptionDal.GetActiveSubscriptionDetailAsync(tenantId);
        return dto == null
            ? new ErrorDataResult<SubscriptionDto>("Kursa ait aktif abonelik bulunamadı.", ResultErrorType.NotFound)
            : new SuccessDataResult<SubscriptionDto>(dto, "Aktif abonelik getirildi.");
    }

    public async Task<IDataResult<SubscriptionDto>> GetByIdAsync(Guid id)
    {
        var dto = await _subscriptionDal.GetSubscriptionDetailAsync(id);
        return dto == null
            ? new ErrorDataResult<SubscriptionDto>("Abonelik bulunamadı.", ResultErrorType.NotFound)
            : new SuccessDataResult<SubscriptionDto>(dto, "Abonelik getirildi.");
    }

    [ValidationAspect(typeof(CreateSubscriptionValidator))]
    public async Task<IResult> AddAsync(CreateSubscriptionDto createSubscriptionDto)
    {
        var tenant = await _tenantDal.GetAsync(t => t.Id == createSubscriptionDto.TenantId);
        if (tenant == null) return new ErrorResult("Kurs bulunamadı.");

        // Eski aktif abonelikleri iptal et
        var existingActive = await _subscriptionDal.GetListAsync(s => s.TenantId == createSubscriptionDto.TenantId && s.Status == SubscriptionStatus.Active);
        foreach (var existing in existingActive)
        {
            existing.Status = SubscriptionStatus.Cancelled;
            _subscriptionDal.Update(existing);
        }

        var subscription = new Subscription
        {
            TenantId = createSubscriptionDto.TenantId,
            PlanName = createSubscriptionDto.PlanName,
            StartDate = createSubscriptionDto.StartDate.Date,
            EndDate = createSubscriptionDto.EndDate.Date,
            Status = SubscriptionStatus.Active
        };

        _subscriptionDal.Add(subscription);

        // Eski aboneliklerin iptali ve yenisinin eklenmesi tek SaveChanges ile atomik kaydedilir
        await _unitOfWork.SaveChangesAsync();
        return new SuccessResult("Abonelik oluşturuldu.");
    }

    [ValidationAspect(typeof(UpdateSubscriptionValidator))]
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
