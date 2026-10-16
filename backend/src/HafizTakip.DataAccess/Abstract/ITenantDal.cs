using HafizTakip.Core.DataAccess;
using HafizTakip.Core.Entities.Platform;

namespace HafizTakip.DataAccess.Abstract;

public interface ITenantDal : IEntityRepository<Tenant>
{
    /// <summary>
    /// Verilen kısa ad ve "{slug}-N" biçimindeki türevlerinden kullanılanları döner.
    /// Silinmiş kurslar da dahildir (slug kolonu silinmişlerde de benzersizdir).
    /// </summary>
    Task<List<string>> GetTakenSlugsAsync(string baseSlug);
}
