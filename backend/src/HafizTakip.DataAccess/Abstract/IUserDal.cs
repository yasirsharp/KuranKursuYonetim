using HafizTakip.Core.DataAccess;
using HafizTakip.Entities.Concrete;

namespace HafizTakip.DataAccess.Abstract;

public interface IUserDal : IEntityRepository<User>
{
    /// <summary>
    /// Giriş akışı için: telefonu eşleşen, silinmemiş ve kurumu aktif olan kullanıcıları
    /// tüm kurumlarda arar (istek henüz bir tenant'a bağlı değildir).
    /// </summary>
    Task<List<User>> GetLoginCandidatesByPhoneAsync(string normalizedPhone);

    /// <summary>Refresh akışı için: kullanıcıyı tenant filtresi olmadan getirir (silinmişler hariç).</summary>
    Task<User?> GetByIdAcrossTenantsAsync(Guid userId);
}