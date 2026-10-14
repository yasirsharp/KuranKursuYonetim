using HafizTakip.Core.Utilities.IoC;

namespace HafizTakip.IntegrationTests.Infrastructure;

/// <summary>Testlerde kurs bağlamını serbestçe ayarlamak için (boş bağlam dahil).</summary>
public sealed class TestTenantContext : ITenantContext
{
    public TestTenantContext(Guid tenantId) => TenantId = tenantId;

    public Guid TenantId { get; private set; }

    public void SetTenantId(Guid tenantId) => TenantId = tenantId;
}
