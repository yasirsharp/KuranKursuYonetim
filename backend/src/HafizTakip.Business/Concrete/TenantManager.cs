using System.Linq.Expressions;
using HafizTakip.Core.Constants;
using HafizTakip.Core.DataAccess;
using HafizTakip.Core.Enums;
using HafizTakip.Business.Abstract;
using HafizTakip.Core.Utilities.Requests;
using HafizTakip.Business.ValidationRules.FluentValidation;
using HafizTakip.Core.Aspects.Autofac.Validation;
using HafizTakip.Core.Entities.Platform;
using HafizTakip.Core.Utilities.IoC;
using HafizTakip.Core.Utilities.Results;
using HafizTakip.Core.Utilities.Sms;
using HafizTakip.DataAccess.Abstract;
using HafizTakip.Entities.Concrete;
using HafizTakip.Entities.Dtos;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace HafizTakip.Business.Concrete;

/// <summary>Platform yöneticisinin kurs (tenant) yönetimi ve kurs başvurularının onayı.</summary>
public class TenantManager : ITenantService
{
    private readonly ITenantDal _tenantDal;
    private readonly ITenantSettingDal _settingDal;
    private readonly IUserDal _userDal;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMemoryCache _cache;
    private readonly ICurrentUser _currentUser;
    private readonly ISmsSender _smsSender;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<TenantManager> _logger;

