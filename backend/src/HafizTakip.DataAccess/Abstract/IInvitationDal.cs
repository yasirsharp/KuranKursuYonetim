using HafizTakip.Core.DataAccess;
using HafizTakip.Core.Entities.Platform;
using HafizTakip.Entities.Concrete;

namespace HafizTakip.DataAccess.Abstract;

public interface IInvitationDal : IEntityRepository<Invitation>
{
    /// <summary>
    /// Herkese açık davet sayfası için: kodu tüm kurslarda arar (istek henüz bir kursa bağlı değildir).
    /// Silinmiş davet ve silinmiş kurs dönmez; süre/iptal/kurs durumu kontrolü çağırana aittir.
    /// </summary>
    Task<(Invitation Invitation, Tenant Tenant)?> GetByCodeAcrossTenantsAsync(string code);

    Task<bool> CodeExistsAsync(string code);
}
