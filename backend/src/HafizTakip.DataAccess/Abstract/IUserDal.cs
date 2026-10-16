using HafizTakip.Core.DataAccess;
using HafizTakip.Entities.Concrete;

namespace HafizTakip.DataAccess.Abstract;

public interface IUserDal : IEntityRepository<User>
{
    /// <summary>
    /// Giriş akışı için: telefonu eşleşen, silinmemiş ve kurumu giriş yapılabilir durumda (bkz. TenantStatus.SignInAllowed) olan kullanıcıları
    /// tüm kurumlarda arar (istek henüz bir tenant'a bağlı değildir).
    /// </summary>
    Task<List<User>> GetLoginCandidatesByPhoneAsync(string normalizedPhone);

    /// <summary>Refresh akışı için: kullanıcıyı tenant filtresi olmadan getirir (silinmişler hariç).</summary>
    Task<User?> GetByIdAcrossTenantsAsync(Guid userId);

    /// <summary>Kurs başvurusu bildirimleri için: kursun yöneticilerini tenant filtresi olmadan getirir (platform yöneticisinin bağlamı yoktur).</summary>
    Task<List<User>> GetTenantAdminsAcrossTenantsAsync(Guid tenantId);

    /// <summary>Bu telefonla açılmış ve hâlâ onay bekleyen bir kurs başvurusu var mı?</summary>
    Task<bool> HasPendingApplicationAsync(string normalizedPhone);
}
