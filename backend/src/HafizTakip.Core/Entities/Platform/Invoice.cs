using HafizTakip.Core.Entities;

namespace HafizTakip.Core.Entities.Platform;

public class Invoice : BaseEntity
{
    public Guid TenantId { get; set; }
    public Guid SubscriptionId { get; set; }
    
    public string InvoiceNo { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public DateTime DueDate { get; set; }
    public DateTime? PaidDate { get; set; }
    public string Status { get; set; } = "draft";
}
