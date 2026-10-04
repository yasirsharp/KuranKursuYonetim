using HafizTakip.Core.Entities;

namespace HafizTakip.Core.Entities.Platform;

public class Subscription : IEntity
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    
    public string PlanName { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string Status { get; set; } = "active";
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public bool IsDeleted { get; set; } = false;
    public DateTime? DeletedAt { get; set; }
}
