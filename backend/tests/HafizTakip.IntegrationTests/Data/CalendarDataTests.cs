using HafizTakip.Business.Concrete;
using HafizTakip.Core.Enums;
using HafizTakip.DataAccess.Concrete.EntityFramework;
using HafizTakip.Entities.Concrete;
using HafizTakip.Entities.Dtos;
using HafizTakip.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using static HafizTakip.IntegrationTests.Infrastructure.PostgresDatabaseFixture;

namespace HafizTakip.IntegrationTests.Data;

/// <summary>Dönem/takvim tabloları: DateOnly eşlemesi, çakışma kısıtı, tatil aktarımı ve ders günü hesabı.</summary>
[Collection(PostgresCollection.Name)]
public class CalendarDataTests
{
    private static readonly DateOnly TermStart = new(2026, 9, 14);
    private static readonly DateOnly TermEnd = new(2027, 8, 28);

    private readonly PostgresDatabaseFixture _db;

    public CalendarDataTests(PostgresDatabaseFixture db) => _db = db;

    private async Task ResetAsync() =>
        await _db.ExecuteSqlAsync("DELETE FROM calendar_events; DELETE FROM academic_terms;");

    private static CalendarManager CreateManager(HafizTakipDbContext context, Guid tenantId) => new(
        new EfAcademicTermDal(context), new EfCalendarEventDal(context), new EfTenantSettingDal(context),
        new TestTenantContext(tenantId), new EfUnitOfWork(context), TimeProvider.System);

    [PostgresFact]
    public async Task OverlappingTerms_ShouldBeRejectedByDatabase_ButOnlyWithinSameTenant()
    {
        await ResetAsync();
        await _db.ExecuteSqlAsync($"""
            INSERT INTO academic_terms (tenant_id, name, start_date, end_date) VALUES ('{TenantA}', 'A1', '2026-09-14', '2027-08-28');
            INSERT INTO academic_terms (tenant_id, name, start_date, end_date) VALUES ('{TenantB}', 'B1', '2026-09-14', '2027-08-28');
            """);

        var overlap = () => _db.ExecuteSqlAsync($"INSERT INTO academic_terms (tenant_id, name, start_date, end_date) VALUES ('{TenantA}', 'A2', '2027-08-28', '2028-06-01');");
        await overlap.Should().ThrowAsync<Npgsql.PostgresException>().Where(e => e.SqlState == "23P01");

        // Silinmiş dönem çakışma sayılmaz
        await _db.ExecuteSqlAsync($"UPDATE academic_terms SET is_deleted = true WHERE tenant_id = '{TenantA}';");
        await _db.ExecuteSqlAsync($"INSERT INTO academic_terms (tenant_id, name, start_date, end_date) VALUES ('{TenantA}', 'A2', '2027-08-28', '2028-06-01');");
    }

    [PostgresFact]
    public async Task AddTerm_ShouldAttachEarlierEventsAndRoundTripDates()
    {
        await ResetAsync();
        await using (var context = _db.CreateContext(TenantA))
        {
            var manager = CreateManager(context, TenantA);
            (await manager.AddEventAsync(new CreateCalendarEventDto
            {
                Title = "Hatim merasimi", EventType = CalendarEventType.Event, StartDate = new DateOnly(2027, 6, 5), EndDate = new DateOnly(2027, 6, 5), IsDayOff = false
            })).Success.Should().BeTrue();

            var added = await manager.AddTermAsync(new CreateAcademicTermDto { Name = "2026-2027", StartDate = TermStart, EndDate = TermEnd });
            added.Success.Should().BeTrue(added.Message);
        }

        await using var readContext = _db.CreateContext(TenantA);
        var term = await readContext.AcademicTerms.SingleAsync();
        term.StartDate.Should().Be(TermStart);
        term.EndDate.Should().Be(TermEnd);
        (await readContext.CalendarEvents.SingleAsync()).TermId.Should().Be(term.Id);
    }

    [PostgresFact]
    public async Task ImportHolidays_ShouldBeIdempotentAndAffectSchoolDays()
    {
        await ResetAsync();
        await using var context = _db.CreateContext(TenantA);
        var manager = CreateManager(context, TenantA);
        (await manager.AddTermAsync(new CreateAcademicTermDto { Name = "2026-2027", StartDate = TermStart, EndDate = TermEnd })).Success.Should().BeTrue();

        var first = await manager.ImportHolidaysAsync(new ImportHolidaysDto { From = TermStart, To = TermEnd });
        first.Success.Should().BeTrue(first.Message);
        first.Data!.Added.Should().BeGreaterThan(5);

        var second = await manager.ImportHolidaysAsync(new ImportHolidaysDto { From = TermStart, To = TermEnd });
        second.Data!.Added.Should().Be(0);
        second.Data.Skipped.Should().Be(first.Data.Added);

        var suggestions = await manager.GetHolidaySuggestionsAsync(TermStart, TermEnd);
        suggestions.Data!.Holidays.Should().OnlyContain(h => h.AlreadyAdded);

        // Ramazan Bayramı haftası: 08.03.2027 Pzt (arife, yarım gün) – 14.03.2027 Paz
        var week = await manager.GetSchoolDaysAsync(new DateOnly(2027, 3, 8), new DateOnly(2027, 3, 14));
        week.Data!.SchoolDays.Should().Equal(new DateOnly(2027, 3, 8), new DateOnly(2027, 3, 12), new DateOnly(2027, 3, 13));
        week.Data.OffDays.Where(d => d.Reason == CalendarEventType.ReligiousHoliday).Should().HaveCount(3);
    }

    [PostgresFact]
    public async Task DeleteTerm_ShouldKeepEventsWithoutTerm()
    {
        await ResetAsync();
        await using var context = _db.CreateContext(TenantA);
        var manager = CreateManager(context, TenantA);
        var term = (await manager.AddTermAsync(new CreateAcademicTermDto { Name = "2026-2027", StartDate = TermStart, EndDate = TermEnd })).Data!;
        await manager.ImportHolidaysAsync(new ImportHolidaysDto { From = TermStart, To = TermEnd, Keys = { "2026-cumhuriyet" } });

        (await manager.DeleteTermAsync(term.Id)).Success.Should().BeTrue();

        var holiday = await _db.ScalarAsync<string>("SELECT COALESCE(term_id::text, 'yok') FROM calendar_events WHERE holiday_key = '2026-cumhuriyet' AND is_deleted = false");
        holiday.Should().Be("yok");
        (await manager.GetTermsAsync()).Data.Should().BeEmpty();
    }

    [PostgresFact]
    public async Task CalendarEvents_ShouldBeIsolatedPerTenant()
    {
        await ResetAsync();
        await _db.ExecuteSqlAsync($"""
            INSERT INTO calendar_events (tenant_id, title, event_type, start_date, end_date)
            VALUES ('{TenantB}', 'B kursu tatili', 'closure', '2026-11-02', '2026-11-03');
            """);

        await using var context = _db.CreateContext(TenantA);
        var events = await CreateManager(context, TenantA).GetEventsAsync(new DateOnly(2026, 11, 1), new DateOnly(2026, 11, 30));
        events.Data.Should().BeEmpty();
    }
}
