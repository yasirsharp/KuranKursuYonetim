using HafizTakip.Core.Entities;

namespace HafizTakip.Entities.Concrete;

/// <summary>
/// Öğrenci bazlı sınav sonucu
/// </summary>
public class ExamResult : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public Guid ExamId { get; set; }
    public Guid StudentId { get; set; }

    public decimal? Score { get; set; }
    public string? Grade { get; set; } // Mükemmel, Çok İyi, İyi, Orta, Zayıf
    public string? TeacherNotes { get; set; }
}
