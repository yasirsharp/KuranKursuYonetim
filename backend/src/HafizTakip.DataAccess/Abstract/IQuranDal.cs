using HafizTakip.Entities.Concrete;

namespace HafizTakip.DataAccess.Abstract;

/// <summary>Kur'an referans verisi (salt okunur, tenant filtresi yok).</summary>
public interface IQuranDal
{
    Task<List<QuranSurah>> GetSurahsAsync();
    Task<List<QuranPage>> GetPagesAsync();
}