    public TenantManager(
        ITenantDal tenantDal,
        ITenantSettingDal settingDal,
        IUserDal userDal,
        IUnitOfWork unitOfWork,
        IMemoryCache cache,
        ICurrentUser currentUser,
        ISmsSender smsSender,
        TimeProvider timeProvider,
        ILogger<TenantManager> logger)
    {
        _tenantDal = tenantDal;
        _settingDal = settingDal;
        _userDal = userDal;
        _unitOfWork = unitOfWork;
        _cache = cache;
        _currentUser = currentUser;
        _smsSender = smsSender;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    private static readonly Expression<Func<Tenant, TenantDto>> ToDto = t => new TenantDto
    {
        Id = t.Id,
        Name = t.Name,
        Slug = t.Slug,
        Status = t.Status,
        ContactEmail = t.ContactEmail,
        ContactPhone = t.ContactPhone,
        IsActive = t.IsActive,
        City = t.City,
        District = t.District,
        Address = t.Address,
        SubmittedAt = t.SubmittedAt,
        ReviewedAt = t.ReviewedAt,
        RejectionReason = t.RejectionReason,
        CreatedAt = t.CreatedAt
    };

    private static readonly Func<Tenant, TenantDto> ToDtoCompiled = ToDto.Compile();

    public async Task<IDataResult<PagedResult<TenantDto>>> GetAllAsync(TenantFilterDto filter, PaginationParams paginationParams)
    {
        var status = string.IsNullOrWhiteSpace(filter.Status) ? null : filter.Status.Trim();
        var search = string.IsNullOrWhiteSpace(filter.Search) ? null : filter.Search.Trim().ToLower();

        if (status != null && !TenantStatus.All.Contains(status))
            return new ErrorDataResult<PagedResult<TenantDto>>(
                $"Geçersiz kurs durumu. Geçerli değerler: {string.Join(", ", TenantStatus.All)}.");

        var result = await _tenantDal.GetPagedAsync(
            filter: t => (status == null || t.Status == status)
                         && (search == null || t.Name.ToLower().Contains(search) || (t.City != null && t.City.ToLower().Contains(search))),
            // Onay kuyruğu en eski başvurudan başlar; diğer listeler ada göre
            orderBy: status == TenantStatus.Pending
                ? q => q.OrderBy(t => t.SubmittedAt).ThenBy(t => t.Name)
                : q => q.OrderBy(t => t.Name),
            selector: ToDto,
            paging: paginationParams);
        return new SuccessDataResult<PagedResult<TenantDto>>(result, "Kurslar listelendi.");
    }

    public async Task<IDataResult<TenantDto>> GetByIdAsync(Guid id)
    {
        var t = await _tenantDal.GetAsync(x => x.Id == id);
        if (t == null) return new ErrorDataResult<TenantDto>("Kurs bulunamadı.", ResultErrorType.NotFound);

        return new SuccessDataResult<TenantDto>(ToDtoCompiled(t), "Kurs getirildi.");
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

        // Kurs ve varsayılan ayarları aynı transaction'da kaydedilir. EF modelinde aralarında ilişki
        // tanımlı olmadığından insert sırası garanti değildir: önce kurs yazılır.
        return await _unitOfWork.ExecuteInTransactionAsync<IResult>(async () =>
        {
            _tenantDal.Add(tenant);
            await _unitOfWork.SaveChangesAsync();
            _settingDal.Add(new TenantSetting { TenantId = tenant.Id });
            await _unitOfWork.SaveChangesAsync();
            return new SuccessResult("Kurs başarıyla eklendi.");
        });
    }

    [ValidationAspect(typeof(UpdateTenantValidator))]
    public async Task<IResult> UpdateAsync(UpdateTenantDto updateTenantDto)
    {
        var tenant = await _tenantDal.GetAsync(x => x.Id == updateTenantDto.Id);
        if (tenant == null) return new ErrorResult("Kurs bulunamadı.", ResultErrorType.NotFound);

        // Başvuru durumları yalnızca onay/ret/yeniden gönderme akışlarıyla değişir (inceleme kaydı ve SMS bildirimi için)
        if (updateTenantDto.Status != tenant.Status
            && (TenantStatus.IsApplication(tenant.Status) || TenantStatus.IsApplication(updateTenantDto.Status)))
        {
            return new ErrorResult(
                "Başvuru durumundaki kursun durumu buradan değiştirilemez; onayla/reddet işlemlerini kullanın.",
                ResultErrorType.Conflict);
        }

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

    // ── Başvuru onayı ──────────────────────────────────────────────

    public async Task<IResult> ApproveAsync(Guid id)
    {
        var tenant = await _tenantDal.GetAsync(x => x.Id == id);
        if (tenant == null) return new ErrorResult("Kurs bulunamadı.", ResultErrorType.NotFound);
        if (tenant.Status != TenantStatus.Pending)
            return new ErrorResult("Yalnızca onay bekleyen başvurular onaylanabilir.", ResultErrorType.Conflict);

        tenant.Status = TenantStatus.Active;
        tenant.IsActive = true;
        tenant.ReviewedAt = _timeProvider.GetUtcNow().UtcDateTime;
        tenant.ReviewedBy = _currentUser.UserId;
        tenant.RejectionReason = null;

        await _tenantDal.UpdateAsync(tenant);
        _cache.Remove(TenantCacheKeys.Status(tenant.Id));

        await NotifyAdminsAsync(tenant,
            $"Hafız Takip: \"{tenant.Name}\" kurs başvurunuz onaylandı. Uygulamaya giriş yaparak kursunuzu kurmaya başlayabilirsiniz.");
        return new SuccessResult("Kurs başvurusu onaylandı.");
    }

    [ValidationAspect(typeof(RejectCourseApplicationValidator))]
    public async Task<IResult> RejectAsync(Guid id, RejectCourseApplicationDto dto)
    {
        var tenant = await _tenantDal.GetAsync(x => x.Id == id);
        if (tenant == null) return new ErrorResult("Kurs bulunamadı.", ResultErrorType.NotFound);
        if (tenant.Status != TenantStatus.Pending)
            return new ErrorResult("Yalnızca onay bekleyen başvurular reddedilebilir.", ResultErrorType.Conflict);

        tenant.Status = TenantStatus.Rejected;
        tenant.ReviewedAt = _timeProvider.GetUtcNow().UtcDateTime;
        tenant.ReviewedBy = _currentUser.UserId;
        tenant.RejectionReason = dto.Reason.Trim();

        await _tenantDal.UpdateAsync(tenant);
        _cache.Remove(TenantCacheKeys.Status(tenant.Id));

        await NotifyAdminsAsync(tenant,
            $"Hafız Takip: \"{tenant.Name}\" kurs başvurunuz onaylanmadı. Gerekçe: {tenant.RejectionReason} " +
            "Bilgilerinizi düzenleyip yeniden gönderebilirsiniz.");
        return new SuccessResult("Kurs başvurusu reddedildi.");
    }

    /// <summary>Karar SMS'i kaydı bozmaz: gönderilemezse yalnızca loglanır (başvuran uygulamada da durumu görür).</summary>
    private async Task NotifyAdminsAsync(Tenant tenant, string message)
    {
        var admins = await _userDal.GetTenantAdminsAcrossTenantsAsync(tenant.Id);
        foreach (var phone in admins.Select(a => a.PhoneNumber).Distinct())
        {
            try
            {
                if (!await _smsSender.SendAsync(phone, message))
                    _logger.LogWarning("Kurs başvurusu sonucu SMS ile gönderilemedi. Kurs: {TenantId}", tenant.Id);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Kurs başvurusu sonucu SMS ile gönderilemedi. Kurs: {TenantId}", tenant.Id);
            }
        }
    }
}
