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
    
    public string Performance { get; set; } = "İyi"; // Mükemmel, Çok İyi, İyi, Orta, Zayıf
    public bool IsNewPage { get; set; } = true; // Yeni ezber mi, has/tekrar mı?
    public string HomeworkStatus { get; set; } = "yapılmadı"; // yapıldı, eksik, yapılmadı
    public string? Notes { get; set; }
}
