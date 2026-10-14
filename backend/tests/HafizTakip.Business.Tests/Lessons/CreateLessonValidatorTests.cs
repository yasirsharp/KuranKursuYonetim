using HafizTakip.Business.ValidationRules.FluentValidation;
using HafizTakip.Core.Enums;
using HafizTakip.Entities.Dtos;

namespace HafizTakip.Business.Tests.Lessons;

public class CreateLessonValidatorTests
{
    private readonly CreateLessonValidator _validator;

    public CreateLessonValidatorTests()
    {
        _validator = new CreateLessonValidator();
    }

    private static CreateLessonDto ValidDto() => new()
    {
        StudentId = Guid.NewGuid(),
        TeacherId = Guid.NewGuid(),
        LessonDate = DateTime.Today,
        PageNo = 12,
        Performance = PerformanceLevel.Iyi,
        HomeworkStatus = HomeworkStatus.Yapildi
    };

    [Fact]
    public void Validate_WhenAllFieldsValid_ShouldBeValid()
    {
        var result = _validator.Validate(ValidDto());

        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData(PerformanceLevel.Mukemmel)]
    [InlineData(PerformanceLevel.CokIyi)]
    [InlineData(PerformanceLevel.Iyi)]
    [InlineData(PerformanceLevel.Orta)]
    [InlineData(PerformanceLevel.Zayif)]
    public void Validate_AllFivePerformanceLevels_ShouldBeAccepted(string performance)
    {
        var dto = ValidDto();
        dto.Performance = performance;

        _validator.Validate(dto).IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    [InlineData("pekiyi")]   // eski 4'lü ölçek
    [InlineData("İyi")]      // eski Türkçe etiket
    public void Validate_WhenPerformanceIsNotAKnownCode_ShouldHaveValidationError(string? invalidPerformance)
    {
        var dto = ValidDto();
        dto.Performance = invalidPerformance!;

        var result = _validator.Validate(dto);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateLessonDto.Performance));
    }

    [Theory]
    [InlineData("yapıldı")]
    [InlineData("done")]
    public void Validate_WhenHomeworkStatusIsNotAKnownCode_ShouldHaveValidationError(string homeworkStatus)
    {
        var dto = ValidDto();
        dto.HomeworkStatus = homeworkStatus;

        _validator.Validate(dto).Errors.Should().Contain(e => e.PropertyName == nameof(CreateLessonDto.HomeworkStatus));
    }

    [Theory]
    [InlineData(null)]
    [InlineData(0)]
    [InlineData(605)]
    public void Validate_WhenPageIsMissingOrOutOfMushaf_ShouldHaveValidationError(int? page)
    {
        var dto = ValidDto();
        dto.PageNo = page;

        _validator.Validate(dto).Errors.Should().Contain(e => e.PropertyName == nameof(CreateLessonDto.PageNo));
    }

    [Fact]
    public void Validate_WhenJuzOutOfRange_ShouldHaveValidationError()
    {
        var dto = ValidDto();
        dto.CuzNo = 31;

        _validator.Validate(dto).Errors.Should().Contain(e => e.PropertyName == nameof(CreateLessonDto.CuzNo));
    }

    [Fact]
    public void Validate_WhenLessonDateIsInTheFuture_ShouldHaveValidationError()
    {
        var dto = ValidDto();
        dto.LessonDate = DateTime.Today.AddDays(5);

        _validator.Validate(dto).Errors.Should().Contain(e => e.PropertyName == nameof(CreateLessonDto.LessonDate));
    }
}
