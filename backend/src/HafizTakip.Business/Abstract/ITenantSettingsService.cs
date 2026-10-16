using HafizTakip.Core.Utilities.Results;
using HafizTakip.Entities.Dtos;

namespace HafizTakip.Business.Abstract;

/// <summary>Oturumdaki kursun ayarları ve kurs bilgileri.</summary>
public interface ITenantSettingsService
{
    /// <summary>Kurs ayarları; ayar satırı yoksa varsayılanlarla oluşturulur.</summary>
    Task<IDataResult<TenantSettingsDto>> GetAsync();
    Task<IResult> UpdateAsync(TenantSettingsDto settings);

    Task<IDataResult<CourseInfoDto>> GetCourseInfoAsync();
    Task<IResult> UpdateCourseInfoAsync(UpdateCourseInfoDto courseInfo);
}
