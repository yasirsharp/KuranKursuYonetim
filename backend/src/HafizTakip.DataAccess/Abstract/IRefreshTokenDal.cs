using HafizTakip.Core.DataAccess;
using HafizTakip.Entities.Concrete;

namespace HafizTakip.DataAccess.Abstract;

public interface IRefreshTokenDal : IEntityRepository<RefreshToken>
{
    /// <summary>Token özetine göre arar. Refresh isteği anonim olduğundan tenant filtresi uygulanmaz.</summary>
    Task<RefreshToken?> GetByHashAcrossTenantsAsync(string tokenHash);

    /// <summary>Eski token'ı iptal edip yenisini tek SaveChanges ile kaydeder.</summary>
    Task RotateAsync(RefreshToken current, RefreshToken replacement);

    /// <summary>Kullanıcının tüm aktif token'larını iptal eder (token yeniden kullanımı tespit edildiğinde).</summary>
    Task RevokeAllForUserAsync(Guid userId, DateTime utcNow);
}