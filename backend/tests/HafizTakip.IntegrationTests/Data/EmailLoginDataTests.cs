using HafizTakip.DataAccess.Concrete.EntityFramework;
using HafizTakip.IntegrationTests.Infrastructure;
using static HafizTakip.IntegrationTests.Infrastructure.PostgresDatabaseFixture;

namespace HafizTakip.IntegrationTests.Data;

/// <summary>E-posta ile giriş: adayların kurslar arası aranması ve kurs içinde e-postanın tekilliği.</summary>
[Collection(PostgresCollection.Name)]
public class EmailLoginDataTests
{
    private readonly PostgresDatabaseFixture _db;

    public EmailLoginDataTests(PostgresDatabaseFixture db) => _db = db;

    [PostgresFact]
    public async Task LoginCandidates_ShouldMatchAcrossTenants_CaseInsensitive()
    {
        await _db.ExecuteSqlAsync($"""
            UPDATE users SET email = 'ortak.hoca@kurs.com' WHERE id IN ('{TeacherA}', '{TeacherB}');
            """);

        // Giriş anında istek henüz bir kursa bağlı değil: tenant bağlamı boş
        await using var context = _db.CreateContext(Guid.Empty);
        var candidates = await new EfUserDal(context).GetLoginCandidatesByEmailAsync("ortak.hoca@kurs.com");

        candidates.Select(u => u.Id).Should().BeEquivalentTo(new[] { TeacherA, TeacherB });

        await _db.ExecuteSqlAsync($"UPDATE users SET email = NULL WHERE id IN ('{TeacherA}', '{TeacherB}');");
    }

    [PostgresFact]
    public async Task SameEmailTwiceInOneTenant_ShouldBeRejected()
    {
        var act = () => _db.ExecuteSqlAsync($"""
            INSERT INTO users (tenant_id, first_name, last_name, phone_number, email, role)
            VALUES ('{TenantA}', 'Bir', 'Hoca', '5009990001', 'ayni@kurs.com', 'head_teacher'),
                   ('{TenantA}', 'İki', 'Hoca', '5009990002', 'AYNI@kurs.com', 'head_teacher');
            """);

        await act.Should().ThrowAsync<Npgsql.PostgresException>().Where(e => e.SqlState == "23505");
    }
}
