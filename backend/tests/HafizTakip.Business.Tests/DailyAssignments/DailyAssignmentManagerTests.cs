using System.Linq.Expressions;
using System.Text.Json;
using HafizTakip.Business.Abstract;
using HafizTakip.Business.Concrete;
using HafizTakip.Core.Entities;
using HafizTakip.Core.Utilities.IoC;
using HafizTakip.Core.Utilities.Requests;
using HafizTakip.DataAccess.Abstract;
using HafizTakip.Entities.Concrete;
using HafizTakip.Entities.Dtos;

namespace HafizTakip.Business.Tests.DailyAssignments;

public class DailyAssignmentManagerTests
{
    private readonly Mock<IDailyAssignmentDal> _dailyAssignmentDalMock;
    private readonly Mock<IStudentDal> _studentDalMock;
    private readonly Mock<IUserDal> _userDalMock;
    private readonly Mock<ITenantContext> _tenantContextMock;
    private readonly Mock<IParentNotificationService> _notificationServiceMock;
    private readonly Mock<ICurrentUser> _currentUserMock;
    private readonly DailyAssignmentManager _manager;
    private readonly Guid _testTenantId;

    public DailyAssignmentManagerTests()
    {
        _dailyAssignmentDalMock = new Mock<IDailyAssignmentDal>();
        _studentDalMock = new Mock<IStudentDal>();
        _userDalMock = new Mock<IUserDal>();
        _tenantContextMock = new Mock<ITenantContext>();
        _notificationServiceMock = new Mock<IParentNotificationService>();
        _currentUserMock = new Mock<ICurrentUser>();
        _testTenantId = Guid.NewGuid();

        _tenantContextMock.SetupGet(t => t.TenantId).Returns(_testTenantId);

        _manager = new DailyAssignmentManager(
            _dailyAssignmentDalMock.Object,
            _studentDalMock.Object,
            _userDalMock.Object,
            _tenantContextMock.Object,
            _notificationServiceMock.Object,
            _currentUserMock.Object);
    }

    [Fact]
    public async Task GetEtutAssignmentsAsync_ShouldFilterByDateAndIsBoarderToday()
    {
        // Arrange
        var testDate = new DateTime(2026, 10, 12);
        var studentId = Guid.NewGuid();

        var assignments = new List<DailyAssignment>
        {
            new()
            {
                Id = Guid.NewGuid(),
                TenantId = _testTenantId,
                StudentId = studentId,
                TeacherId = Guid.NewGuid(),
                AssignmentDate = DateTime.SpecifyKind(testDate, DateTimeKind.Utc),
                IsBoarderToday = true,
                Status = "assigned",
                AssignedTasks = "[]",
                CompletedInEtut = "[]",
                CompletedAtHome = "[]"
            }
        };

        _dailyAssignmentDalMock
            .Setup(d => d.GetListAsync(It.IsAny<Expression<Func<DailyAssignment, bool>>>()))
            .ReturnsAsync(assignments);

        var pagination = new PaginationParams { Page = 1, PageSize = 10 };

        // Act
        var result = await _manager.GetEtutAssignmentsAsync(testDate, pagination);

        // Assert
        result.Success.Should().BeTrue();
        result.Data.Data.Should().HaveCount(1);
        result.Data.Data.First().IsBoarderToday.Should().BeTrue();
    }

    [Fact]
    public async Task CompleteEtutAsync_WhenAllTasksCompleted_ShouldSetStatusToCompleted()
    {
        // Arrange
        var assignmentId = Guid.NewGuid();
        var taskList = new List<AssignmentTaskItem>
        {
            new() { Id = "task1", Type = "ezber", Cuz = 1, Page = 5 },
            new() { Id = "task2", Type = "has", Cuz = 2, Page = 10 }
        };

        var assignment = new DailyAssignment
        {
            Id = assignmentId,
            TenantId = _testTenantId,
            Status = "assigned",
            AssignedTasks = JsonSerializer.Serialize(taskList),
            CompletedInEtut = "[]"
        };

        var dto = new CompleteEtutAssignmentDto
        {
            Id = assignmentId,
            CompletedTaskIds = new List<string> { "task1", "task2" },
            FinishEtut = true,
            EtutTeacherNote = "Eksiksiz tamamladı"
        };

        _dailyAssignmentDalMock
            .Setup(d => d.GetAsync(It.IsAny<Expression<Func<DailyAssignment, bool>>>()))
            .ReturnsAsync(assignment);

        // Act
        var result = await _manager.CompleteEtutAsync(dto);

        // Assert
        result.Success.Should().BeTrue();
        assignment.Status.Should().Be("completed");
        assignment.EtutTeacherNote.Should().Be("Eksiksiz tamamladı");

        _dailyAssignmentDalMock.Verify(d => d.UpdateAsync(assignment), Times.Once);
    }

    [Fact]
    public async Task CompleteEtutAsync_WhenSomeTasksRemain_ShouldSetStatusToSentToParent()
    {
        // Arrange
        var assignmentId = Guid.NewGuid();
        var studentId = Guid.NewGuid();

        var taskList = new List<AssignmentTaskItem>
        {
            new() { Id = "task1", Type = "ezber", Cuz = 1, Page = 5 },
            new() { Id = "task2", Type = "has", Cuz = 2, Page = 10 }
        };

        var assignment = new DailyAssignment
        {
            Id = assignmentId,
            TenantId = _testTenantId,
            StudentId = studentId,
            Status = "assigned",
            AssignedTasks = JsonSerializer.Serialize(taskList),
            CompletedInEtut = "[]"
        };

        var student = new Student
        {
            Id = studentId,
            FirstName = "Yusuf",
            LastName = "Kaya"
        };

        var dto = new CompleteEtutAssignmentDto
        {
            Id = assignmentId,
            CompletedTaskIds = new List<string> { "task1" }, // task2 eksik kaldı
            FinishEtut = true
        };

        _dailyAssignmentDalMock
            .Setup(d => d.GetAsync(It.IsAny<Expression<Func<DailyAssignment, bool>>>()))
            .ReturnsAsync(assignment);

        _studentDalMock
            .Setup(s => s.GetAsync(It.IsAny<Expression<Func<Student, bool>>>()))
            .ReturnsAsync(student);

        // Act
        var result = await _manager.CompleteEtutAsync(dto);

        // Assert
        result.Success.Should().BeTrue();
        assignment.Status.Should().Be("sent_to_parent", "çünkü etütte tamamlanamayan ödevler veliye yönlendirilmeli");

        _notificationServiceMock.Verify(n => n.AddAsync(It.Is<CreateParentNotificationDto>(p =>
            p.StudentId == studentId &&
            p.NotificationType == "homework")), Times.Once);
    }
}
