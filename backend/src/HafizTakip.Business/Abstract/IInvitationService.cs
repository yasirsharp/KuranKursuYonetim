using HafizTakip.Core.Utilities.Results;
using HafizTakip.Entities.Dtos;

namespace HafizTakip.Business.Abstract;

/// <summary>
/// Davetle personel katılımı.
/// Yönetici: davet oluşturur (rol başına tek geçerli davet; yenisi eskisini iptal eder), iptal eder,
/// katılım isteklerini onaylar/reddeder. Kullanıcı yalnızca atayabildiği rollerin davetlerini görür.
/// Herkese açık: davet bilgisi → SMS kodu → katılım isteği (onay bekler).
/// </summary>
public interface IInvitationService
{
    Task<IDataResult<List<InvitationDto>>> GetActiveAsync();
    Task<IDataResult<InvitationDto>> CreateAsync(CreateInvitationDto dto);
    Task<IResult> RevokeAsync(Guid id);

    Task<IDataResult<List<JoinRequestDto>>> GetJoinRequestsAsync(string? status);
    Task<IResult> ApproveAsync(Guid joinRequestId);
    Task<IResult> RejectAsync(Guid joinRequestId, RejectJoinRequestDto dto);

    Task<IDataResult<InvitationPublicDto>> GetPublicAsync(string code);
    Task<IResult> SendJoinCodeAsync(string code, SendOtpDto dto);
    Task<IResult> JoinAsync(string code, JoinWithInvitationDto dto);
}
