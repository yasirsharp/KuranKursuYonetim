using HafizTakip.Core.Entities;

namespace HafizTakip.Entities.Dtos;

public class SurahDto : IDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int AyahCount { get; set; }
    public int StartPage { get; set; }

    /// <summary>Surenin bittiği sayfa (sonraki sure aynı sayfada başlıyorsa başlangıç sayfasıyla aynı olabilir).</summary>
    public int EndPage { get; set; }
}

public class QuranPageDto : IDto
{
    public int Page { get; set; }
    public int Juz { get; set; }

    /// <summary>Sayfada yer alan sureler (ilkten sona).</summary>
    public List<SurahDto> Surahs { get; set; } = new();
}

public class JuzDto : IDto
{
    public int Juz { get; set; }
    public int StartPage { get; set; }
    public int EndPage { get; set; }
    public List<SurahDto> Surahs { get; set; } = new();
}
