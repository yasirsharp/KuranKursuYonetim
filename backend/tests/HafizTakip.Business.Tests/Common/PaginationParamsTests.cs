using HafizTakip.Core.Utilities.Requests;

namespace HafizTakip.Business.Tests.Common;

public class PaginationParamsTests
{
    [Fact]
    public void Page_DefaultValue_ShouldBeOne()
    {
        // Arrange & Act
        var pagination = new PaginationParams();

        // Assert
        pagination.Page.Should().Be(1);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-50)]
    public void Page_WhenSetToZeroOrNegative_ShouldClampToOne(int invalidPage)
    {
        // Arrange
        var pagination = new PaginationParams();

        // Act
        pagination.Page = invalidPage;

        // Assert
        pagination.Page.Should().Be(1, "çünkü 0 veya negatif sayfa numaraları LINQ Skip operasyonunda hataya yol açar");
    }

    [Fact]
    public void Page_WhenSetToPositiveNumber_ShouldKeepValue()
    {
        // Arrange
        var pagination = new PaginationParams();

        // Act
        pagination.Page = 5;

        // Assert
        pagination.Page.Should().Be(5);
    }

    [Fact]
    public void PageSize_DefaultValue_ShouldBeTwenty()
    {
        // Arrange & Act
        var pagination = new PaginationParams();

        // Assert
        pagination.PageSize.Should().Be(20);
    }

    [Theory]
    [InlineData(101)]
    [InlineData(500)]
    public void PageSize_WhenExceedsMax_ShouldClampTo100(int largeSize)
    {
        // Arrange
        var pagination = new PaginationParams();

        // Act
        pagination.PageSize = largeSize;

        // Assert
        pagination.PageSize.Should().Be(100);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-10)]
    public void PageSize_WhenLessThanOne_ShouldDefaultToTwenty(int invalidSize)
    {
        // Arrange
        var pagination = new PaginationParams();

        // Act
        pagination.PageSize = invalidSize;

        // Assert
        pagination.PageSize.Should().Be(20);
    }
}
