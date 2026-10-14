using HafizTakip.Business.Concrete;
using HafizTakip.DataAccess.Concrete.EntityFramework;
using HafizTakip.Entities.Dtos;
using HafizTakip.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using static HafizTakip.IntegrationTests.Infrastructure.PostgresDatabaseFixture;

namespace HafizTakip.IntegrationTests.Data;

/// <summary>
/// Çok adımlı yazma akışlarının atomikliği: bir adım veritabanında reddedilirse
/// önceki adımlar da kalıcı olmamalı. Uygulama içi referans kontrolü bilerek devre dışı bırakılır.
/// </summary>
[Collection(PostgresCollection.Name)]
public class AtomicWriteFlowTests
{
    private readonly PostgresDatabaseFixture _db;

    public AtomicWriteFlowTests(PostgresDatabaseFixture db) => _db = db;

    [PostgresFact]
    public async Task Lesson_WhenLessonInsertFails_StudentProgressMustNotChange()
    {
        // Ders tarihi başka testlerle çakışmasın
        var lessonDate = new DateTime(2025, 1, 15, 0, 0, 0, DateTimeKind.Utc);
        var pagesBefore = await _db.ScalarAsync<int>($"SELECT completed_pages FROM students WHERE id = '{StudentA}'");

        await using var context = _db.CreateContext(TenantA);
        var manager = new LessonManager(
            new EfLessonDal(context), new EfStudentDal(context), new EfUserDal(context),
            new TestTenantContext(TenantA), new PermissiveReferenceValidator(),
            new FixedCurrentUser(TeacherA, "head_teacher"), new UnrestrictedAccessScope(),
            new EfUnitOfWork(context), new FakeQuranReference());

        // Hoca başka kursa ait: öğrenci ilerlemesi güncellenir ama ders insert'ü bileşik FK'ye takılır
        var act = () => manager.AddAsync(new CreateLessonDto
        {
            StudentId = StudentA,
            TeacherId = TeacherB,
            LessonDate = lessonDate,
            PageNo = 10,
            IsNewPage = true,
            Performance = "iyi",
            HomeworkStatus = "yapildi"
        });

        await act.Should().ThrowAsync<DbUpdateException>();

        var pagesAfter = await _db.ScalarAsync<int>($"SELECT completed_pages FROM students WHERE id = '{StudentA}'");
        pagesAfter.Should().Be(pagesBefore, "ders yazılamadıysa öğrenci ilerlemesi de yazılmamalı");
    }

    [PostgresFact]
    public async Task BulkAttendance_WhenOneRowFails_NothingIsSaved()
    {
        var date = new DateTime(2025, 2, 20, 0, 0, 0, DateTimeKind.Utc);

        await using var context = _db.CreateContext(TenantA);
        var manager = new AttendanceManager(
            new EfAttendanceDal(context), new EfStudentDal(context), new TestTenantContext(TenantA),
            new PermissiveReferenceValidator(), new UnrestrictedAccessScope(), new EfUnitOfWork(context));

        // İkinci satır başka kursun öğrencisi: bileşik FK reddeder → ilk satır da kaydedilmemeli
        var act = () => manager.AddBulkAsync(new BulkAttendanceDto
        {
            Date = date,
            Attendances = new List<CreateAttendanceItemDto>
            {
                new() { StudentId = StudentA, Status = "present", Notes = "" },
                new() { StudentId = StudentB, Status = "present", Notes = "" }
            }
        });

        await act.Should().ThrowAsync<DbUpdateException>();

        var saved = await _db.ScalarAsync<long>(
            $"SELECT count(*) FROM attendances WHERE date = '{date:yyyy-MM-dd}'");
        saved.Should().Be(0);
    }
}
