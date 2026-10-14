namespace HafizTakip.Core.Enums;

public static class InvoiceStatus
{
    public const string Draft = "draft";
    public const string Pending = "pending";
    public const string Paid = "paid";
    public const string Cancelled = "cancelled";

    public static readonly IReadOnlyCollection<string> All = new[] { Draft, Pending, Paid, Cancelled };
}
