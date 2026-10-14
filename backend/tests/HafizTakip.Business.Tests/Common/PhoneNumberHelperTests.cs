using HafizTakip.Core.Utilities.Helpers;

namespace HafizTakip.Business.Tests.Common;

public class PhoneNumberHelperTests
{
    [Theory]
    [InlineData("5321234567", "5321234567")]
    [InlineData("05321234567", "5321234567")]
    [InlineData("+90 532 123 45 67", "5321234567")]
    [InlineData("905321234567", "5321234567")]
    [InlineData("0 (532) 123-45-67", "5321234567")]
    [InlineData("  ", "")]
    [InlineData(null, "")]
    public void Normalize_ShouldReturnCanonicalTenDigits(string? input, string expected)
    {
        PhoneNumberHelper.Normalize(input).Should().Be(expected);
    }

    [Theory]
    [InlineData("05321234567", true)]
    [InlineData("5321234567", true)]
    [InlineData("02121234567", false)] // sabit hat
    [InlineData("532123456", false)]   // eksik hane
    public void IsValidMobile_ShouldAcceptOnlyTurkishGsmNumbers(string input, bool expected)
    {
        PhoneNumberHelper.IsValidMobile(input).Should().Be(expected);
    }

    [Fact]
    public void ToInternational_ShouldPrefixCountryCode()
    {
        PhoneNumberHelper.ToInternational("0532 123 45 67").Should().Be("905321234567");
    }
}
