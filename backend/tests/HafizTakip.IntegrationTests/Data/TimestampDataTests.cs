using HafizTakip.Core.Entities.Platform;
using HafizTakip.Entities.Concrete;
using HafizTakip.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using static HafizTakip.IntegrationTests.Infrastructure.PostgresDatabaseFixture;

namespace HafizTakip.IntegrationTests.Data;

/// <summary>
/// Zaman damgaları veritabanına gerçek UTC anı olarak yazılmalı (eskiden sunucu saat dilimine göre
/// 3 saat kayıyordu) ve tarih kolonları gün olarak kalmalı.
/// </summary>
[Collection(PostgresCollection.Name)]
public class TimestampDataTests
{
    private readonly PostgresDatabaseFixture _db;

    public TimestampDataTests(PostgresDatabaseFixture db) => _db = db;

    [PostgresFact]
    public async Task UtcTimestamp_ShouldBeStoredAsTheSameInstant()
    {
        // Oturumun saat dilimi İstanbul olsa da yazılan an değişmemeli
        // (geçici test veritabanı; yeni bağlantılar bu saat dilimiyle açılır)
        await _db.ExecuteSqlAsync(
            "DO $$ BEGIN EXECUTE format('ALTER DATABASE %I SET timezone TO %L', current_database(), 'Europe/Istanbul'); END $$;");
        var submitted = new DateTime(2026, 10, 16, 11, 29, 0, DateTimeKind.Utc);
        var tenant = new Tenant { Name = "Saat Testi", Slug = "saat-testi-" + Guid.NewGuid().ToString("N")[..8], Status = "pending", SubmittedAt = submitted };

        await using (var context = _db.CreateContext(Guid.Empty))
        {
            context.Tenants.Add(tenant);
            await context.SaveChangesAsync();
        }

        (await _db.ScalarAsync<string>(
                $"SELECT to_char(submitted_at AT TIME ZONE 'UTC', 'YYYY-MM-DD HH24:MI') FROM tenants WHERE id = '{tenant.Id}'"))
            .Should().Be("2026-10-16 11:29");

        // Veritabanının kendi saatiyle (now()) karşılaştırma tutarlı olmalı: az önce yazılan created_at "şimdi"ye yakın
        (await _db.ScalarAsync<bool>(
                $"SELECT abs(extract(epoch FROM now() - created_at)) < 120 FROM tenants WHERE id = '{tenant.Id}'"))
            .Should().BeTrue("created_at sunucu saatine göre kaydırılmamalı");

        await using var readContext = _db.CreateContext(Guid.Empty);
        var stored = await readContext.Tenants.SingleAsync(t => t.Id == tenant.Id);
        stored.SubmittedAt.Should().Be(submitted);
        stored.SubmittedAt!.Value.Kind.Should().Be(DateTimeKind.Utc);
    }

    [PostgresFact]
    public async Task UnspecifiedKind_ShouldBeTreatedAsUtc()
    {
        var unspecified = new DateTime(2026, 1, 2, 8, 0, 0, DateTimeKind.Unspecified);
        var tenant = new Tenant { Name = "Belirsiz Saat", Slug = "belirsiz-" + Guid.NewGuid().ToString("N")[..8], ReviewedAt = unspecified };

        await using (var context = _db.CreateContext(Guid.Empty))
        {
            context.Tenants.Add(tenant);
            await context.SaveChangesAsync();
        }

        (await _db.ScalarAsync<string>(
                $"SELECT to_char(reviewed_at AT TIME ZONE 'UTC', 'HH24:MI') FROM tenants WHERE id = '{tenant.Id}'"))
            .Should().Be("08:00");
    }

    [PostgresFact]
    public async Task CalendarDate_ShouldKeepTheDay()
    {
        var studentId = await _db.CreateStudentAsync("Tarih", "Testi");
        var day = new DateTime(2025, 3, 9, 0, 0, 0, DateTimeKind.Utc);

        await using (var context = _db.CreateContext(TenantA))
        {
            context.Attendances.Add(new Attendance { StudentId = studentId, Date = day, Status = "present" });
            await context.SaveChangesAsync();
        }

        (await _db.ScalarAsync<string>($"SELECT to_char(date, 'YYYY-MM-DD') FROM attendances WHERE student_id = '{studentId}'"))
            .Should().Be("2025-03-09");

        await using var readContext = _db.CreateContext(TenantA);
        var found = await readContext.Attendances.CountAsync(a => a.StudentId == studentId && a.Date == day);
        found.Should().Be(1, "UTC gece yarısıyla yapılan gün karşılaştırması aynı günü bulmalı");
    }

    [Fact]
    public void Model_DateTimeColumnTypes_ShouldMatchSchemaConvention()
    {
        using var context = new HafizTakip.DataAccess.Concrete.EntityFramework.HafizTakipDbContext(
            new DbContextOptionsBuilder<HafizTakip.DataAccess.Concrete.EntityFramework.HafizTakipDbContext>()
                .UseNpgsql("Host=offline;Database=model_only").UseSnakeCaseNamingConvention().Options,
            new TestTenantContext(Guid.NewGuid()));

        var columns = context.Model.GetEntityTypes()
            .SelectMany(t => t.GetProperties())
            .Where(p => p.ClrType == typeof(DateTime) || p.ClrType == typeof(DateTime?))
            .ToDictionary(p => $"{p.DeclaringType.ClrType.Name}.{p.Name}", p => p.GetColumnType());

        columns["Lesson.LessonDate"].Should().Be("date");
        columns["Attendance.Date"].Should().Be("date");
        columns["Subscription.EndDate"].Should().Be("date");
        columns["Tenant.SubmittedAt"].Should().Be("timestamp with time zone");
        columns["RefreshToken.ExpiresAt"].Should().Be("timestamp with time zone");
        columns.Where(c => c.Key.EndsWith(".CreatedAt")).Should().OnlyContain(c => c.Value == "timestamp with time zone");
    }
}
