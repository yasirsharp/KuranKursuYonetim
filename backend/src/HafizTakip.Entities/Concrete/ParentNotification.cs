using HafizTakip.Core.Entities;

namespace HafizTakip.Entities.Concrete;

public class ParentNotification : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public Guid StudentId { get; set; }
    
    public string NotificationType { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string RecipientPhone { get; set; } = string.Empty;
    
    public string DeliveryChannel { get; set; } = "in_app";
    public string DeliveryStatus { get; set; } = "pending";
    public DateTime? SentAt { get; set; }
}
