using HafizTakip.Core.Entities;

namespace HafizTakip.Entities.Concrete;

/// <summary>
/// Sınıf / Grup tablosu
/// </summary>
public class Group : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public Guid TeacherId { get; set; } // User tablosuna FK
    
    public string Name { get; set; } = string.Empty; // Örn: 1. Sınıf, Hızlı Grup
    public string? Description { get; set; }
}
