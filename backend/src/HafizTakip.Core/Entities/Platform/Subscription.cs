using HafizTakip.Core.Entities;

namespace HafizTakip.Core.Entities.Platform;

public class Subscription : BaseEntity
{
    public Guid TenantId { get; set; }
    
    public string PlanName { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string Status { get; set; } = "active";
}
