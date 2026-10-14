using HafizTakip.Core.Entities;

namespace HafizTakip.Entities.Concrete;

/// <summary>Sure referans verisi (platform seviyesi, salt okunur; database/02_quran_reference.sql).</summary>
public class QuranSurah : IEntity
{
    public int Id { get; set; }
    public string NameTr { get; set; } = string.Empty;
    public int AyahCount { get; set; }
    public int StartPage { get; set; }
}
