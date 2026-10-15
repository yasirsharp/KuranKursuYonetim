using HafizTakip.Core.Constants;
using HafizTakip.IntegrationTests.Infrastructure;

namespace HafizTakip.IntegrationTests.Data;

/// <summary>
/// database/02_quran_reference.sql verisinin değişmezleri. Sure başlangıç sayfalarının mushafa
/// birebir uyumu ayrıca bir hoca tarafından kontrol edilir (database/KURAN_REFERANS_KONTROL.md).
/// </summary>
[Collection(PostgresCollection.Name)]
public class QuranReferenceDataTests
{
    private readonly PostgresDatabaseFixture _db;

    public QuranReferenceDataTests(PostgresDatabaseFixture db) => _db = db;

    [PostgresFact]
    public async Task Surahs_ShouldHave114EntriesAnd6236Ayahs()
    {
        (await _db.ScalarAsync<long>("SELECT count(*) FROM quran_surahs")).Should().Be(QuranConstants.TotalSuras);
        (await _db.ScalarAsync<long>("SELECT sum(ayah_count) FROM quran_surahs")).Should().Be(QuranConstants.TotalAyahs);
    }

    [PostgresFact]
    public async Task SurahStartPages_ShouldBeNonDecreasingAndAnchored()
    {
        var decreasing = await _db.ScalarAsync<long>("""
            SELECT count(*) FROM quran_surahs a JOIN quran_surahs b ON b.id = a.id + 1 WHERE b.start_page < a.start_page
            """);
        decreasing.Should().Be(0);

        (await _db.ScalarAsync<int>("SELECT start_page FROM quran_surahs WHERE id = 1")).Should().Be(1, "Fatiha");
        (await _db.ScalarAsync<int>("SELECT start_page FROM quran_surahs WHERE id = 2")).Should().Be(2, "Bakara");
        (await _db.ScalarAsync<int>("SELECT start_page FROM quran_surahs WHERE id = 114")).Should().Be(604, "Nas");
    }

    [PostgresFact]
    public async Task Pages_ShouldCover604PagesWithFormulaJuz()
    {
        (await _db.ScalarAsync<long>("SELECT count(*) FROM quran_pages")).Should().Be(QuranConstants.TotalPages);

        for (var juz = 1; juz <= QuranConstants.TotalJuz; juz++)
        {
            var start = await _db.ScalarAsync<int>($"SELECT min(page) FROM quran_pages WHERE juz = {juz}");
            var end = await _db.ScalarAsync<int>($"SELECT max(page) FROM quran_pages WHERE juz = {juz}");
            start.Should().Be(QuranConstants.JuzStartPage(juz), $"{juz}. cüzün başlangıcı formülle aynı olmalı");
            end.Should().Be(QuranConstants.JuzEndPage(juz));
        }
    }

    [PostgresFact]
    public async Task LastJuz_ShouldStartWithNebe()
    {
        var firstSurahOfJuz30 = await _db.ScalarAsync<int>(
            $"SELECT first_surah_id FROM quran_pages WHERE page = {QuranConstants.JuzStartPage(30)}");
        firstSurahOfJuz30.Should().Be(78, "Amme cüzü Nebe suresiyle başlar");
    }
}
