using HafizTakip.Core.Entities;

namespace HafizTakip.Entities.Concrete;

public class RepetitionSession : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public Guid StudentId { get; set; }
    public Guid TeacherId { get; set; }
    
    public DateTime SessionDate { get; set; }
    public int? StartCuz { get; set; }
    public int? EndCuz { get; set; }
    public int? StartPage { get; set; }
    public int? EndPage { get; set; }
    
    public string? Performance { get; set; }
    public string? TeacherNotes { get; set; }
}
