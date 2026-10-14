using HafizTakip.Core.Entities;

namespace HafizTakip.Entities.Concrete;

/// <summary>Mushaf sayfası referans verisi: cüz ve sayfadaki ilk/son sure (salt okunur).</summary>
public class QuranPage : IEntity
{
    public int Page { get; set; }
    public int Juz { get; set; }
    public int FirstSurahId { get; set; }
    public int LastSurahId { get; set; }
}
