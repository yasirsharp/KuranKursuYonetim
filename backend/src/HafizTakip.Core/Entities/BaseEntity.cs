namespace HafizTakip.Core.Entities;

/// <summary>
/// Tüm entity'lerin kalıtım aldığı temel sınıf.
/// Ortak alanlar: Id, CreatedAt, UpdatedAt, soft-delete alanları.
/// </summary>
public abstract class BaseEntity : IEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    // Soft delete
    public bool IsDeleted { get; set; } = false;
    public DateTime? DeletedAt { get; set; }
}
