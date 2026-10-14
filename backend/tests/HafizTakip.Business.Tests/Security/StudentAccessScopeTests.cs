using System.Linq.Expressions;
using HafizTakip.Business.Concrete;
using HafizTakip.Business.Tests.Common;
using HafizTakip.Core.Enums;
using HafizTakip.Core.Utilities.IoC;
using HafizTakip.Core.Utilities.Requests;
using HafizTakip.Core.Utilities.Results;
using HafizTakip.DataAccess.Abstract;
using HafizTakip.Entities.Concrete;
using HafizTakip.Entities.Dtos;

namespace HafizTakip.Business.Tests.Security;

public class StudentAccessScopeTests
{
    private readonly Mock<ICurrentUser> _currentUser = new();
    private readonly Mock<IStudentParentDal> _studentParentDal = new();
    private readonly Guid _parentId = Guid.NewGuid();
    private readonly Guid _ownChild = Guid.NewGuid();
    private readonly Guid _otherChild = Guid.NewGuid();

    public StudentAccessScopeTests()
    {
        _studentParentDal
            .Setup(d => d.GetListAsync(It.IsAny<Expression<Func<StudentParent, bool>>>()))
            .ReturnsAsync(new List<StudentParent> { new() { ParentId = _parentId, StudentId = _ownChild } });
    }

    private StudentAccessScope CreateScope(string role)
    {
        _currentUser.SetupGet(c => c.Role).Returns(role);
        _currentUser.SetupGet(c => c.UserId).Returns(_parentId);
        _currentUser.Setup(c => c.IsInRoleOrAbove(It.IsAny<string>()))
            .Returns((string min) => HafizTakip.Core.Utilities.Security.RoleHierarchy.IsAtLeast(role, min));
        return new StudentAccessScope(_currentUser.Object, _studentParentDal.Object);
    }

    [Theory]
    [InlineData(UserRole.TenantAdmin)]
    [InlineData(UserRole.HeadTeacher)]
    [InlineData(UserRole.AssistantTeacher)]
    public async Task Teachers_ShouldBeUnrestricted(string role)
    {
        var scope = CreateScope(role);

        (await scope.GetAccessibleStudentIdsAsync()).Should().BeNull();
        (await scope.CanAccessStudentAsync(_otherChild)).Should().BeTrue();
        _studentParentDal.Verify(d => d.GetListAsync(It.IsAny<Expression<Func<StudentParent, bool>>>()), Times.Never);
    }

    [Fact]
    public async Task Parent_ShouldOnlyAccessLinkedChildren_AndResolveOncePerRequest()
    {
        var scope = CreateScope(UserRole.Parent);

        (await scope.CanAccessStudentAsync(_ownChild)).Should().BeTrue();
        (await scope.CanAccessStudentAsync(_otherChild)).Should().BeFalse();
        (await scope.GetAccessibleStudentIdsAsync()).Should().BeEquivalentTo(new[] { _ownChild });
        _studentParentDal.Verify(d => d.GetListAsync(It.IsAny<Expression<Func<StudentParent, bool>>>()), Times.Once);
    }

    [Fact]
    public async Task StudentRole_ShouldHaveNoAccessYet()
    {
        var scope = CreateScope(UserRole.Student);

        (await scope.GetAccessibleStudentIdsAsync()).Should().BeEmpty();
        (await scope.CanAccessStudentAsync(_ownChild)).Should().BeFalse();
    }

    [Fact]
    public async Task ParentReadingOtherChildsLessons_ShouldBeForbidden()
    {
        var lessonDal = new Mock<ILessonDal>();
        var manager = new LessonManager(lessonDal.Object, new Mock<IStudentDal>().Object, new Mock<IUserDal>().Object,
            new Mock<ITenantContext>().Object, new Mock<HafizTakip.Business.Abstract.ITenantReferenceValidator>().Object,
            _currentUser.Object, AccessScopes.Only(_ownChild));

        var result = await manager.GetByStudentIdAsync(_otherChild, new PaginationParams());

        result.ErrorType.Should().Be(ResultErrorType.Forbidden);
        lessonDal.Verify(d => d.GetListAsync(It.IsAny<Expression<Func<Lesson, bool>>>()), Times.Never);
    }

    [Fact]
    public async Task ParentReadingSingleAttendanceOfOtherChild_ShouldLookNotFound()
    {
        var attendanceDal = new Mock<IAttendanceDal>();
        attendanceDal.Setup(d => d.GetAsync(It.IsAny<Expression<Func<HafizTakip.Entities.Concrete.Attendance, bool>>>()))
            .ReturnsAsync(new HafizTakip.Entities.Concrete.Attendance { Id = Guid.NewGuid(), StudentId = _otherChild });
        var manager = new AttendanceManager(attendanceDal.Object, new Mock<IStudentDal>().Object,
            new Mock<ITenantContext>().Object, new Mock<HafizTakip.Business.Abstract.ITenantReferenceValidator>().Object,
            AccessScopes.Only(_ownChild));

        var result = await manager.GetByIdAsync(Guid.NewGuid());

        result.ErrorType.Should().Be(ResultErrorType.NotFound);
    }

    [Fact]
    public async Task ParentCompletingOtherChildsHomework_ShouldBeRejected()
    {
        var dal = new Mock<IDailyAssignmentDal>();
        var assignment = new DailyAssignment { Id = Guid.NewGuid(), StudentId = _otherChild, Status = "sent_to_parent" };
        dal.Setup(d => d.GetAsync(It.IsAny<Expression<Func<DailyAssignment, bool>>>())).ReturnsAsync(assignment);
        var manager = new DailyAssignmentManager(dal.Object, new Mock<IStudentDal>().Object, new Mock<IUserDal>().Object,
            new Mock<ITenantContext>().Object, new Mock<HafizTakip.Business.Abstract.IParentNotificationService>().Object,
            _currentUser.Object, AccessScopes.Only(_ownChild));

        var result = await manager.CompleteHomeAsync(new CompleteHomeAssignmentDto { Id = assignment.Id });

        result.ErrorType.Should().Be(ResultErrorType.NotFound);
        dal.Verify(d => d.UpdateAsync(It.IsAny<DailyAssignment>()), Times.Never);
    }
}
