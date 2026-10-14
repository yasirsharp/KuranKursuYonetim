using HafizTakip.Business.ValidationRules.FluentValidation;
using HafizTakip.Entities.Dtos;

namespace HafizTakip.Business.Tests.Students;

public class CreateStudentValidatorTests
{
    private readonly CreateStudentValidator _validator;

    public CreateStudentValidatorTests()
    {
        _validator = new CreateStudentValidator();
    }

    [Fact]
    public void Validate_WhenValidDto_ShouldBeValid()
    {
        // Arrange
        var dto = new CreateStudentDto
        {
            FirstName = "Ali",
            LastName = "Kaya",
            IdentificationNumber = "12345678901"
        };

        // Act
        var result = _validator.Validate(dto);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void Validate_WhenFirstNameIsEmpty_ShouldHaveError(string? invalidName)
    {
        // Arrange
        var dto = new CreateStudentDto
        {
            FirstName = invalidName!,
            LastName = "Kaya"
        };

        // Act
        var result = _validator.Validate(dto);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateStudentDto.FirstName));
    }

    [Fact]
    public void Validate_WhenTcIsNot11Digits_ShouldHaveError()
    {
        // Arrange
        var dto = new CreateStudentDto
        {
            FirstName = "Ali",
            LastName = "Kaya",
            IdentificationNumber = "12345" // 5 hane
        };

        // Act
        var result = _validator.Validate(dto);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateStudentDto.IdentificationNumber));
    }
}
