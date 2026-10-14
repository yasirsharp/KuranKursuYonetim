namespace HafizTakip.Core.Utilities.IoC;

/// <summary>
/// Geçerli isteği yapan tenant'ın Id'sini tutar.
/// </summary>
public interface ITenantContext
{
    Guid TenantId { get; }
    void SetTenantId(Guid tenantId);
}
