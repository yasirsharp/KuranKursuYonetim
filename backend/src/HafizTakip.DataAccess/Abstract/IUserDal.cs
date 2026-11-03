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

    /// <summary>E-posta ile giriş için: <see cref="GetLoginCandidatesByPhoneAsync"/> ile aynı kurallar, adres büyük/küçük harf duyarsız.</summary>
    Task<List<User>> GetLoginCandidatesByEmailAsync(string normalizedEmail);

    /// <summary>Refresh akışı için: kullanıcıyı tenant filtresi olmadan getirir (silinmişler hariç).</summary>
    Task<User?> GetByIdAcrossTenantsAsync(Guid userId);

    /// <summary>Kurs başvurusu bildirimleri için: kursun yöneticilerini tenant filtresi olmadan getirir (platform yöneticisinin bağlamı yoktur).</summary>
    Task<List<User>> GetTenantAdminsAcrossTenantsAsync(Guid tenantId);

    /// <summary>Bu telefonla açılmış ve hâlâ onay bekleyen bir kurs başvurusu var mı?</summary>
    Task<bool> HasPendingApplicationAsync(string normalizedPhone);

    /// <summary>
    /// Davet onayı için: kursta bu numaraya ait kullanıcıyı silinmiş olsa da getirir
    /// (users(tenant_id, phone_number) benzersizliği silinmiş kayıtları da kapsar; silinmiş kayıt yeniden etkinleştirilir).
    /// </summary>
    Task<User?> GetByPhoneIncludingDeletedAsync(Guid tenantId, string normalizedPhone);
}
