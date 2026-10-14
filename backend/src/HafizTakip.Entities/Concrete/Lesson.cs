using HafizTakip.Core.Entities;

namespace HafizTakip.Entities.Concrete;

/// <summary>
/// Hafızlık ders/sayfa kayıt tablosu
/// </summary>
public class Lesson : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public Guid StudentId { get; set; }
    public Guid TeacherId { get; set; }
    
    public DateTime LessonDate { get; set; }
    
    // Okunan yer
    public int SuraId { get; set; }
    public int JuzNumber { get; set; }
    public int PageNumber { get; set; } // O gün okuduğu spesifik sayfa
    
    public string Performance { get; set; } = "iyi"; // PerformanceLevel: mukemmel, cok_iyi, iyi, orta, zayif
    public bool IsNewPage { get; set; } = true; // Yeni ezber mi, has/tekrar mı?
    public string HomeworkStatus { get; set; } = "yapilmadi"; // HomeworkStatus: yapildi, eksik, yapilmadi, mazeretli
    public string? Notes { get; set; }
}
