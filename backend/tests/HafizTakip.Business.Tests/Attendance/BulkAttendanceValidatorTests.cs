using HafizTakip.Business.ValidationRules.FluentValidation;
using HafizTakip.Entities.Dtos;

namespace HafizTakip.Business.Tests.Attendance;

public class BulkAttendanceValidatorTests
{
    private readonly BulkAttendanceValidator _validator;

    public BulkAttendanceValidatorTests()
    {
        _validator = new BulkAttendanceValidator();
    }

    [Fact]
    public void Validate_WhenDtoIsValid_ShouldHaveNoErrors()
    {
        // Arrange
        var dto = new BulkAttendanceDto
        {
            Date = DateTime.Today,
            Attendances = new List<CreateAttendanceItemDto>
            {
                new() { StudentId = Guid.NewGuid(), Status = "present" }
            }
        };

        // Act
        var result = _validator.Validate(dto);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_WhenDateIsEmpty_ShouldHaveValidationError()
    {
        // Arrange
        var dto = new BulkAttendanceDto
        {
            Date = default,
            Attendances = new List<CreateAttendanceItemDto>
            {
                new() { StudentId = Guid.NewGuid(), Status = "present" }
            }
        };

        // Act
        var result = _validator.Validate(dto);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(BulkAttendanceDto.Date));
    }

    [Fact]
    public void Validate_WhenAttendancesListIsEmpty_ShouldHaveValidationError()
    {
        // Arrange
        var dto = new BulkAttendanceDto
        {
            Date = DateTime.Today,
            Attendances = new List<CreateAttendanceItemDto>()
        };

        // Act
        var result = _validator.Validate(dto);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(BulkAttendanceDto.Attendances));
    }

    [Fact]
    public void Validate_WhenItemStudentIdIsEmpty_ShouldHaveValidationError()
    {
        // Arrange
        var dto = new BulkAttendanceDto
        {
            Date = DateTime.Today,
            Attendances = new List<CreateAttendanceItemDto>
            {
                new() { StudentId = Guid.Empty, Status = "present" }
            }
        };

        // Act
        var result = _validator.Validate(dto);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName.Contains("StudentId"));
    }
}
