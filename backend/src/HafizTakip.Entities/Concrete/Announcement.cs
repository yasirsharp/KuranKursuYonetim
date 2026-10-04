using HafizTakip.Core.Entities;

namespace HafizTakip.Entities.Concrete;

/// <summary>
/// Kurs duyuruları
/// </summary>
public class Announcement : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public Guid AuthorId { get; set; }

    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string TargetAudience { get; set; } = "all"; // all, teachers, parents, group:{uuid}
    public bool IsPinned { get; set; } = false;
    public DateTime? PublishedAt { get; set; }
}
