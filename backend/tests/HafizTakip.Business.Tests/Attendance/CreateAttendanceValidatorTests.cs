using HafizTakip.Business.ValidationRules.FluentValidation;
using HafizTakip.Entities.Dtos;

namespace HafizTakip.Business.Tests.Attendance;

public class CreateAttendanceValidatorTests
{
    private readonly CreateAttendanceValidator _validator;

    public CreateAttendanceValidatorTests()
    {
        _validator = new CreateAttendanceValidator();
    }

    [Fact]
    public void Validate_WhenAllFieldsPresent_ShouldBeValid()
    {
        // Arrange
        var dto = new CreateAttendanceDto
        {
            StudentId = Guid.NewGuid(),
            Date = DateTime.Today,
            Status = "present"
        };

        // Act
        var result = _validator.Validate(dto);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_WhenStudentIdIsEmpty_ShouldHaveError()
    {
        // Arrange
        var dto = new CreateAttendanceDto
        {
            StudentId = Guid.Empty,
            Date = DateTime.Today,
            Status = "present"
        };

        // Act
        var result = _validator.Validate(dto);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateAttendanceDto.StudentId));
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void Validate_WhenStatusIsEmpty_ShouldHaveError(string? invalidStatus)
    {
        // Arrange
        var dto = new CreateAttendanceDto
        {
            StudentId = Guid.NewGuid(),
            Date = DateTime.Today,
            Status = invalidStatus!
        };

        // Act
        var result = _validator.Validate(dto);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateAttendanceDto.Status));
    }
}
