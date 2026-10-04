namespace HafizTakip.Core.Entities.Platform;

/// <summary>
/// Sistem yöneticilerini veya tenant'tan bağımsız kullanıcıları tutar. (İsteğe bağlı)
/// </summary>
public class PlatformUser : BaseEntity
{
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string Role { get; set; } = "super_admin";
}
