namespace HafizTakip.Core.Utilities.IoC;

/// <summary>
/// İsteği yapan oturum açmış kullanıcının bilgileri (JWT claim'lerinden okunur).
/// </summary>
public interface ICurrentUser
{
    bool IsAuthenticated { get; }
    Guid? UserId { get; }
    string? Role { get; }

    /// <summary>Kullanıcının rolü minimumRole ile aynı veya daha yetkili mi?</summary>
    bool IsInRoleOrAbove(string minimumRole);
}
