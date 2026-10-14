using System.Linq.Expressions;
using HafizTakip.Business.Abstract;
using HafizTakip.Business.Concrete;
using HafizTakip.Core.DataAccess;
using HafizTakip.Core.Utilities.IoC;
using HafizTakip.Core.Utilities.Requests;
using HafizTakip.Core.Utilities.Results;
using HafizTakip.DataAccess.Abstract;
using HafizTakip.Entities.Concrete;
using HafizTakip.Entities.Dtos;
using HafizTakip.Business.Tests.Common;

namespace HafizTakip.Business.Tests.Attendance;

public class AttendanceManagerTests
{
    private readonly Mock<ITenantReferenceValidator> _referencesMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = UnitOfWorks.PassThrough();
    private readonly Mock<IAttendanceDal> _attendanceDalMock;
    private readonly Mock<IStudentDal> _studentDalMock;
    private readonly Mock<ITenantContext> _tenantContextMock;
    private readonly AttendanceManager _attendanceManager;
    private readonly Guid _testTenantId;

    public AttendanceManagerTests()
    {
        _attendanceDalMock = new Mock<IAttendanceDal>();
        _studentDalMock = new Mock<IStudentDal>();
        _tenantContextMock = new Mock<ITenantContext>();
        _testTenantId = Guid.NewGuid();

        _tenantContextMock.Setup(t => t.TenantId).ToString();
        _tenantContextMock.SetupGet(t => t.TenantId).Returns(_testTenantId);

        _attendanceManager = new AttendanceManager(
            _attendanceDalMock.Object,
            _studentDalMock.Object,
            _tenantContextMock.Object,
            _referencesMock.Object,
            AccessScopes.Unrestricted(),
            _unitOfWorkMock.Object);
    }

    [Fact]
    public async Task AddBulkAsync_WhenNewAttendances_ShouldAssignTenantIdAndUtcDateAndSave()
    {
        // Arrange
        var studentId1 = Guid.NewGuid();
        var studentId2 = Guid.NewGuid();
        var testDate = new DateTime(2026, 10, 12);

        var dto = new BulkAttendanceDto
        {
            Date = testDate,
            Attendances = new List<CreateAttendanceItemDto>
            {
                new() { StudentId = studentId1, Status = "present", Notes = "Zamanında geldi" },
                new() { StudentId = studentId2, Status = "absent", Notes = "Raporlu" }
            }
        };

        _attendanceDalMock
            .Setup(d => d.GetListAsync(It.IsAny<Expression<Func<Entities.Concrete.Attendance, bool>>>()))
            .ReturnsAsync(new List<Entities.Concrete.Attendance>());

        // Act
        var result = await _attendanceManager.AddBulkAsync(dto);

        // Assert
        result.Success.Should().BeTrue();
        result.Message.Should().Contain("başarıyla");

        _attendanceDalMock.Verify(d => d.Add(It.Is<Entities.Concrete.Attendance>(a =>
            a.TenantId == _testTenantId &&
            a.StudentId == studentId1 &&
            a.Status == "present" &&
            a.Date.Kind == DateTimeKind.Utc)), Times.Once);

        _attendanceDalMock.Verify(d => d.Add(It.Is<Entities.Concrete.Attendance>(a =>
            a.TenantId == _testTenantId &&
            a.StudentId == studentId2 &&
            a.Status == "absent" &&
            a.Date.Kind == DateTimeKind.Utc)), Times.Once);

        // Tüm sınıfın yoklaması tek seferde kaydedilir
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task AddBulkAsync_WhenAttendanceAlreadyExists_ShouldUpdateInsteadOfAdd()
    {
        // Arrange
        var studentId = Guid.NewGuid();
        var testDate = new DateTime(2026, 10, 12);

        var existingAttendance = new Entities.Concrete.Attendance
        {
            Id = Guid.NewGuid(),
            TenantId = _testTenantId,
            StudentId = studentId,
            Date = DateTime.SpecifyKind(testDate, DateTimeKind.Utc),
            Status = "absent",
            Notes = "Eski not"
        };

        var dto = new BulkAttendanceDto
        {
            Date = testDate,
            Attendances = new List<CreateAttendanceItemDto>
            {
                new() { StudentId = studentId, Status = "present", Notes = "Geç geldi, düzeltildi" }
            }
        };

        _attendanceDalMock
            .Setup(d => d.GetListAsync(It.IsAny<Expression<Func<Entities.Concrete.Attendance, bool>>>()))
            .ReturnsAsync(new List<Entities.Concrete.Attendance> { existingAttendance });

        // Act
        var result = await _attendanceManager.AddBulkAsync(dto);

        // Assert
        result.Success.Should().BeTrue();
        existingAttendance.Status.Should().Be("present");
        existingAttendance.Notes.Should().Be("Geç geldi, düzeltildi");

        _attendanceDalMock.Verify(d => d.Update(existingAttendance), Times.Once);
        _attendanceDalMock.Verify(d => d.Add(It.IsAny<Entities.Concrete.Attendance>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task AddAsync_WhenDuplicateRecordExistsOnSameDay_ShouldReturnError()
    {
        // Arrange
        var studentId = Guid.NewGuid();
        var testDate = new DateTime(2026, 10, 12);

        var existingAttendance = new Entities.Concrete.Attendance
        {
            Id = Guid.NewGuid(),
            TenantId = _testTenantId,
            StudentId = studentId,
            Date = DateTime.SpecifyKind(testDate, DateTimeKind.Utc),
            Status = "present"
        };

        var dto = new CreateAttendanceDto
        {
            StudentId = studentId,
            Date = testDate,
            Status = "late"
        };

        _attendanceDalMock
            .Setup(d => d.GetAsync(It.IsAny<Expression<Func<Entities.Concrete.Attendance, bool>>>()))
            .ReturnsAsync(existingAttendance);

        // Act
        var result = await _attendanceManager.AddAsync(dto);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("zaten bir yoklama kaydı var");
        _attendanceDalMock.Verify(d => d.AddAsync(It.IsAny<Entities.Concrete.Attendance>()), Times.Never);
    }

    [Fact]
    public async Task GetAllAsync_ShouldDelegateFilterAndPagingToSingleDatabaseQuery()
    {
        // Arrange: öğrenci adı birleştirmesi ve sayfalama DAL'daki tek sorguda yapılır (N+1 yok)
        var filter = new AttendanceFilter { From = new DateTime(2026, 10, 1), To = new DateTime(2026, 10, 31) };
        var pagination = new PaginationParams { Page = 1, PageSize = 10 };
        var page = new PagedResult<AttendanceDto>(new List<AttendanceDto> { new() { StudentName = "Ahmet Yılmaz", Status = "present" } }, 1, 1, 10);

        _attendanceDalMock
            .Setup(d => d.GetAttendanceDetailsPagedAsync(filter, null, pagination))
            .ReturnsAsync(page);

        // Act
        var result = await _attendanceManager.GetAllAsync(filter, pagination);

        // Assert
        result.Success.Should().BeTrue();
        result.Data.Should().BeSameAs(page);
        _studentDalMock.Verify(s => s.GetAsync(It.IsAny<Expression<Func<Entities.Concrete.Student, bool>>>()), Times.Never);
    }
}
