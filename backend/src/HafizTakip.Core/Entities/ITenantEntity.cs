namespace HafizTakip.Core.Entities;

/// <summary>
/// Tenant tabanlı entity'ler bu interface'i implement eder.
/// EF Core Global Query Filter, TenantId üzerinden çalışır.
/// </summary>
public interface ITenantEntity : IEntity
{
    Guid TenantId { get; set; }
}
