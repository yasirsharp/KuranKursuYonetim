using HafizTakip.Core.Utilities.Results;
using HafizTakip.Entities.Dtos;

namespace HafizTakip.Business.Abstract;

/// <summary>Mushaf referansı: sureler, sayfa → cüz/sure ve cüz → sayfa aralığı (bellekte önbellekli).</summary>
public interface IQuranReferenceService
{
    Task<IDataResult<List<SurahDto>>> GetSurahsAsync();
    Task<IDataResult<QuranPageDto>> GetPageAsync(int page);
    Task<IDataResult<JuzDto>> GetJuzAsync(int juz);

    /// <summary>Sayfanın ilk suresi (ders kaydında sure bilgisi için); geçersiz sayfada null.</summary>
    Task<int?> GetFirstSurahIdOfPageAsync(int page);
}
