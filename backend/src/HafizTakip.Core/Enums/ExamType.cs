namespace HafizTakip.Core.Enums;

/// <summary>Sınav türü. Veritabanında kod olarak saklanır.</summary>
public static class ExamType
{
    public const string Hafizlik = "hafizlik";
    public const string Tecvid = "tecvid";
    public const string Okuma = "okuma";
    public const string Deneme = "deneme";

    public static readonly IReadOnlyCollection<string> All = new[] { Hafizlik, Tecvid, Okuma, Deneme };
}
