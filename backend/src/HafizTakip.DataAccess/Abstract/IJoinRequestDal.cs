using HafizTakip.Core.DataAccess;
using HafizTakip.Entities.Concrete;

namespace HafizTakip.DataAccess.Abstract;

public interface IJoinRequestDal : IEntityRepository<JoinRequest>
{
    /// <summary>Giriş akışı için: numaranın herhangi bir kursta onay bekleyen katılım isteği var mı?</summary>
    Task<bool> HasPendingAcrossTenantsAsync(string normalizedPhone);
}
