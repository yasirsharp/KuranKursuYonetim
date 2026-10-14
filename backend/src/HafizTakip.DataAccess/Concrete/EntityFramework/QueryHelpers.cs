namespace HafizTakip.DataAccess.Concrete.EntityFramework;

internal static class QueryHelpers
{
    public const string Unknown = "Bilinmiyor";

    /// <summary>ILIKE desenindeki özel karakterleri (\ % _) kaçırır; kullanıcı girdisi joker karakter olarak yorumlanmaz.</summary>
    public static string EscapeLike(string value) =>
        value.Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_");
}
