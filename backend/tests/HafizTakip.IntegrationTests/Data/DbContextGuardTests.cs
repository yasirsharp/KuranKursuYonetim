using HafizTakip.Entities.Concrete;
using HafizTakip.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using static HafizTakip.IntegrationTests.Infrastructure.PostgresDatabaseFixture;

namespace HafizTakip.IntegrationTests.Data;

/// <summary>HafizTakipDbContext.SaveChanges güvenceleri: tenant damgası, kurslar arası yazma, soft delete.</summary>
[Collection(PostgresCollection.Name)]
public class DbContextGuardTests
{
    private readonly PostgresDatabaseFixture _db;

    public DbContextGuardTests(PostgresDatabaseFixture db) => _db = db;

    [PostgresFact]
    public async Task Add_WithoutTenantId_ShouldStampCurrentTenant()
    {
        var id = Guid.NewGuid();
        await using (var context = _db.CreateContext(TenantA))
        {
            context.Attendances.Add(new Attendance { Id = id, StudentId = StudentA, Date = DateTime.UtcNow.Date, Status = "present" });
            await context.SaveChangesAsync();
        }

        var storedTenant = await _db.ScalarAsync<Guid>($"SELECT tenant_id FROM attendances WHERE id = '{id}'");
        storedTenant.Should().Be(TenantA);
    }

    [PostgresFact]
    public async Task Add_ForAnotherTenant_ShouldBeRejected()
    {
        await using var context = _db.CreateContext(TenantA);
        context.Attendances.Add(new Attendance { TenantId = TenantB, StudentId = StudentB, Date = DateTime.UtcNow.Date.AddDays(-30), Status = "present" });

        var act = () => context.SaveChangesAsync();

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*başka bir kursa*");
    }

    [PostgresFact]
    public async Task ChangingTenantId_ShouldBeRejected()
    {
        await using var context = _db.CreateContext(TenantA);
        var student = await context.Students.SingleAsync(s => s.Id == StudentA);

        student.TenantId = TenantB;
        var act = () => context.SaveChangesAsync();

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*değiştirilemez*");
    }

    [PostgresFact]
    public async Task Remove_ShouldSoftDeleteAndHideFromQueries()
    {
        var id = Guid.NewGuid();
        await using (var context = _db.CreateContext(TenantA))
        {
            context.EtutNotes.Add(new EtutNote { Id = id, StudentId = StudentA, TeacherId = TeacherA, NoteDate = DateTime.UtcNow.Date, HomeworkStatus = "yapildi" });
            await context.SaveChangesAsync();
        }

        await using (var context = _db.CreateContext(TenantA))
        {
            var note = await context.EtutNotes.SingleAsync(n => n.Id == id);
            context.EtutNotes.Remove(note);
            await context.SaveChangesAsync();
        }

        await using (var context = _db.CreateContext(TenantA))
        {
            (await context.EtutNotes.AnyAsync(n => n.Id == id)).Should().BeFalse("silinen kayıt sorgularda görünmemeli");
        }

        var isDeleted = await _db.ScalarAsync<bool>($"SELECT is_deleted FROM etut_notes WHERE id = '{id}'");
        isDeleted.Should().BeTrue("satır fiziksel olarak silinmemeli");
    }

    [PostgresFact]
    public async Task Queries_ShouldOnlySeeCurrentTenantRows()
    {
        await using var contextA = _db.CreateContext(TenantA);
        await using var contextB = _db.CreateContext(TenantB);

        (await contextA.Students.Select(s => s.Id).ToListAsync()).Should().Contain(StudentA).And.NotContain(StudentB);
        (await contextB.Students.Select(s => s.Id).ToListAsync()).Should().Contain(StudentB).And.NotContain(StudentA);
    }

    [PostgresFact]
    public async Task DailyAssignment_ShouldPersistJsonbTaskLists()
    {
        var id = Guid.NewGuid();
        await using (var context = _db.CreateContext(TenantA))
        {
            context.DailyAssignments.Add(new DailyAssignment
            {
                Id = id,
                StudentId = StudentA,
                TeacherId = TeacherA,
                AssignmentDate = DateTime.UtcNow.Date,
                AssignedTasks = """[{"title":"Sayfa 12 tekrar"}]"""
            });
            await context.SaveChangesAsync();
        }

        var jsonType = await _db.ScalarAsync<string>($"SELECT jsonb_typeof(assigned_tasks) FROM daily_assignments WHERE id = '{id}'");
        jsonType.Should().Be("array");

        await using var readContext = _db.CreateContext(TenantA);
        var stored = await readContext.DailyAssignments.SingleAsync(d => d.Id == id);
        stored.AssignedTasks.Should().Contain("Sayfa 12 tekrar");
    }
}
