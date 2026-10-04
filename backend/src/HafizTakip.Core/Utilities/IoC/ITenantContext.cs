namespace HafizTakip.Core.Utilities.IoC;

/// <summary>
/// Geçerli isteği yapan tenant'ın Id'sini tutar.
/// </summary>
public interface ITenantContext
{
    Guid TenantId { get; }
    void SetTenantId(Guid tenantId);
}

public class TenantContext : ITenantContext
{
    public Guid TenantId { get; private set; }

    public void SetTenantId(Guid tenantId)
    {
        if (tenantId == Guid.Empty)
            throw new ArgumentException("TenantId boş olamaz.");
            
        TenantId = tenantId;
    }
}
