using HafizTakip.Core.Entities;

namespace HafizTakip.Entities.Concrete;

public class DailyAssignment : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public Guid StudentId { get; set; }
    public Guid TeacherId { get; set; }
    
    public DateTime AssignmentDate { get; set; }
    public bool IsBoarderToday { get; set; }
    
    public string AssignedTasks { get; set; } = "[]";
    public string CompletedInEtut { get; set; } = "[]";
    public string CompletedAtHome { get; set; } = "[]";
    
    public string Status { get; set; } = "assigned";
    public string? EtutTeacherNote { get; set; }
    public string? ParentNote { get; set; }
}
