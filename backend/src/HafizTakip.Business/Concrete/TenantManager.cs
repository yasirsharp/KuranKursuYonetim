using HafizTakip.Core.Constants;
using HafizTakip.Core.DataAccess;
using HafizTakip.Core.Enums;
using HafizTakip.Business.Abstract;
using HafizTakip.Core.Utilities.Requests;
using HafizTakip.Business.ValidationRules.FluentValidation;
using HafizTakip.Core.Aspects.Autofac.Validation;
using HafizTakip.Core.Entities.Platform;
using HafizTakip.Core.Utilities.Results;
using HafizTakip.DataAccess.Abstract;
using HafizTakip.Entities.Concrete;
using HafizTakip.Entities.Dtos;
using Microsoft.Extensions.Caching.Memory;

namespace HafizTakip.Business.Concrete;

/// <summary>Platform yöneticisinin kurs (tenant) yönetimi.</summary>
public class TenantManager : ITenantService
{
    private readonly ITenantDal _tenantDal;
    private readonly ITenantSettingDal _settingDal;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMemoryCache _cache;

    public TenantManager(ITenantDal tenantDal, ITenantSettingDal settingDal, IUnitOfWork unitOfWork, IMemoryCache cache)
    {
        _tenantDal = tenantDal;
        _settingDal = settingDal;
        _unitOfWork = unitOfWork;
        _cache = cache;
    }

    public async Task<IDataResult<PagedResult<TenantDto>>> GetAllAsync(PaginationParams paginationParams)
    {
        var result = await _tenantDal.GetPagedAsync(
            filter: null,
            orderBy: q => q.OrderBy(t => t.Name),
            selector: t => new TenantDto
            {
                Id = t.Id,
                Name = t.Name,
                Slug = t.Slug,
                Status = t.Status,
                ContactEmail = t.ContactEmail,
                ContactPhone = t.ContactPhone,
                IsActive = t.IsActive
            },
            paging: paginationParams);
        return new SuccessDataResult<PagedResult<TenantDto>>(result, "Kurslar listelendi.");
    }

    public async Task<IDataResult<TenantDto>> GetByIdAsync(Guid id)
    {
        var t = await _tenantDal.GetAsync(x => x.Id == id);
        if (t == null) return new ErrorDataResult<TenantDto>("Kurs bulunamadı.", ResultErrorType.NotFound);

        var dto = new TenantDto
        {
            Id = t.Id,
            Name = t.Name,
            Slug = t.Slug,
            Status = t.Status,
            ContactEmail = t.ContactEmail,
            ContactPhone = t.ContactPhone,
            IsActive = t.IsActive
        };

        return new SuccessDataResult<TenantDto>(dto, "Kurs getirildi.");
    }

    [ValidationAspect(typeof(CreateTenantValidator))]
    public async Task<IResult> AddAsync(CreateTenantDto createTenantDto)
    {
        var existing = await _tenantDal.GetAsync(x => x.Slug == createTenantDto.Slug);
        if (existing != null)
        {
            return new ErrorResult("Bu kısa ad (slug) zaten kullanılıyor.", ResultErrorType.Conflict);
        }

        var tenant = new Tenant
        {
            Name = createTenantDto.Name,
            Slug = createTenantDto.Slug,
            ContactEmail = createTenantDto.ContactEmail,
            ContactPhone = createTenantDto.ContactPhone,
            Status = TenantStatus.Trial
        };

        // Kurs ve varsayılan ayarları tek seferde kaydedilir
        _tenantDal.Add(tenant);
        _settingDal.Add(new TenantSetting { TenantId = tenant.Id });
        await _unitOfWork.SaveChangesAsync();

        return new SuccessResult("Kurs başarıyla eklendi.");
    }

    [ValidationAspect(typeof(UpdateTenantValidator))]
    public async Task<IResult> UpdateAsync(UpdateTenantDto updateTenantDto)
    {
        var tenant = await _tenantDal.GetAsync(x => x.Id == updateTenantDto.Id);
        if (tenant == null) return new ErrorResult("Kurs bulunamadı.", ResultErrorType.NotFound);

        tenant.Name = updateTenantDto.Name;
        tenant.Status = updateTenantDto.Status;
        tenant.IsActive = updateTenantDto.IsActive;
        tenant.ContactEmail = updateTenantDto.ContactEmail;
        tenant.ContactPhone = updateTenantDto.ContactPhone;

        await _tenantDal.UpdateAsync(tenant);
        // Askıya alma/aktifleştirme bir sonraki istekte hemen etkili olsun
        _cache.Remove(TenantCacheKeys.Status(tenant.Id));
        return new SuccessResult("Kurs güncellendi.");
    }

    public async Task<IResult> DeleteAsync(Guid id)
    {
        var tenant = await _tenantDal.GetAsync(x => x.Id == id);
        if (tenant == null) return new ErrorResult("Kurs bulunamadı.", ResultErrorType.NotFound);

        await _tenantDal.DeleteAsync(tenant); // DbContext soft delete'e çevirir
        _cache.Remove(TenantCacheKeys.Status(tenant.Id));

        return new SuccessResult("Kurs silindi (Soft Delete).");
    }
}
