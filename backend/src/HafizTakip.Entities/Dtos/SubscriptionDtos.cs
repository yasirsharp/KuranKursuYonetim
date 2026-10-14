using HafizTakip.Core.Entities;

namespace HafizTakip.Entities.Dtos;

public class SubscriptionDto : IDto
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public string TenantName { get; set; } = string.Empty;
    public string PlanName { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string Status { get; set; } = string.Empty;
}

public class CreateSubscriptionDto : IDto
{
    public Guid TenantId { get; set; }
    public string PlanName { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
}

public class UpdateSubscriptionDto : IDto
{
    public Guid Id { get; set; }
    public string PlanName { get; set; } = string.Empty;
    public DateTime EndDate { get; set; }
    public string Status { get; set; } = string.Empty;
}

public class SubscriptionFilter : IDto
{
    public Guid? TenantId { get; set; }
}