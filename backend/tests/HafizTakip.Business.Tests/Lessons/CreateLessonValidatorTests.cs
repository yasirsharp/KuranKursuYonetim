using HafizTakip.Business.ValidationRules.FluentValidation;
using HafizTakip.Entities.Dtos;

namespace HafizTakip.Business.Tests.Lessons;

public class CreateLessonValidatorTests
{
    private readonly CreateLessonValidator _validator;

    public CreateLessonValidatorTests()
    {
        _validator = new CreateLessonValidator();
    }

    [Fact]
    public void Validate_WhenAllFieldsValid_ShouldBeValid()
    {
        // Arrange
        var dto = new CreateLessonDto
        {
            StudentId = Guid.NewGuid(),
            TeacherId = Guid.NewGuid(),
            LessonDate = DateTime.Today,
            Performance = "iyi"
        };

        // Act
        var result = _validator.Validate(dto);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void Validate_WhenPerformanceIsEmpty_ShouldHaveValidationError(string? invalidPerformance)
    {
        // Arrange
        var dto = new CreateLessonDto
        {
            StudentId = Guid.NewGuid(),
            TeacherId = Guid.NewGuid(),
            LessonDate = DateTime.Today,
            Performance = invalidPerformance!
        };

        // Act
        var result = _validator.Validate(dto);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateLessonDto.Performance));
    }
}
