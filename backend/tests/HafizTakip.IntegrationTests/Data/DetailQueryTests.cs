using HafizTakip.Core.Utilities.Requests;
using HafizTakip.DataAccess.Concrete.EntityFramework;
using HafizTakip.Entities.Dtos;
using HafizTakip.IntegrationTests.Infrastructure;
using static HafizTakip.IntegrationTests.Infrastructure.PostgresDatabaseFixture;

namespace HafizTakip.IntegrationTests.Data;

/// <summary>
/// DAL detay sorguları: sayfalama veritabanında, adlar join ile, veli kapsamı uygulanıyor ve
/// satır sayısından bağımsız olarak sabit sayıda SQL komutu çalışıyor (N+1 regresyon testi).
/// </summary>
[Collection(PostgresCollection.Name)]
public class DetailQueryTests
{
    private readonly PostgresDatabaseFixture _db;

    public DetailQueryTests(PostgresDatabaseFixture db) => _db = db;

    private async Task InsertLessonsAsync(Guid studentId, int count)
    {
        for (var i = 0; i < count; i++)
        {
            await _db.ExecuteSqlAsync($"""
                INSERT INTO lessons (tenant_id, student_id, teacher_id, lesson_date, sura_id, juz_number, page_number, performance, homework_status)
                VALUES ('{TenantA}', '{studentId}', '{TeacherA}', DATE '2024-01-01' + {i}, 1, 1, {i + 1}, 'iyi', 'yapildi');
                """);
        }
    }

    [PostgresFact]
    public async Task Lessons_ShouldPageInDatabaseWithNamesNewestFirst_InTwoCommands()
    {
        var studentId = await _db.CreateStudentAsync("Sayfa", "Testi");
        await InsertLessonsAsync(studentId, 25);

        var counter = new CommandCounter();
        await using var context = _db.CreateContext(new TestTenantContext(TenantA), counter);
        var dal = new EfLessonDal(context);

        var page = await dal.GetLessonDetailsPagedAsync(
            new LessonFilter { StudentId = studentId }, null, new PaginationParams { Page = 2, PageSize = 10 });

        page.TotalCount.Should().Be(25);
        page.Data.Should().HaveCount(10);
        page.Data.Select(l => l.LessonDate).Should().BeInDescendingOrder();
        page.Data.First().PageNo.Should().Be(15, "2. sayfanın ilk kaydı en yeni 11. ders olmalı (25, 24, ... 16 | 15 ...)");
        page.Data.Should().OnlyContain(l => l.StudentName == "Sayfa Testi" && l.TeacherName == "Hoca A");
        counter.Count.Should().Be(2, "yalnızca COUNT ve sayfa sorgusu çalışmalı; satır başına sorgu olmamalı");
    }

    [PostgresFact]
    public async Task Lessons_DateRangeFilter_ShouldBeApplied()
    {
        var studentId = await _db.CreateStudentAsync();
        await InsertLessonsAsync(studentId, 10); // 2024-01-01 … 2024-01-10

        await using var context = _db.CreateContext(TenantA);
        var page = await new EfLessonDal(context).GetLessonDetailsPagedAsync(
            new LessonFilter { StudentId = studentId, From = new DateTime(2024, 1, 3), To = new DateTime(2024, 1, 5) },
            null, new PaginationParams());

        page.TotalCount.Should().Be(3);
    }

    [PostgresFact]
    public async Task ParentScope_ShouldLimitRowsInDatabase()
    {
        var ownChild = await _db.CreateStudentAsync("Kendi", "Cocugu");
        var otherChild = await _db.CreateStudentAsync("Baska", "Cocuk");
        await InsertLessonsAsync(ownChild, 2);
        await InsertLessonsAsync(otherChild, 3);

        await using var context = _db.CreateContext(TenantA);
        var dal = new EfLessonDal(context);

        var scoped = await dal.GetLessonDetailsPagedAsync(new LessonFilter(), new[] { ownChild }, new PaginationParams { PageSize = 100 });
        scoped.Data.Should().OnlyContain(l => l.StudentId == ownChild);
        scoped.TotalCount.Should().Be(2);

        var empty = await dal.GetLessonDetailsPagedAsync(new LessonFilter(), Array.Empty<Guid>(), new PaginationParams());
        empty.TotalCount.Should().Be(0, "bağlı çocuğu olmayan veli hiçbir dersi görmemeli");
    }

