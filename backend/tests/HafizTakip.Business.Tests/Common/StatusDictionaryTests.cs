using HafizTakip.Business.ValidationRules.FluentValidation;
using HafizTakip.Core.Enums;
using HafizTakip.Entities.Dtos;

namespace HafizTakip.Business.Tests.Common;

public class StatusDictionaryTests
{
    [Theory]
    [InlineData(PerformanceLevel.Mukemmel, 5, "Mükemmel")]
    [InlineData(PerformanceLevel.CokIyi, 4, "Çok İyi")]
    [InlineData(PerformanceLevel.Iyi, 3, "İyi")]
    [InlineData(PerformanceLevel.Orta, 2, "Orta")]
    [InlineData(PerformanceLevel.Zayif, 1, "Zayıf")]
    public void PerformanceLevel_ShouldMapToScoreAndLabel(string code, int score, string label)
    {
        PerformanceLevel.ScoreOf(code).Should().Be(score);
        PerformanceLevel.LabelOf(code).Should().Be(label);
    }

    [Fact]
    public void PerformanceLevel_UnknownCode_ShouldHaveNoScore()
    {
        PerformanceLevel.ScoreOf("pekiyi").Should().BeNull();
        PerformanceLevel.LabelOf("pekiyi").Should().Be("pekiyi");
    }

    [Fact]
    public void Codes_ShouldBeAsciiLowercase()
    {
        var all = AttendanceStatus.All
            .Concat(PerformanceLevel.All)
            .Concat(HomeworkStatus.All)
            .Concat(ExamType.All)
            .Concat(AssignmentStatus.All);

        all.Should().OnlyContain(code => code.All(c => (c >= 'a' && c <= 'z') || c == '_'),
            "veritabanında saklanan kodlar ASCII olmalı; Türkçe etiketler yalnızca gösterim içindir");
    }

    [Theory]
    [InlineData("present", true)]
    [InlineData("late", true)]
    [InlineData("var", false)]   // eski dashboard değeri
    [InlineData("geldi", false)] // eski seed değeri
    public void AttendanceValidator_ShouldOnlyAcceptKnownStatuses(string status, bool valid)
    {
        var result = new CreateAttendanceValidator().Validate(new CreateAttendanceDto
        {
            StudentId = Guid.NewGuid(),
            Date = DateTime.Today,
            Status = status,
            Notes = ""
        });

        result.IsValid.Should().Be(valid);
    }

    [Fact]
    public void ExamValidator_ShouldAcceptDenemeType()
    {
        var result = new CreateExamValidator().Validate(new CreateExamDto
        {
            GroupId = Guid.NewGuid(),
            ExamType = ExamType.Deneme,
            ExamDate = DateTime.Today,
            Title = "Ekim denemesi"
        });

        result.IsValid.Should().BeTrue();
    }
}
