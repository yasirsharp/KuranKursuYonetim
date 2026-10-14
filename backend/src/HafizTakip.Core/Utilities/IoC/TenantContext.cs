namespace HafizTakip.Core.Utilities.IoC;

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
