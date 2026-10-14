namespace HafizTakip.Core.Enums;

/// <summary>Öğrenci yoklama durumu. Veritabanında kod olarak saklanır.</summary>
public static class AttendanceStatus
{
    public const string Present = "present";   // Geldi
    public const string Absent = "absent";     // Gelmedi
    public const string Late = "late";         // Geç geldi
    public const string Excused = "excused";   // İzinli / mazeretli

    public static readonly IReadOnlyCollection<string> All = new[] { Present, Absent, Late, Excused };
}
