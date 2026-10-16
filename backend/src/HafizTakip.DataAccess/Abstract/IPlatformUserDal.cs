using HafizTakip.Core.DataAccess;
using HafizTakip.Core.Entities.Platform;

namespace HafizTakip.DataAccess.Abstract;

public interface IPlatformUserDal : IEntityRepository<PlatformUser>
{
    /// <summary>E-posta büyük/küçük harf duyarsız eşleşir (silinmişler hariç).</summary>
    Task<PlatformUser?> GetByEmailAsync(string email);
}
