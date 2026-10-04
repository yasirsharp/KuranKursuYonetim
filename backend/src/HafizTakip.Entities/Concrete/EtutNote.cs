using HafizTakip.Core.Entities;

namespace HafizTakip.Entities.Concrete;

/// <summary>
/// Akşam etüdü notları
/// </summary>
public class EtutNote : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public Guid StudentId { get; set; }
    public Guid TeacherId { get; set; }

    public DateTime NoteDate { get; set; }
    public string HomeworkStatus { get; set; } = "yapılmadı"; // yapıldı, eksik, yapılmadı, mazeretli
    public string? TeacherNotes { get; set; }
}
