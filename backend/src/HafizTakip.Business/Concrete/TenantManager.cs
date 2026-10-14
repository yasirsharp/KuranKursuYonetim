using HafizTakip.Core.Enums;
using HafizTakip.Business.Abstract;
using HafizTakip.Core.Utilities.Requests;
using HafizTakip.Business.ValidationRules.FluentValidation;
using HafizTakip.Core.Aspects.Autofac.Validation;
using HafizTakip.Core.Entities.Platform;
using HafizTakip.Core.Utilities.Results;
using HafizTakip.DataAccess.Abstract;
using HafizTakip.Entities.Dtos;

namespace HafizTakip.Business.Concrete;

public class TenantManager : ITenantService
{
    private readonly ITenantDal _tenantDal;

    public TenantManager(ITenantDal tenantDal)
    {
        _tenantDal = tenantDal;
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
        if (t == null) return new ErrorDataResult<TenantDto>("Kurs bulunamadı.");

        var dto = new TenantDto
        {
            Id = t.Id,
            Name = t.Name,
            Slug = t.Slug,
            Status = t.Status,
            ContactEmail = t.ContactEmail,
            ContactPhone = t.ContactPhone
        };
        
        return new SuccessDataResult<TenantDto>(dto, "Kurs getirildi.");
    }

    [ValidationAspect(typeof(CreateTenantValidator))]
    public async Task<IResult> AddAsync(CreateTenantDto createTenantDto)
    {
        var existing = await _tenantDal.GetAsync(x => x.Slug == createTenantDto.Slug);
        if (existing != null)
        {
            return new ErrorResult("Bu kısa ad (slug) zaten kullanılıyor.");
        }

        var tenant = new Tenant
        {
            Name = createTenantDto.Name,
            Slug = createTenantDto.Slug,
            ContactEmail = createTenantDto.ContactEmail,
            ContactPhone = createTenantDto.ContactPhone,
            Status = TenantStatus.Trial
        };

        await _tenantDal.AddAsync(tenant);
        return new SuccessResult("Kurs başarıyla eklendi.");
    }

    [ValidationAspect(typeof(UpdateTenantValidator))]
    public async Task<IResult> UpdateAsync(UpdateTenantDto updateTenantDto)
    {
        var tenant = await _tenantDal.GetAsync(x => x.Id == updateTenantDto.Id);
        if (tenant == null) return new ErrorResult("Kurs bulunamadı.");

        tenant.Name = updateTenantDto.Name;
        tenant.Status = updateTenantDto.Status;
        tenant.ContactEmail = updateTenantDto.ContactEmail;
        tenant.ContactPhone = updateTenantDto.ContactPhone;
        tenant.UpdatedAt = DateTime.UtcNow;

        await _tenantDal.UpdateAsync(tenant);
        return new SuccessResult("Kurs güncellendi.");
    }

    public async Task<IResult> DeleteAsync(Guid id)
    {
        var tenant = await _tenantDal.GetAsync(x => x.Id == id);
        if (tenant == null) return new ErrorResult("Kurs bulunamadı.");

        tenant.IsDeleted = true;
        tenant.DeletedAt = DateTime.UtcNow;
        await _tenantDal.UpdateAsync(tenant); // Soft delete
        
        return new SuccessResult("Kurs silindi (Soft Delete).");
    }
}
