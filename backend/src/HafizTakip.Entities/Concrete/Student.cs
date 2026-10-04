using HafizTakip.Core.Entities;

namespace HafizTakip.Entities.Concrete;

/// <summary>
/// Öğrenci tablosu
/// </summary>
public class Student : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public Guid? GroupId { get; set; } // Hangi grupta/sınıfta
    
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string IdentificationNumber { get; set; } = string.Empty;
    
    // Hafızlık durumu
    public int CurrentPage { get; set; } = 0; // Toplamda nerede? (0-604)
    public int CompletedPages { get; set; } = 0; // Toplam kaç sayfa tamamladı?
    public int TotalPagesToMemorize { get; set; } = 604;
    public bool IsHafiz { get; set; } = false;
    public bool IsBoarder { get; set; } = false;
    
    public string Status { get; set; } = "active"; // active, archived, graduated
}
