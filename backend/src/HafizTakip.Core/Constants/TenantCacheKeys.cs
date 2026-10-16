namespace HafizTakip.Core.Constants;

/// <summary>Kursa ait önbellek anahtarları (middleware ile yönetim servisleri aynı anahtarı kullanır).</summary>
public static class TenantCacheKeys
{
    /// <summary>Kursun durum bilgisi (aktif / onay bekliyor / kapalı); kurs güncellenince silinmelidir.</summary>
    public static string Status(Guid tenantId) => $"tenant_status_{tenantId}";
}
