namespace HafizTakip.Core.Enums;

/// <summary>Ödev durumu. Veritabanında kod olarak saklanır.</summary>
public static class HomeworkStatus
{
    public const string Yapildi = "yapildi";
    public const string Eksik = "eksik";
    public const string Yapilmadi = "yapilmadi";
    public const string Mazeretli = "mazeretli";

    public static readonly IReadOnlyCollection<string> All = new[] { Yapildi, Eksik, Yapilmadi, Mazeretli };
}
