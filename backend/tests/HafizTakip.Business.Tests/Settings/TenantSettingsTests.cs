using System.Linq.Expressions;
using HafizTakip.Business.Concrete;
using HafizTakip.Business.ValidationRules.FluentValidation;
using HafizTakip.Core.Enums;
using HafizTakip.Core.Utilities.IoC;
using HafizTakip.DataAccess.Abstract;
using HafizTakip.Entities.Concrete;
using HafizTakip.Entities.Dtos;

namespace HafizTakip.Business.Tests.Settings;

public class TenantSettingsTests
{
    private static TenantSettingsDto ValidSettings() => new()
    {
        WorkingDays = new List<int> { 1, 2, 3, 4, 5 },
        DayStartTime = new TimeOnly(8, 30),
        DayEndTime = new TimeOnly(16, 0),
        DefaultSlotMinutes = 40,
        TimeZone = "Europe/Istanbul",
        AbsenceThreshold = 4,
        AbsencePeriodDays = 30,
        ReviewMode = ReviewMode.Cyclic,
        DailyReviewTargetPages = 20
    };

    [Fact]
    public void Validator_ValidSettings_ShouldPass()
    {
        new UpdateTenantSettingsValidator().Validate(ValidSettings()).IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData(new int[0])]
    [InlineData(new[] { 0, 1 })]
    [InlineData(new[] { 1, 8 })]
    [InlineData(new[] { 1, 1, 2 })]
    public void Validator_InvalidWorkingDays_ShouldFail(int[] days)
    {
        var dto = ValidSettings();
        dto.WorkingDays = days.ToList();

        new UpdateTenantSettingsValidator().Validate(dto).Errors
            .Should().Contain(e => e.PropertyName == nameof(TenantSettingsDto.WorkingDays));
    }

    [Fact]
    public void Validator_EndBeforeStart_ShouldFail()
    {
        var dto = ValidSettings();
        dto.DayEndTime = new TimeOnly(7, 0);

        new UpdateTenantSettingsValidator().Validate(dto).Errors
            .Should().Contain(e => e.PropertyName == nameof(TenantSettingsDto.DayEndTime));
    }

    [Theory]
    [InlineData("Mars/Olympus")]
    [InlineData("")]
    public void Validator_UnknownTimeZone_ShouldFail(string timeZone)
    {
        var dto = ValidSettings();
        dto.TimeZone = timeZone;

        new UpdateTenantSettingsValidator().Validate(dto).IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task Get_WhenTenantHasNoSettingsRow_ShouldCreateDefaults()
    {
        var tenantId = Guid.NewGuid();
        var settingDal = new Mock<ITenantSettingDal>();
        settingDal.Setup(d => d.GetAsync(It.IsAny<Expression<Func<TenantSetting, bool>>>())).ReturnsAsync((TenantSetting?)null);
        var tenantContext = new Mock<ITenantContext>();
        tenantContext.SetupGet(t => t.TenantId).Returns(tenantId);
        var manager = new TenantSettingsManager(settingDal.Object, new Mock<ITenantDal>().Object, tenantContext.Object);

        var result = await manager.GetAsync();

        result.Success.Should().BeTrue();
        result.Data.WorkingDays.Should().Equal(1, 2, 3, 4, 5, 6);
        result.Data.ReviewMode.Should().Be(ReviewMode.Spaced);
        settingDal.Verify(d => d.AddAsync(It.Is<TenantSetting>(s => s.TenantId == tenantId)), Times.Once);
    }

    [Fact]
    public async Task Update_ShouldNormalizeWorkingDays()
    {
        var existing = new TenantSetting { TenantId = Guid.NewGuid() };
        var settingDal = new Mock<ITenantSettingDal>();
        settingDal.Setup(d => d.GetAsync(It.IsAny<Expression<Func<TenantSetting, bool>>>())).ReturnsAsync(existing);
        var manager = new TenantSettingsManager(settingDal.Object, new Mock<ITenantDal>().Object, new Mock<ITenantContext>().Object);
        var dto = ValidSettings();
        dto.WorkingDays = new List<int> { 5, 1, 3 };

        await manager.UpdateAsync(dto);

        existing.WorkingDays.Should().Equal(1, 3, 5);
        existing.ReviewMode.Should().Be(ReviewMode.Cyclic);
        settingDal.Verify(d => d.UpdateAsync(existing), Times.Once);
    }
}
