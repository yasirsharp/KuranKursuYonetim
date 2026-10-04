using HafizTakip.Core.Entities;

namespace HafizTakip.Entities.Concrete;

/// <summary>
/// Sınav tanımı (hafizlik, tecvid, okuma)
/// </summary>
public class Exam : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public Guid GroupId { get; set; }

    public string ExamType { get; set; } = "hafizlik"; // hafizlik, tecvid, okuma
    public DateTime ExamDate { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
}
