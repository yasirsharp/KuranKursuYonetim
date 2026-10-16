using HafizTakip.Core.Utilities.Results;
using HafizTakip.Entities.Dtos;

namespace HafizTakip.Business.Abstract;

/// <summary>
/// "Kurs oluştur" başvurusu: telefon doğrulanır, kurs "onay bekliyor" durumunda açılır ve başvuran
/// kurs yöneticisi olarak oturum açar. Platform yöneticisi onaylayana kadar yalnızca başvuru durumunu görür.
/// </summary>
public interface ICourseApplicationService
{
    Task<IResult> SendCodeAsync(SendOtpDto dto);
    Task<IDataResult<TokenResponseDto>> SubmitAsync(CourseApplicationDto dto);

    /// <summary>Oturumdaki kursun başvuru durumu (onaylandıktan sonra da çağrılabilir; durum "active" döner).</summary>
    Task<IDataResult<CourseApplicationStatusDto>> GetMineAsync();

    /// <summary>Reddedilen başvuruyu düzeltilmiş bilgilerle yeniden onaya gönderir.</summary>
    Task<IResult> ResubmitAsync(CourseApplicationInfoDto dto);
}
