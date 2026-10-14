using HafizTakip.Core.Entities;

namespace HafizTakip.Entities.Dtos;

public class InvoiceDto : IDto
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public string TenantName { get; set; } = string.Empty;
    public Guid SubscriptionId { get; set; }
    public string InvoiceNo { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public DateTime DueDate { get; set; }
    public DateTime? PaidDate { get; set; }
    public string Status { get; set; } = string.Empty;
}

public class CreateInvoiceDto : IDto
{
    public Guid TenantId { get; set; }
    public Guid SubscriptionId { get; set; }
    public string InvoiceNo { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public DateTime DueDate { get; set; }
}

public class UpdateInvoiceStatusDto : IDto
{
    public Guid Id { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime? PaidDate { get; set; }
}

public class InvoiceFilter : IDto
{
    public Guid? TenantId { get; set; }
}