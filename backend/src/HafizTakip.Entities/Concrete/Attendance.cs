using HafizTakip.Core.Entities;

namespace HafizTakip.Entities.Concrete;

public class Attendance : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public Guid StudentId { get; set; }
    
    public DateTime Date { get; set; }
    public string Status { get; set; } = "present"; // present, absent, late, excused
    public string? Notes { get; set; }
}
