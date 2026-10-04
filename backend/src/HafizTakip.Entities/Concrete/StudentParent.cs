using HafizTakip.Core.Entities;

namespace HafizTakip.Entities.Concrete;

public class StudentParent : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public Guid StudentId { get; set; }
    public Guid ParentId { get; set; }
}
