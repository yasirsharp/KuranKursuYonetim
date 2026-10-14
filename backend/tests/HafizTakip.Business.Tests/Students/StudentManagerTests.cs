using System.Linq.Expressions;
using HafizTakip.Business.Abstract;
using HafizTakip.Business.Concrete;
using HafizTakip.Core.Entities;
using HafizTakip.Core.Utilities.IoC;
using HafizTakip.Core.Utilities.Requests;
using HafizTakip.Core.Utilities.Results;
using HafizTakip.DataAccess.Abstract;
using HafizTakip.Entities.Concrete;
using HafizTakip.Entities.Dtos;
using HafizTakip.Business.Tests.Common;

namespace HafizTakip.Business.Tests.Students;

public class StudentManagerTests
{
    private readonly Mock<ITenantReferenceValidator> _referencesMock = new();
    private readonly Mock<IStudentDal> _studentDalMock;
    private readonly Mock<IGroupDal> _groupDalMock;
    private readonly Mock<ITenantContext> _tenantContextMock;
    private readonly StudentManager _studentManager;
    private readonly Guid _testTenantId;

    public StudentManagerTests()
    {
        _studentDalMock = new Mock<IStudentDal>();
        _groupDalMock = new Mock<IGroupDal>();
        _tenantContextMock = new Mock<ITenantContext>();
        _testTenantId = Guid.NewGuid();

        _tenantContextMock.SetupGet(t => t.TenantId).Returns(_testTenantId);

        _studentManager = new StudentManager(
            _studentDalMock.Object,
            _groupDalMock.Object,
            _tenantContextMock.Object,
            _referencesMock.Object,
            AccessScopes.Unrestricted());
    }

    [Fact]
    public async Task AddAsync_WhenValidDtoWithoutGroup_ShouldAssignTenantIdAndActiveStatus()
    {
        // Arrange
        var dto = new CreateStudentDto
        {
            FirstName = "Enes",
            LastName = "Demir",
            IdentificationNumber = "12345678901",
            GroupId = null
        };

        // Act
        var result = await _studentManager.AddAsync(dto);

        // Assert
        result.Success.Should().BeTrue();
        result.Message.Should().Contain("başarıyla eklendi");

        _studentDalMock.Verify(s => s.AddAsync(It.Is<Student>(st =>
            st.TenantId == _testTenantId &&
            st.FirstName == "Enes" &&
            st.LastName == "Demir" &&
            st.Status == "active" &&
            st.CurrentPage == 0 &&
            st.IsHafiz == false)), Times.Once);
    }

    [Fact]
    public async Task AddAsync_WhenGroupIdDoesNotExist_ShouldReturnError()
    {
        // Arrange
        var invalidGroupId = Guid.NewGuid();
        var dto = new CreateStudentDto
        {
            FirstName = "Enes",
            LastName = "Demir",
            GroupId = invalidGroupId
        };

        _groupDalMock
            .Setup(g => g.GetAsync(It.IsAny<Expression<Func<Group, bool>>>()))
            .ReturnsAsync((Group?)null);

        // Act
        var result = await _studentManager.AddAsync(dto);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("sınıf bulunamadı");
        _studentDalMock.Verify(s => s.AddAsync(It.IsAny<Student>()), Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_WhenStudentExists_ShouldPerformSoftDelete()
    {
        // Arrange
        var studentId = Guid.NewGuid();
        var student = new Student
        {
            Id = studentId,
            TenantId = _testTenantId,
            FirstName = "Enes",
            IsDeleted = false
        };

        _studentDalMock
            .Setup(s => s.GetAsync(It.IsAny<Expression<Func<Student, bool>>>()))
            .ReturnsAsync(student);

        // Act
        var result = await _studentManager.DeleteAsync(studentId);

        // Assert
        result.Success.Should().BeTrue();
        student.IsDeleted.Should().BeTrue();
        student.DeletedAt.Should().NotBeNull();

        _studentDalMock.Verify(s => s.UpdateAsync(student), Times.Once);
    }

    [Fact]
    public async Task GetAllAsync_ShouldDelegateFilterAndPagingToSingleDatabaseQuery()
    {
        // Arrange: grup adı birleştirmesi ve sayfalama DAL'daki tek sorguda yapılır (N+1 yok)
        var filter = new StudentFilter { Search = "Ahmet", GroupId = Guid.NewGuid() };
        var pagination = new PaginationParams { Page = 2, PageSize = 10 };
        var page = new PagedResult<StudentDto>(new List<StudentDto> { new() { FirstName = "Ahmet", GroupName = "A Sınıfı" } }, 11, 2, 10);

        _studentDalMock
            .Setup(s => s.GetStudentDetailsPagedAsync(filter, null, pagination))
            .ReturnsAsync(page);

        // Act
        var result = await _studentManager.GetAllAsync(filter, pagination);

        // Assert
        result.Success.Should().BeTrue();
        result.Data.Should().BeSameAs(page);
        _groupDalMock.Verify(g => g.GetAsync(It.IsAny<Expression<Func<Group, bool>>>()), Times.Never);
        _studentDalMock.Verify(s => s.GetListAsync(It.IsAny<Expression<Func<Student, bool>>>()), Times.Never);
    }
}
