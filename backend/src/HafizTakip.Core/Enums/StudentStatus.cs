namespace HafizTakip.Core.Enums;

public static class StudentStatus
{
    public const string Active = "active";
    public const string Archived = "archived";
    public const string Graduated = "graduated";

    public static readonly IReadOnlyCollection<string> All = new[] { Active, Archived, Graduated };
}
