namespace HafizTakip.Core.Enums;

/// <summary>Günlük görev (ödev) akışının durumu: hoca → etüt → veli → tamamlandı.</summary>
public static class AssignmentStatus
{
    public const string Assigned = "assigned";            // Etüt hocasında (yatılı öğrenci)
    public const string SentToParent = "sent_to_parent";  // Evde tamamlanmak üzere veliye gönderildi
    public const string Completed = "completed";

    public static readonly IReadOnlyCollection<string> All = new[] { Assigned, SentToParent, Completed };
}
