using HafizTakip.Business.Abstract;
using HafizTakip.Core.Constants;
using HafizTakip.Core.Utilities.Results;
using HafizTakip.Entities.Dtos;

namespace HafizTakip.Business.Tests.Common;

/// <summary>Testler için sabit Kur'an referansı: her sayfanın ilk suresi 2 (Bakara) kabul edilir.</summary>
public sealed class FakeQuranReference : IQuranReferenceService
{
    public Task<IDataResult<List<SurahDto>>> GetSurahsAsync() => Task.FromResult<IDataResult<List<SurahDto>>>(new SuccessDataResult<List<SurahDto>>(new List<SurahDto>()));
    public Task<IDataResult<QuranPageDto>> GetPageAsync(int page) => Task.FromResult<IDataResult<QuranPageDto>>(new SuccessDataResult<QuranPageDto>(new QuranPageDto { Page = page, Juz = QuranConstants.JuzOfPage(page) }));
    public Task<IDataResult<JuzDto>> GetJuzAsync(int juz) => Task.FromResult<IDataResult<JuzDto>>(new SuccessDataResult<JuzDto>(new JuzDto { Juz = juz }));
    public Task<int?> GetFirstSurahIdOfPageAsync(int page) => Task.FromResult<int?>(QuranConstants.IsValidPage(page) ? 2 : null);
}