using HafizTakip.Business.Abstract;
using HafizTakip.Business.ValidationRules.FluentValidation;
using HafizTakip.Core.Aspects.Autofac.Validation;
using HafizTakip.Core.Utilities.IoC;
using HafizTakip.Core.Utilities.Results;
using HafizTakip.DataAccess.Abstract;
using HafizTakip.Entities.Concrete;
using HafizTakip.Entities.Dtos;

namespace HafizTakip.Business.Concrete;

public class TenantSettingsManager : ITenantSettingsService
{
    private readonly ITenantSettingDal _settingDal;
    private readonly ITenantDal _tenantDal;
    private readonly ITenantContext _tenantContext;

    public TenantSettingsManager(ITenantSettingDal settingDal, ITenantDal tenantDal, ITenantContext tenantContext)
    {
        _settingDal = settingDal;
        _tenantDal = tenantDal;
        _tenantContext = tenantContext;
    }

    public async Task<IDataResult<TenantSettingsDto>> GetAsync()
    {
        var settings = await GetOrCreateAsync();
        return new SuccessDataResult<TenantSettingsDto>(ToDto(settings), "Kurs ayarları getirildi.");
    }

    [ValidationAspect(typeof(UpdateTenantSettingsValidator))]
    public async Task<IResult> UpdateAsync(TenantSettingsDto dto)
    {
        var settings = await GetOrCreateAsync();

        settings.WorkingDays = dto.WorkingDays.Distinct().OrderBy(d => d).ToArray();
        settings.DayStartTime = dto.DayStartTime;
        settings.DayEndTime = dto.DayEndTime;
        settings.DefaultSlotMinutes = dto.DefaultSlotMinutes;
        settings.TimeZone = dto.TimeZone;
        settings.AbsenceThreshold = dto.AbsenceThreshold;
        settings.AbsencePeriodDays = dto.AbsencePeriodDays;
        settings.EtutStartTime = dto.EtutStartTime;
        settings.ReviewMode = dto.ReviewMode;
        settings.DailyReviewTargetPages = dto.DailyReviewTargetPages;
        settings.NotifyParentOnAbsence = dto.NotifyParentOnAbsence;
        settings.NotifyParentOnMilestone = dto.NotifyParentOnMilestone;

        await _settingDal.UpdateAsync(settings);
        return new SuccessResult("Kurs ayarları güncellendi.");
    }

    public async Task<IDataResult<CourseInfoDto>> GetCourseInfoAsync()
    {
        var tenant = await _tenantDal.GetAsync(t => t.Id == _tenantContext.TenantId);
        if (tenant == null) return new ErrorDataResult<CourseInfoDto>("Kurs bulunamadı.", ResultErrorType.NotFound);

        return new SuccessDataResult<CourseInfoDto>(new CourseInfoDto
        {
            Id = tenant.Id,
            Name = tenant.Name,
            Slug = tenant.Slug,
            Status = tenant.Status,
            ContactEmail = tenant.ContactEmail,
            ContactPhone = tenant.ContactPhone
        });
    }

    [ValidationAspect(typeof(UpdateCourseInfoValidator))]
    public async Task<IResult> UpdateCourseInfoAsync(UpdateCourseInfoDto dto)
    {
        var tenant = await _tenantDal.GetAsync(t => t.Id == _tenantContext.TenantId);
        if (tenant == null) return new ErrorResult("Kurs bulunamadı.", ResultErrorType.NotFound);

        // Kurs müdürü yalnızca ad ve iletişim bilgilerini değiştirebilir; durum/aktiflik platform yöneticisindedir
        tenant.Name = dto.Name.Trim();
        tenant.ContactEmail = dto.ContactEmail;
        tenant.ContactPhone = dto.ContactPhone;

        await _tenantDal.UpdateAsync(tenant);
        return new SuccessResult("Kurs bilgileri güncellendi.");
    }

    private async Task<TenantSetting> GetOrCreateAsync()
    {
        var settings = await _settingDal.GetAsync(s => s.TenantId == _tenantContext.TenantId);
        if (settings != null) return settings;

        // Eski kurslarda veya yarım kalmış kurulumda ayar satırı yoksa varsayılanlarla oluştur
        settings = new TenantSetting { TenantId = _tenantContext.TenantId };
        await _settingDal.AddAsync(settings);
        return settings;
    }

    private static TenantSettingsDto ToDto(TenantSetting s) => new()
    {
        WorkingDays = s.WorkingDays.OrderBy(d => d).ToList(),
        DayStartTime = s.DayStartTime,
        DayEndTime = s.DayEndTime,
        DefaultSlotMinutes = s.DefaultSlotMinutes,
        TimeZone = s.TimeZone,
        AbsenceThreshold = s.AbsenceThreshold,
        AbsencePeriodDays = s.AbsencePeriodDays,
        EtutStartTime = s.EtutStartTime,
        ReviewMode = s.ReviewMode,
        DailyReviewTargetPages = s.DailyReviewTargetPages,
        NotifyParentOnAbsence = s.NotifyParentOnAbsence,
        NotifyParentOnMilestone = s.NotifyParentOnMilestone
    };
}
