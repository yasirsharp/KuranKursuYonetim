using System.Linq.Expressions;
using HafizTakip.Business.Abstract;
using HafizTakip.Business.Concrete;
using HafizTakip.Core.Utilities.IoC;
using HafizTakip.Core.Utilities.Results;
using HafizTakip.DataAccess.Abstract;
using HafizTakip.Entities.Concrete;
using HafizTakip.Entities.Dtos;
using HafizTakip.Business.Tests.Common;

namespace HafizTakip.Business.Tests.Security;

/// <summary>
/// İstekle gelen ilişkili kimlikler başka bir kursa aitse (global filtre altında "bulunamadı")
/// yazma işlemi yapılmamalı.
/// </summary>
public class CrossTenantReferenceTests
{
    private static readonly IResult NotFound = new ErrorResult("bulunamadı", ResultErrorType.NotFound);

    private readonly Mock<ITenantReferenceValidator> _references = new();
    private readonly Mock<ITenantContext> _tenantContext = new();
    private readonly Mock<ICurrentUser> _currentUser = new();
    private readonly Guid _currentUserId = Guid.NewGuid();

    public CrossTenantReferenceTests()
    {
        _tenantContext.SetupGet(t => t.TenantId).Returns(Guid.NewGuid());
        _currentUser.SetupGet(c => c.UserId).Returns(_currentUserId);
    }

    [Fact]
    public async Task BulkAttendance_WithForeignStudent_ShouldNotWriteAnything()
    {
        var attendanceDal = new Mock<IAttendanceDal>();
        _references.Setup(r => r.EnsureStudentsAsync(It.IsAny<IReadOnlyCollection<Guid>>())).ReturnsAsync(NotFound);
        var manager = new AttendanceManager(attendanceDal.Object, new Mock<IStudentDal>().Object, _tenantContext.Object, _references.Object,
            AccessScopes.Unrestricted(),
            UnitOfWorks.PassThrough().Object);

        var result = await manager.AddBulkAsync(new BulkAttendanceDto
        {
            Date = DateTime.UtcNow,
            Attendances = new List<CreateAttendanceItemDto>
            {
                new() { StudentId = Guid.NewGuid(), Status = "present" },
                new() { StudentId = Guid.NewGuid(), Status = "absent" }
            }
        });

        result.ErrorType.Should().Be(ResultErrorType.NotFound);
        attendanceDal.Verify(d => d.AddAsync(It.IsAny<HafizTakip.Entities.Concrete.Attendance>()), Times.Never);
        attendanceDal.Verify(d => d.UpdateAsync(It.IsAny<HafizTakip.Entities.Concrete.Attendance>()), Times.Never);
    }

    [Fact]
    public async Task StudentParent_WithForeignParent_ShouldBeRejected()
    {
        var relationDal = new Mock<IStudentParentDal>();
        _references.Setup(r => r.EnsureParentAsync(It.IsAny<Guid>())).ReturnsAsync(NotFound);
        var manager = new StudentParentManager(relationDal.Object, new Mock<IStudentDal>().Object,
            new Mock<IUserDal>().Object, _tenantContext.Object, _references.Object);

        var result = await manager.AddAsync(new CreateStudentParentDto { StudentId = Guid.NewGuid(), ParentId = Guid.NewGuid() });

        result.ErrorType.Should().Be(ResultErrorType.NotFound);
        relationDal.Verify(d => d.AddAsync(It.IsAny<StudentParent>()), Times.Never);
    }

