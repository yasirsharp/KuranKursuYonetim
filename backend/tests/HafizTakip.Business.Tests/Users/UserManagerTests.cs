using System.Linq.Expressions;
using HafizTakip.Business.Concrete;
using HafizTakip.Core.Enums;
using HafizTakip.Core.Utilities.IoC;
using HafizTakip.Core.Utilities.Results;
using HafizTakip.DataAccess.Abstract;
using HafizTakip.Entities.Concrete;
using HafizTakip.Entities.Dtos;

namespace HafizTakip.Business.Tests.Users;

public class UserManagerTests
{
    private readonly Mock<IUserDal> _userDalMock = new();
    private readonly Mock<ITenantContext> _tenantContextMock = new();
    private readonly Mock<ICurrentUser> _currentUserMock = new();
    private readonly UserManager _manager;
    private readonly Guid _tenantId = Guid.NewGuid();
    private readonly Guid _actorId = Guid.NewGuid();

    public UserManagerTests()
    {
        _tenantContextMock.SetupGet(t => t.TenantId).Returns(_tenantId);
        _currentUserMock.SetupGet(c => c.UserId).Returns(_actorId);
        _manager = new UserManager(_userDalMock.Object, _tenantContextMock.Object, _currentUserMock.Object);
    }

    private void ActAs(string role) => _currentUserMock.SetupGet(c => c.Role).Returns(role);

    private void SetupExistingUser(User? user) =>
        _userDalMock.Setup(d => d.GetAsync(It.IsAny<Expression<Func<User, bool>>>())).ReturnsAsync(user);

    private static CreateUserDto NewUserDto(string role) => new()
    {
        FirstName = "Ali",
        LastName = "Veli",
        PhoneNumber = "0532 111 22 33",
        Role = role
    };

    [Theory]
    [InlineData(UserRole.SuperAdmin)]
    [InlineData(UserRole.TenantAdmin)]
    [InlineData(UserRole.HeadTeacher)]
    public async Task AddAsync_HeadTeacherAssigningEqualOrHigherRole_ShouldBeForbidden(string targetRole)
    {
        ActAs(UserRole.HeadTeacher);

        var result = await _manager.AddAsync(NewUserDto(targetRole));

        result.Success.Should().BeFalse();
        result.ErrorType.Should().Be(ResultErrorType.Forbidden);
        _userDalMock.Verify(d => d.AddAsync(It.IsAny<User>()), Times.Never);
    }

    [Fact]
    public async Task AddAsync_TenantAdminCreatingTeacher_ShouldStoreNormalizedPhoneInCurrentTenant()
    {
        ActAs(UserRole.TenantAdmin);
        SetupExistingUser(null);

        var result = await _manager.AddAsync(NewUserDto(UserRole.HeadTeacher));

        result.Success.Should().BeTrue();
        _userDalMock.Verify(d => d.AddAsync(It.Is<User>(u =>
            u.TenantId == _tenantId &&
            u.PhoneNumber == "5321112233" &&
            u.Role == UserRole.HeadTeacher)), Times.Once);
    }

    [Fact]
    public async Task AddAsync_DuplicatePhone_ShouldReturnConflict()
    {
        ActAs(UserRole.TenantAdmin);
        SetupExistingUser(new User { Id = Guid.NewGuid(), PhoneNumber = "5321112233" });

        var result = await _manager.AddAsync(NewUserDto(UserRole.Parent));

        result.ErrorType.Should().Be(ResultErrorType.Conflict);
    }

    [Fact]
    public async Task UpdateAsync_HeadTeacherPromotingParentToSuperAdmin_ShouldBeForbidden()
    {
        ActAs(UserRole.HeadTeacher);
        var parent = new User { Id = Guid.NewGuid(), Role = UserRole.Parent, PhoneNumber = "5321112233" };
        SetupExistingUser(parent);

        var result = await _manager.UpdateAsync(new UpdateUserDto
        {
            Id = parent.Id,
            FirstName = "X",
            LastName = "Y",
            PhoneNumber = "5321112233",
            Role = UserRole.SuperAdmin
        });

        result.ErrorType.Should().Be(ResultErrorType.Forbidden);
        parent.Role.Should().Be(UserRole.Parent);
        _userDalMock.Verify(d => d.UpdateAsync(It.IsAny<User>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_UserChangingOwnRole_ShouldBeForbidden()
    {
        ActAs(UserRole.HeadTeacher);
        var self = new User { Id = _actorId, Role = UserRole.HeadTeacher, PhoneNumber = "5321112233" };
        SetupExistingUser(self);

        var result = await _manager.UpdateAsync(new UpdateUserDto
        {
            Id = _actorId,
            FirstName = "X",
            LastName = "Y",
            PhoneNumber = "5321112233",
            Role = UserRole.TenantAdmin
        });

        result.ErrorType.Should().Be(ResultErrorType.Forbidden);
    }

    [Fact]
    public async Task UpdateAsync_UserEditingOwnProfileWithoutRoleChange_ShouldSucceed()
    {
        ActAs(UserRole.HeadTeacher);
        var self = new User { Id = _actorId, Role = UserRole.HeadTeacher, PhoneNumber = "5321112233" };
        SetupExistingUser(self);

        var result = await _manager.UpdateAsync(new UpdateUserDto
        {
            Id = _actorId,
            FirstName = "Yeni",
            LastName = "Ad",
            PhoneNumber = "5321112233",
            Role = UserRole.HeadTeacher
        });

        result.Success.Should().BeTrue();
        self.FirstName.Should().Be("Yeni");
    }

    [Fact]
    public async Task DeleteAsync_HeadTeacherDeletingTenantAdmin_ShouldBeForbidden()
    {
        ActAs(UserRole.HeadTeacher);
        SetupExistingUser(new User { Id = Guid.NewGuid(), Role = UserRole.TenantAdmin });

        var result = await _manager.DeleteAsync(Guid.NewGuid());

        result.ErrorType.Should().Be(ResultErrorType.Forbidden);
    }

    [Fact]
    public async Task DeleteAsync_Self_ShouldBeForbidden()
    {
        ActAs(UserRole.TenantAdmin);
        SetupExistingUser(new User { Id = _actorId, Role = UserRole.TenantAdmin });

        var result = await _manager.DeleteAsync(_actorId);

        result.ErrorType.Should().Be(ResultErrorType.Forbidden);
    }
}
