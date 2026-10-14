using HafizTakip.Business.Abstract;
using HafizTakip.Core.Constants;
using HafizTakip.Core.Utilities.Results;
using HafizTakip.DataAccess.Abstract;
using HafizTakip.Entities.Concrete;
using HafizTakip.Entities.Dtos;
using Microsoft.Extensions.Caching.Memory;

namespace HafizTakip.Business.Concrete;

/// <summary>
/// Kur'an referans verisi değişmez olduğundan ilk kullanımda bir kez yüklenir ve bellekte tutulur;
/// sonraki istekler veritabanına gitmez.
/// </summary>
public class QuranReferenceManager : IQuranReferenceService
{
    private const string CacheKey = "quran:reference";
    private static readonly TimeSpan CacheDuration = TimeSpan.FromHours(12);

    private readonly IQuranDal _quranDal;
    private readonly IMemoryCache _cache;

    public QuranReferenceManager(IQuranDal quranDal, IMemoryCache cache)
    {
        _quranDal = quranDal;
        _cache = cache;
    }

    public async Task<IDataResult<List<SurahDto>>> GetSurahsAsync()
    {
        var reference = await LoadAsync();
        return new SuccessDataResult<List<SurahDto>>(reference.Surahs.ToList(), "Sureler listelendi.");
    }

    public async Task<IDataResult<QuranPageDto>> GetPageAsync(int page)
    {
        if (!QuranConstants.IsValidPage(page))
            return new ErrorDataResult<QuranPageDto>($"Sayfa 1-{QuranConstants.TotalPages} arasında olmalıdır.", ResultErrorType.NotFound);

        var reference = await LoadAsync();
        if (!reference.Pages.TryGetValue(page, out var info))
            return new ErrorDataResult<QuranPageDto>("Kur'an referans verisi yüklenmemiş.", ResultErrorType.NotFound);

        return new SuccessDataResult<QuranPageDto>(new QuranPageDto
        {
            Page = page,
            Juz = info.Juz,
            Surahs = reference.SurahsBetween(info.FirstSurahId, info.LastSurahId)
        });
    }

    public async Task<IDataResult<JuzDto>> GetJuzAsync(int juz)
    {
        if (!QuranConstants.IsValidJuz(juz))
            return new ErrorDataResult<JuzDto>($"Cüz 1-{QuranConstants.TotalJuz} arasında olmalıdır.", ResultErrorType.NotFound);

        var reference = await LoadAsync();
        var startPage = QuranConstants.JuzStartPage(juz);
        var endPage = QuranConstants.JuzEndPage(juz);

        var pagesInJuz = Enumerable.Range(startPage, endPage - startPage + 1)
            .Where(reference.Pages.ContainsKey)
            .Select(p => reference.Pages[p])
            .ToList();

        var surahs = pagesInJuz.Count == 0
            ? new List<SurahDto>()
            : reference.SurahsBetween(pagesInJuz.Min(p => p.FirstSurahId), pagesInJuz.Max(p => p.LastSurahId));

        return new SuccessDataResult<JuzDto>(new JuzDto { Juz = juz, StartPage = startPage, EndPage = endPage, Surahs = surahs });
    }

    public async Task<int?> GetFirstSurahIdOfPageAsync(int page)
    {
        if (!QuranConstants.IsValidPage(page)) return null;
        var reference = await LoadAsync();
        return reference.Pages.TryGetValue(page, out var info) ? info.FirstSurahId : null;
    }

    private async Task<ReferenceData> LoadAsync()
    {
        if (_cache.TryGetValue(CacheKey, out ReferenceData? cached) && cached != null)
            return cached;

        var surahs = await _quranDal.GetSurahsAsync();
        var pages = await _quranDal.GetPagesAsync();
        var data = ReferenceData.Build(surahs, pages);

        // Veri yüklenmemişse (02_quran_reference.sql çalıştırılmamış) boş sonucu önbelleğe alma
        if (data.Surahs.Count > 0)
            _cache.Set(CacheKey, data, CacheDuration);

        return data;
    }

    private sealed class ReferenceData
    {
        public IReadOnlyList<SurahDto> Surahs { get; private init; } = Array.Empty<SurahDto>();
        public IReadOnlyDictionary<int, QuranPage> Pages { get; private init; } = new Dictionary<int, QuranPage>();

        public List<SurahDto> SurahsBetween(int firstId, int lastId) =>
            Surahs.Where(s => s.Id >= firstId && s.Id <= lastId).ToList();

        public static ReferenceData Build(List<QuranSurah> surahs, List<QuranPage> pages)
        {
            // Bitiş sayfası: surenin yer aldığı son sayfa (sayfa tablosundaki ilk/son sure aralığından)
            var endPages = new Dictionary<int, int>();
            foreach (var page in pages)
            {
                for (var id = page.FirstSurahId; id <= page.LastSurahId; id++)
                    endPages[id] = page.Page;
            }

            return new ReferenceData
            {
                Surahs = surahs.Select(s => new SurahDto
                {
                    Id = s.Id,
                    Name = s.NameTr,
                    AyahCount = s.AyahCount,
                    StartPage = s.StartPage,
                    EndPage = endPages.TryGetValue(s.Id, out var end) ? end : s.StartPage
                }).ToList(),
                Pages = pages.ToDictionary(p => p.Page)
            };
        }
    }
}
