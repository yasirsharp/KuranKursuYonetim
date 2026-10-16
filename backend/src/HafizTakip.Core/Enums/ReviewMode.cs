namespace HafizTakip.Core.Enums;

/// <summary>Akıllı tekrar planı yöntemi (kurs ayarı).</summary>
public static class ReviewMode
{
    /// <summary>Aralıklı tekrar: sayfa kalitesine göre bir sonraki tekrar tarihi hesaplanır.</summary>
    public const string Spaced = "spaced";

    /// <summary>Döngüsel (geleneksel) tekrar: ezberlenen tüm sayfalar belirli günde bir tur döner.</summary>
    public const string Cyclic = "cyclic";

    public static readonly IReadOnlyCollection<string> All = new[] { Spaced, Cyclic };
}
