namespace HafizTakip.Core.Enums;

/// <summary>Davetle gelen katılım isteğinin durumu.</summary>
public static class JoinRequestStatus
{
    public const string Pending = "pending";
    public const string Approved = "approved";
    public const string Rejected = "rejected";

    public static readonly IReadOnlyCollection<string> All = new[] { Pending, Approved, Rejected };
}
