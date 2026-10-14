namespace HafizTakip.Core.Enums;

/// <summary>
/// Ders performans değerlendirmesi (5 seviye). Puan (1–5) akıllı tekrar algoritması ve
/// raporlamada kalite ölçüsü olarak kullanılır.
/// </summary>
public static class PerformanceLevel
{
    public const string Mukemmel = "mukemmel";
    public const string CokIyi = "cok_iyi";
    public const string Iyi = "iyi";
    public const string Orta = "orta";
    public const string Zayif = "zayif";

    public static readonly IReadOnlyCollection<string> All = new[] { Mukemmel, CokIyi, Iyi, Orta, Zayif };

    private static readonly Dictionary<string, (int Score, string Label)> Levels = new()
    {
        [Mukemmel] = (5, "Mükemmel"),
        [CokIyi] = (4, "Çok İyi"),
        [Iyi] = (3, "İyi"),
        [Orta] = (2, "Orta"),
        [Zayif] = (1, "Zayıf")
    };

    /// <summary>1–5 arası puan; bilinmeyen kod için null.</summary>
    public static int? ScoreOf(string? code) =>
        code != null && Levels.TryGetValue(code, out var level) ? level.Score : null;

    /// <summary>Türkçe etiket; bilinmeyen kod için kodun kendisi.</summary>
    public static string LabelOf(string? code) =>
        code != null && Levels.TryGetValue(code, out var level) ? level.Label : code ?? string.Empty;
}
