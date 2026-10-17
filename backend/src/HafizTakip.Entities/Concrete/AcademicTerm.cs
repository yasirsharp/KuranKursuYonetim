using HafizTakip.Core.Entities;

namespace HafizTakip.Entities.Concrete;

/// <summary>Eğitim dönemi (ör. 14.09.2026–28.08.2027). Aynı kursun dönemleri çakışamaz.</summary>
public class AcademicTerm : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public string? Notes { get; set; }
}