    [PostgresFact]
    public async Task Students_SearchAndGroupFilter_ShouldWorkWithGroupName()
    {
        var groupId = Guid.NewGuid();
        await _db.ExecuteSqlAsync($"INSERT INTO groups (id, tenant_id, teacher_id, name) VALUES ('{groupId}', '{TenantA}', '{TeacherA}', 'Arama Grubu');");
        var target = await _db.CreateStudentAsync("Zübeyde", "Aramalı", groupId);
        await _db.CreateStudentAsync("Zübeyde", "Grupsuz");

        var counter = new CommandCounter();
        await using var context = _db.CreateContext(new TestTenantContext(TenantA), counter);
        var dal = new EfStudentDal(context);

        var bySearch = await dal.GetStudentDetailsPagedAsync(new StudentFilter { Search = "zübeyde ara" }, null, new PaginationParams());
        bySearch.Data.Should().ContainSingle(s => s.Id == target).Which.GroupName.Should().Be("Arama Grubu");

        var byGroup = await dal.GetStudentDetailsPagedAsync(new StudentFilter { GroupId = groupId }, null, new PaginationParams());
        byGroup.Data.Should().ContainSingle().Which.Id.Should().Be(target);

        var likeWildcard = await dal.GetStudentDetailsPagedAsync(new StudentFilter { Search = "%" }, null, new PaginationParams());
        likeWildcard.TotalCount.Should().Be(0, "kullanıcı girdisindeki % joker karakter olarak yorumlanmamalı");

        counter.Count.Should().Be(6, "üç sorgunun her biri COUNT + sayfa = 2 komut");
    }

    [PostgresFact]
    public async Task DailyAssignments_ShouldParseJsonAfterPaging()
    {
        var studentId = await _db.CreateStudentAsync();
        await _db.ExecuteSqlAsync($$"""
            INSERT INTO daily_assignments (tenant_id, student_id, teacher_id, assignment_date, is_boarder_today, assigned_tasks, status)
            VALUES ('{{TenantA}}', '{{studentId}}', '{{TeacherA}}', DATE '2024-03-01', true,
                    '[{"Id":"t1","Type":"ezber","Cuz":1,"Page":5}]', 'assigned');
            """);

        var counter = new CommandCounter();
        await using var context = _db.CreateContext(new TestTenantContext(TenantA), counter);
        var page = await new EfDailyAssignmentDal(context).GetAssignmentDetailsPagedAsync(
            new DailyAssignmentFilter { Date = new DateTime(2024, 3, 1), IsBoarderToday = true, Status = "assigned", StudentId = studentId },
            null, new PaginationParams());

        page.TotalCount.Should().Be(1);
        var dto = page.Data.Single();
        dto.AssignedTasks.Should().ContainSingle().Which.Page.Should().Be(5);
        dto.TeacherName.Should().Be("Hoca A");
        counter.Count.Should().Be(2);
    }

    [PostgresFact]
    public async Task OtherDetailQueries_ShouldRunInConstantCommandCount()
    {
        var studentId = await _db.CreateStudentAsync();
        await _db.ExecuteSqlAsync($"INSERT INTO groups (tenant_id, teacher_id, name) VALUES ('{TenantA}', '{TeacherA}', 'Sayaç Grubu');");
        for (var i = 0; i < 5; i++)
        {
            await _db.ExecuteSqlAsync($"""
                INSERT INTO attendances (tenant_id, student_id, date, status) VALUES ('{TenantA}', '{studentId}', DATE '2023-06-01' + {i}, 'present');
                INSERT INTO etut_notes (tenant_id, student_id, teacher_id, note_date, homework_status) VALUES ('{TenantA}', '{studentId}', '{TeacherA}', DATE '2023-06-01' + {i}, 'yapildi');
                """);
        }

        var counter = new CommandCounter();
        await using var context = _db.CreateContext(new TestTenantContext(TenantA), counter);

        var attendance = await new EfAttendanceDal(context).GetAttendanceDetailsPagedAsync(new AttendanceFilter { StudentId = studentId }, null, new PaginationParams());
        attendance.TotalCount.Should().Be(5);
        attendance.Data.Should().OnlyContain(a => a.StudentName == "Test Öğrenci");

        var notes = await new EfEtutNoteDal(context).GetEtutNoteDetailsPagedAsync(new EtutNoteFilter { StudentId = studentId }, null, new PaginationParams());
        notes.TotalCount.Should().Be(5);
        notes.Data.Should().OnlyContain(n => n.TeacherName == "Hoca A");

        var groups = await new EfGroupDal(context).GetGroupDetailsPagedAsync(new PaginationParams());
        groups.Data.Should().NotBeEmpty();

        counter.Count.Should().Be(6, "her liste COUNT + sayfa = 2 komutla gelmeli");
    }
}
