using HafizTakip.Entities.Concrete;
using HafizTakip.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using static HafizTakip.IntegrationTests.Infrastructure.PostgresDatabaseFixture;

namespace HafizTakip.IntegrationTests.Data;

[Collection(PostgresCollection.Name)]
public class TenantSettingsDataTests
{
    private readonly PostgresDatabaseFixture _db;

    public TenantSettingsDataTests(PostgresDatabaseFixture db) => _db = db;

    [PostgresFact]
    public async Task Settings_ShouldRoundTripArrayAndTimeColumns()
    {
        await using (var context = _db.CreateContext(TenantB))
        {
            context.TenantSettings.RemoveRange(context.TenantSettings);
            await context.SaveChangesAsync();
            context.TenantSettings.Add(new TenantSetting
            {
                WorkingDays = new[] { 1, 3, 5 },
                DayStartTime = new TimeOnly(9, 15),
                DayEndTime = new TimeOnly(15, 45),
                EtutStartTime = new TimeOnly(19, 0),
                ReviewMode = "cyclic"
            });
            await context.SaveChangesAsync();
        }

        await using var readContext = _db.CreateContext(TenantB);
        var stored = await readContext.TenantSettings.SingleAsync();
        stored.TenantId.Should().Be(TenantB);
        stored.WorkingDays.Should().Equal(1, 3, 5);
        stored.DayStartTime.Should().Be(new TimeOnly(9, 15));
        stored.EtutStartTime.Should().Be(new TimeOnly(19, 0));
    }

    [PostgresFact]
    public async Task Settings_ShouldBeOnePerTenant()
    {
        // Fixture iki kursu doğrudan SQL ile ekler; A kursuna iki ayar satırı eklenemez
        await _db.ExecuteSqlAsync($"DELETE FROM tenant_settings WHERE tenant_id = '{TenantA}'; INSERT INTO tenant_settings (tenant_id) VALUES ('{TenantA}');");

        var act = () => _db.ExecuteSqlAsync($"INSERT INTO tenant_settings (tenant_id) VALUES ('{TenantA}');");

        await act.Should().ThrowAsync<Npgsql.PostgresException>().Where(e => e.SqlState == "23505");
    }
}
