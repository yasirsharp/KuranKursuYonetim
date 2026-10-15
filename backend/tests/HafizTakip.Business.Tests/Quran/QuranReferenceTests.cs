using HafizTakip.Business.Concrete;
using HafizTakip.Core.Constants;
using HafizTakip.Core.Utilities.Results;
using HafizTakip.DataAccess.Abstract;
using HafizTakip.Entities.Concrete;
using Microsoft.Extensions.Caching.Memory;

namespace HafizTakip.Business.Tests.Quran;

public class QuranConstantsTests
{
    [Theory]
    [InlineData(1, 1)]
    [InlineData(21, 1)]   // 1. cüz 21 sayfa
    [InlineData(22, 2)]
    [InlineData(41, 2)]
    [InlineData(42, 3)]
    [InlineData(121, 6)]  // 6. cüz s.102-121
    [InlineData(122, 7)]  // ayetberkenar: 7. cüz s.122'de başlar (Medine'de 121)
    [InlineData(581, 29)]
    [InlineData(582, 30)]
    [InlineData(604, 30)] // 30. cüz 23 sayfa
    public void JuzOfPage_ShouldFollowAyetberkenarLayout(int page, int expectedJuz)
    {
        QuranConstants.JuzOfPage(page).Should().Be(expectedJuz);
    }

    [Fact]
    public void JuzRanges_ShouldCoverAllPagesWithoutGapsOrOverlaps()
    {
        var expectedStart = 1;
        for (var juz = 1; juz <= QuranConstants.TotalJuz; juz++)
        {
            QuranConstants.JuzStartPage(juz).Should().Be(expectedStart, $"{juz}. cüz bir önceki cüzün bittiği yerden başlamalı");
            var end = QuranConstants.JuzEndPage(juz);
            for (var page = expectedStart; page <= end; page++)
                QuranConstants.JuzOfPage(page).Should().Be(juz);
            expectedStart = end + 1;
        }
        expectedStart.Should().Be(QuranConstants.TotalPages + 1);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(605)]
    public void JuzOfPage_InvalidPage_ShouldThrow(int page)
    {
        var act = () => QuranConstants.JuzOfPage(page);
        act.Should().Throw<ArgumentOutOfRangeException>();
    }
}

public class QuranReferenceManagerTests
{
    // Küçük bir örnek: s.1 Fatiha, s.2-3 Bakara, s.3'te ayrıca Âl-i İmrân başlıyor
    private readonly List<QuranSurah> _surahs = new()
    {
        new() { Id = 1, NameTr = "Fatiha", AyahCount = 7, StartPage = 1 },
        new() { Id = 2, NameTr = "Bakara", AyahCount = 286, StartPage = 2 },
        new() { Id = 3, NameTr = "Al-i İmran", AyahCount = 200, StartPage = 3 }
    };

    private readonly List<QuranPage> _pages = new()
    {
        new() { Page = 1, Juz = 1, FirstSurahId = 1, LastSurahId = 1 },
        new() { Page = 2, Juz = 1, FirstSurahId = 2, LastSurahId = 2 },
        new() { Page = 3, Juz = 1, FirstSurahId = 3, LastSurahId = 3 }
    };

    private (QuranReferenceManager Manager, Mock<IQuranDal> Dal) Create()
    {
        var dal = new Mock<IQuranDal>();
        dal.Setup(d => d.GetSurahsAsync()).ReturnsAsync(_surahs);
        dal.Setup(d => d.GetPagesAsync()).ReturnsAsync(_pages);
        return (new QuranReferenceManager(dal.Object, new MemoryCache(new MemoryCacheOptions())), dal);
    }

    [Fact]
    public async Task ReferenceData_ShouldBeLoadedOnceAndCached()
    {
        var (manager, dal) = Create();

        await manager.GetSurahsAsync();
        await manager.GetPageAsync(2);
        await manager.GetFirstSurahIdOfPageAsync(3);

        dal.Verify(d => d.GetSurahsAsync(), Times.Once);
        dal.Verify(d => d.GetPagesAsync(), Times.Once);
    }

    [Fact]
    public async Task GetSurahs_ShouldComputeEndPageFromPageTable()
    {
        var (manager, _) = Create();

        var surahs = (await manager.GetSurahsAsync()).Data;

        surahs.Single(s => s.Id == 2).EndPage.Should().Be(2);
        surahs.Single(s => s.Id == 3).EndPage.Should().Be(3);
    }

    [Fact]
    public async Task GetFirstSurahIdOfPage_ShouldReturnSurahStartingOrContinuingOnPage()
    {
        var (manager, _) = Create();

        (await manager.GetFirstSurahIdOfPageAsync(2)).Should().Be(2);
        (await manager.GetFirstSurahIdOfPageAsync(700)).Should().BeNull();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(605)]
    public async Task GetPage_OutOfMushaf_ShouldBeNotFound(int page)
    {
        var (manager, _) = Create();

        (await manager.GetPageAsync(page)).ErrorType.Should().Be(ResultErrorType.NotFound);
    }

    [Fact]
    public async Task EmptyReferenceData_ShouldNotBeCached()
    {
        var dal = new Mock<IQuranDal>();
        dal.Setup(d => d.GetSurahsAsync()).ReturnsAsync(new List<QuranSurah>());
        dal.Setup(d => d.GetPagesAsync()).ReturnsAsync(new List<QuranPage>());
        var manager = new QuranReferenceManager(dal.Object, new MemoryCache(new MemoryCacheOptions()));

        await manager.GetSurahsAsync();
        await manager.GetSurahsAsync();

        dal.Verify(d => d.GetSurahsAsync(), Times.Exactly(2), "referans verisi yüklenmemişse sonraki istekte tekrar denenmeli");
    }
}