    [Fact]
    public async Task GroupUpdate_WithForeignTeacher_ShouldBeRejected()
    {
        var groupDal = new Mock<IGroupDal>();
        var group = new Group { Id = Guid.NewGuid(), TeacherId = Guid.NewGuid() };
        var originalTeacher = group.TeacherId;
        groupDal.Setup(d => d.GetAsync(It.IsAny<Expression<Func<Group, bool>>>())).ReturnsAsync(group);
        _references.Setup(r => r.EnsureTeacherAsync(It.IsAny<Guid>())).ReturnsAsync(NotFound);
        var manager = new GroupManager(groupDal.Object, new Mock<IUserDal>().Object, _tenantContext.Object, _references.Object);

        var result = await manager.UpdateAsync(new UpdateGroupDto { Id = group.Id, TeacherId = Guid.NewGuid(), Name = "A" });

        result.ErrorType.Should().Be(ResultErrorType.NotFound);
        group.TeacherId.Should().Be(originalTeacher);
        groupDal.Verify(d => d.UpdateAsync(It.IsAny<Group>()), Times.Never);
    }

    [Fact]
    public async Task ExamResult_WithForeignExam_ShouldBeRejected()
    {
        var resultDal = new Mock<IExamResultDal>();
        _references.Setup(r => r.EnsureExamAsync(It.IsAny<Guid>())).ReturnsAsync(NotFound);
        var manager = new ExamResultManager(resultDal.Object, new Mock<IExamDal>().Object,
            new Mock<IStudentDal>().Object, _tenantContext.Object, _references.Object,
            AccessScopes.Unrestricted());

        var result = await manager.AddAsync(new CreateExamResultDto { ExamId = Guid.NewGuid(), StudentId = Guid.NewGuid(), Score = 90 });

        result.ErrorType.Should().Be(ResultErrorType.NotFound);
        resultDal.Verify(d => d.AddAsync(It.IsAny<ExamResult>()), Times.Never);
    }

    [Fact]
    public async Task Lesson_WithoutTeacherId_ShouldUseCurrentUserAsTeacher()
    {
        var (manager, lessonDal) = CreateLessonManager();

        var result = await manager.AddAsync(new CreateLessonDto
        {
            StudentId = Guid.NewGuid(),
            TeacherId = null,
            LessonDate = DateTime.UtcNow,
            PageNo = 5,
            IsNewPage = false
        });

        result.Success.Should().BeTrue();
        _references.Verify(r => r.EnsureTeacherAsync(_currentUserId), Times.Once);
        lessonDal.Verify(d => d.Add(It.Is<Lesson>(l => l.TeacherId == _currentUserId)), Times.Once);
    }

    [Fact]
    public async Task Lesson_WithForeignTeacher_ShouldBeRejected()
    {
        var (manager, lessonDal) = CreateLessonManager();
        _references.Setup(r => r.EnsureTeacherAsync(It.IsAny<Guid>())).ReturnsAsync(NotFound);

        var result = await manager.AddAsync(new CreateLessonDto
        {
            StudentId = Guid.NewGuid(),
            TeacherId = Guid.NewGuid(),
            LessonDate = DateTime.UtcNow,
            PageNo = 5
        });

        result.ErrorType.Should().Be(ResultErrorType.NotFound);
        lessonDal.Verify(d => d.Add(It.IsAny<Lesson>()), Times.Never);
    }

    private (LessonManager Manager, Mock<ILessonDal> LessonDal) CreateLessonManager()
    {
        var lessonDal = new Mock<ILessonDal>();
        var studentDal = new Mock<IStudentDal>();
        lessonDal.Setup(d => d.GetAsync(It.IsAny<Expression<Func<Lesson, bool>>>())).ReturnsAsync((Lesson?)null);
        studentDal.Setup(d => d.GetAsync(It.IsAny<Expression<Func<Student, bool>>>())).ReturnsAsync(new Student { Id = Guid.NewGuid() });
        var manager = new LessonManager(lessonDal.Object, studentDal.Object, new Mock<IUserDal>().Object,
            _tenantContext.Object, _references.Object, _currentUser.Object,
            AccessScopes.Unrestricted(),
            UnitOfWorks.PassThrough().Object, new HafizTakip.Business.Tests.Common.FakeQuranReference());
        return (manager, lessonDal);
    }
}
