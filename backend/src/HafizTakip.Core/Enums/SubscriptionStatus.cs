namespace HafizTakip.Core.Enums;

public static class SubscriptionStatus
{
    public const string Active = "active";
    public const string Cancelled = "cancelled";
    public const string Expired = "expired";

    public static readonly IReadOnlyCollection<string> All = new[] { Active, Cancelled, Expired };
}
