using System.Linq.Expressions;
using HafizTakip.Business.Concrete;
using HafizTakip.Core.Entities;
using HafizTakip.Core.Utilities.IoC;
using HafizTakip.Core.Utilities.Requests;
using HafizTakip.DataAccess.Abstract;
using HafizTakip.Entities.Concrete;
using HafizTakip.Entities.Dtos;

namespace HafizTakip.Business.Tests.Lessons;

public class LessonManagerTests
{
    private readonly Mock<ILessonDal> _lessonDalMock;
    private readonly Mock<IStudentDal> _studentDalMock;
    private readonly Mock<IUserDal> _userDalMock;
    private readonly Mock<ITenantContext> _tenantContextMock;
    private readonly LessonManager _lessonManager;
    private readonly Guid _testTenantId;

    public LessonManagerTests()
    {
        _lessonDalMock = new Mock<ILessonDal>();
        _studentDalMock = new Mock<IStudentDal>();
        _userDalMock = new Mock<IUserDal>();
        _tenantContextMock = new Mock<ITenantContext>();
        _testTenantId = Guid.NewGuid();

        _tenantContextMock.SetupGet(t => t.TenantId).Returns(_testTenantId);

        _lessonManager = new LessonManager(
            _lessonDalMock.Object,
            _studentDalMock.Object,
            _userDalMock.Object,
            _tenantContextMock.Object);
    }

    [Fact]
    public async Task AddAsync_WhenValidDto_ShouldSaveLessonAndAdvanceStudentProgress()
    {
        // Arrange
        var studentId = Guid.NewGuid();
        var teacherId = Guid.NewGuid();
        var lessonDate = new DateTime(2026, 10, 12);

        var student = new Student
        {
            Id = studentId,
            TenantId = _testTenantId,
            FirstName = "Ali",
            LastName = "Kaya",
            CurrentPage = 15,
            CompletedPages = 10
        };

        var dto = new CreateLessonDto
        {
            StudentId = studentId,
            TeacherId = teacherId,
            LessonDate = lessonDate,
            CuzNo = 1,
            PageNo = 16,
            IsNewPage = true,
            Performance = "iyi"
        };

        _lessonDalMock
            .Setup(d => d.GetAsync(It.IsAny<Expression<Func<Lesson, bool>>>()))
            .ReturnsAsync((Lesson?)null);

        _studentDalMock
            .Setup(s => s.GetAsync(It.IsAny<Expression<Func<Student, bool>>>()))
            .ReturnsAsync(student);

        // Act
        var result = await _lessonManager.AddAsync(dto);

        // Assert
        result.Success.Should().BeTrue();
        result.Message.Should().Contain("başarıyla kaydedildi");

        _lessonDalMock.Verify(d => d.AddAsync(It.Is<Lesson>(l =>
            l.TenantId == _testTenantId &&
            l.StudentId == studentId &&
            l.TeacherId == teacherId &&
            l.LessonDate.Kind == DateTimeKind.Utc &&
            l.PageNumber == 16)), Times.Once);

        _studentDalMock.Verify(s => s.UpdateAsync(It.Is<Student>(st =>
            st.CurrentPage == 16 &&
            st.CompletedPages == 11)), Times.Once);
    }

    [Fact]
    public async Task AddAsync_WhenDuplicateLessonOnSameDay_ShouldReturnError()
    {
        // Arrange
        var studentId = Guid.NewGuid();
        var existingLesson = new Lesson
        {
            Id = Guid.NewGuid(),
            StudentId = studentId,
            LessonDate = DateTime.UtcNow
        };

        var dto = new CreateLessonDto
        {
            StudentId = studentId,
            TeacherId = Guid.NewGuid(),
            LessonDate = DateTime.Today
        };

        _lessonDalMock
            .Setup(d => d.GetAsync(It.IsAny<Expression<Func<Lesson, bool>>>()))
            .ReturnsAsync(existingLesson);

        // Act
        var result = await _lessonManager.AddAsync(dto);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("zaten bir ders kaydı mevcut");
        _lessonDalMock.Verify(d => d.AddAsync(It.IsAny<Lesson>()), Times.Never);
    }

    [Fact]
    public async Task AddAsync_WhenStudentDoesNotExist_ShouldReturnError()
    {
        // Arrange
        var dto = new CreateLessonDto
        {
            StudentId = Guid.NewGuid(),
            TeacherId = Guid.NewGuid(),
            LessonDate = DateTime.Today
        };

        _lessonDalMock
            .Setup(d => d.GetAsync(It.IsAny<Expression<Func<Lesson, bool>>>()))
            .ReturnsAsync((Lesson?)null);

        _studentDalMock
            .Setup(s => s.GetAsync(It.IsAny<Expression<Func<Student, bool>>>()))
            .ReturnsAsync((Student?)null);

        // Act
        var result = await _lessonManager.AddAsync(dto);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("Öğrenci bulunamadı");
        _lessonDalMock.Verify(d => d.AddAsync(It.IsAny<Lesson>()), Times.Never);
    }

    [Fact]
    public async Task GetByStudentIdAsync_ShouldReturnPagedLessonsForStudent()
    {
        // Arrange
        var studentId = Guid.NewGuid();
        var teacherId = Guid.NewGuid();

        var lessons = new List<Lesson>
        {
            new() { Id = Guid.NewGuid(), StudentId = studentId, TeacherId = teacherId, LessonDate = DateTime.UtcNow, PageNumber = 20 }
        };

        var student = new Student { Id = studentId, FirstName = "Mehmet", LastName = "Demir" };
        var teacher = new User { Id = teacherId, FirstName = "Ahmet", LastName = "Hoca" };

        _lessonDalMock
            .Setup(d => d.GetListAsync(It.IsAny<Expression<Func<Lesson, bool>>>()))
            .ReturnsAsync(lessons);

        _studentDalMock
            .Setup(s => s.GetAsync(It.IsAny<Expression<Func<Student, bool>>>()))
            .ReturnsAsync(student);

        _userDalMock
            .Setup(u => u.GetAsync(It.IsAny<Expression<Func<User, bool>>>()))
            .ReturnsAsync(teacher);

        var pagination = new PaginationParams { Page = 1, PageSize = 10 };

        // Act
        var result = await _lessonManager.GetByStudentIdAsync(studentId, pagination);

        // Assert
        result.Success.Should().BeTrue();
        result.Data.Data.Should().HaveCount(1);
        result.Data.Data.First().StudentName.Should().Be("Mehmet Demir");
        result.Data.Data.First().TeacherName.Should().Be("Ahmet Hoca");
    }
}
