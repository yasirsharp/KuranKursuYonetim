namespace HafizTakip.WebAPI.Security;

/// <summary>
/// Onay bekleyen veya reddedilen kursun kullanıcısının da çağırabileceği uçları işaretler
/// (başvuru durumu, oturum yenileme/çıkış). Diğer tüm uçlar bu kurslar için 403 döner.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class AllowPendingTenantAttribute : Attribute
{
}
