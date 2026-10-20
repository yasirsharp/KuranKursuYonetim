using System.Linq.Expressions;
using HafizTakip.Business.Calendar;
using HafizTakip.Business.Concrete;
using HafizTakip.Business.Tests.Common;
using HafizTakip.Business.ValidationRules.FluentValidation;
using HafizTakip.Core.Enums;
using HafizTakip.Core.Utilities.IoC;
using HafizTakip.Core.Utilities.Results;
using HafizTakip.DataAccess.Abstract;
using HafizTakip.Entities.Concrete;
using HafizTakip.Entities.Dtos;

namespace HafizTakip.Business.Tests.Calendar;

public class CalendarTests
{
    private static readonly DateOnly TermStart = new(2026, 9, 14);
    private static readonly DateOnly TermEnd = new(2027, 8, 28);

    private static AcademicTerm Term(DateOnly start, DateOnly end, string name = "2026-2027 Eğitim Dönemi") =>
        new() { Name = name, StartDate = start, EndDate = end };

    // ── Tatil listesi ──

    [Fact]
    public void Holidays_ForTerm_ShouldIncludeOfficialAndReligiousHolidaysInRange()
    {
        var (holidays, missing) = TurkishHolidayCalendar.GetHolidays(TermStart, TermEnd);

        missing.Should().BeEmpty();
        holidays.Select(h => h.Key).Should().Contain(new[]
        {
            "2026-cumhuriyet", "2027-yilbasi", "2027-ramazan-bayrami", "2027-kurban-bayrami", "2027-genclik-spor"
        });
        // 30 Ağustos dönem bitişinden (28.08) sonra; 15 Temmuz 2026 dönem başlangıcından önce
        holidays.Should().NotContain(h => h.Key == "2027-zafer" || h.Key == "2026-demokrasi");
        holidays.Should().BeInAscendingOrder(h => h.StartDate);
    }

    [Fact]
    public void Holidays_ReligiousHolidays_ShouldHaveCorrectLengthAndHalfDayArife()
    {
        var (holidays, _) = TurkishHolidayCalendar.GetHolidays(new DateOnly(2027, 1, 1), new DateOnly(2027, 12, 31));

        var ramazan = holidays.Single(h => h.Key == "2027-ramazan-bayrami");
        ramazan.StartDate.Should().Be(new DateOnly(2027, 3, 9));
        ramazan.EndDate.Should().Be(new DateOnly(2027, 3, 11));
        ramazan.IsDayOff.Should().BeTrue();

        var kurban = holidays.Single(h => h.Key == "2027-kurban-bayrami");
        kurban.StartDate.Should().Be(new DateOnly(2027, 5, 16));
        kurban.EndDate.Should().Be(new DateOnly(2027, 5, 19));

        var arife = holidays.Single(h => h.Key == "2027-kurban-bayrami-arife");
        arife.StartDate.Should().Be(new DateOnly(2027, 5, 15));
        arife.IsDayOff.Should().BeFalse("arife öğleden sonra yarım gündür");
        holidays.Single(h => h.Key == "2027-cumhuriyet-arife").IsDayOff.Should().BeFalse();
    }

    [Fact]
    public void Holidays_YearWithoutReligiousTable_ShouldBeReportedAsMissing()
    {
        var (holidays, missing) = TurkishHolidayCalendar.GetHolidays(new DateOnly(2027, 9, 13), new DateOnly(2028, 8, 26));

        missing.Should().Equal(2028);
        holidays.Should().Contain(h => h.Key == "2028-yilbasi");
        holidays.Should().NotContain(h => h.EventType == CalendarEventType.ReligiousHoliday && h.StartDate.Year == 2028);
    }

    // ── Ders günü hesabı ──

    [Fact]
    public void SchoolDays_ShouldSkipNonWorkingDaysHolidaysAndDaysOutsideTerm()
    {
        // 26.10.2026 Pzt – 01.11.2026 Paz; çalışma günleri Pzt–Cmt; 29 Ekim tatil; 28 Ekim yarım gün (ders günü)
        var terms = new[] { Term(new DateOnly(2026, 9, 14), new DateOnly(2026, 10, 31)) };
        var events = new[]
        {
            new CalendarEvent { Title = "Cumhuriyet Bayramı", EventType = CalendarEventType.OfficialHoliday, StartDate = new DateOnly(2026, 10, 29), EndDate = new DateOnly(2026, 10, 29), IsDayOff = true },
            new CalendarEvent { Title = "Arife", EventType = CalendarEventType.OfficialHoliday, StartDate = new DateOnly(2026, 10, 28), EndDate = new DateOnly(2026, 10, 28), IsDayOff = false }
        };

        var result = CalendarManager.CalculateSchoolDays(
            new DateOnly(2026, 10, 26), new DateOnly(2026, 11, 1), new[] { 1, 2, 3, 4, 5, 6 }, terms, events);

        result.SchoolDays.Should().Equal(
            new DateOnly(2026, 10, 26), new DateOnly(2026, 10, 27), new DateOnly(2026, 10, 28),
            new DateOnly(2026, 10, 30), new DateOnly(2026, 10, 31));
        result.SchoolDayCount.Should().Be(5);
        result.OffDays.Should().ContainSingle(d => d.Date == new DateOnly(2026, 10, 29) && d.Reason == CalendarEventType.OfficialHoliday);
        result.OffDays.Should().ContainSingle(d => d.Date == new DateOnly(2026, 11, 1) && d.Reason == "outside_term");
    }

    [Fact]
    public void SchoolDays_WithoutAnyTerm_ShouldOnlyUseWorkingDays()
    {
        var result = CalendarManager.CalculateSchoolDays(
            new DateOnly(2026, 10, 19), new DateOnly(2026, 10, 25), new[] { 1, 3, 5 },
            Array.Empty<AcademicTerm>(), Array.Empty<CalendarEvent>());

        result.SchoolDayCount.Should().Be(3);
        result.OffDays.Should().OnlyContain(d => d.Reason == "not_working_day");
    }

    // ── Yeni dönem önerisi ──

    [Fact]
    public void SuggestFollowingTerm_ShouldKeepWeekdaysAndShiftOneYear()
    {
        var suggestion = CalendarManager.SuggestFollowingTerm(Term(TermStart, TermEnd), new DateOnly(2027, 7, 1));

        suggestion.StartDate.Should().Be(new DateOnly(2027, 9, 13));
        suggestion.StartDate.DayOfWeek.Should().Be(TermStart.DayOfWeek);
        suggestion.EndDate.Should().Be(new DateOnly(2028, 8, 26));
        suggestion.EndDate.DayOfWeek.Should().Be(TermEnd.DayOfWeek);
        suggestion.Name.Should().Be("2027-2028 Eğitim Dönemi");
    }

    [Fact]
    public void SuggestFollowingTerm_FromOldTerm_ShouldMoveToFuture()
    {
        var suggestion = CalendarManager.SuggestFollowingTerm(Term(new DateOnly(2023, 9, 18), new DateOnly(2024, 8, 31)), new DateOnly(2026, 10, 20));

        suggestion.EndDate.Should().BeOnOrAfter(new DateOnly(2026, 10, 20));
        suggestion.StartDate.DayOfWeek.Should().Be(DayOfWeek.Monday);
    }

    // ── Validator'lar ──

    [Fact]
    public void TermValidator_ValidTerm_ShouldPass()
    {
        new CreateAcademicTermValidator().Validate(new CreateAcademicTermDto { Name = "2026-2027", StartDate = TermStart, EndDate = TermEnd })
            .IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("2026-09-14", "2026-09-14")]
    [InlineData("2026-09-14", "2026-09-01")]
    [InlineData("2026-09-14", "2028-06-01")]
    public void TermValidator_InvalidRange_ShouldFail(string start, string end)
    {
        var dto = new CreateAcademicTermDto { Name = "Dönem", StartDate = DateOnly.Parse(start), EndDate = DateOnly.Parse(end) };
        new CreateAcademicTermValidator().Validate(dto).Errors.Should().Contain(e => e.PropertyName == nameof(CreateAcademicTermDto.EndDate));
    }

    [Fact]
    public void EventValidator_UnknownType_ShouldFail()
    {
        var dto = new CreateCalendarEventDto { Title = "Tatil", EventType = "vacation", StartDate = TermStart, EndDate = TermStart };
        new CreateCalendarEventValidator().Validate(dto).Errors.Should().Contain(e => e.PropertyName == nameof(CreateCalendarEventDto.EventType));
    }

    // ── Manager ──

    private sealed class FixedTimeProvider : TimeProvider
    {
        private readonly DateTimeOffset _now;
        public FixedTimeProvider(DateTimeOffset now) => _now = now;
        public override DateTimeOffset GetUtcNow() => _now;
    }

    private readonly Mock<IAcademicTermDal> _termDal = new();
    private readonly Mock<ICalendarEventDal> _eventDal = new();
    private readonly Mock<ITenantSettingDal> _settingDal = new();

    private CalendarManager CreateManager(DateOnly today)
    {
        var tenantContext = new Mock<ITenantContext>();
        tenantContext.SetupGet(t => t.TenantId).Returns(Guid.NewGuid());
        var unitOfWork = UnitOfWorks.PassThrough();
        unitOfWork.Setup(u => u.ExecuteInTransactionAsync(It.IsAny<Func<Task<IDataResult<AcademicTermDto>>>>()))
            .Returns((Func<Task<IDataResult<AcademicTermDto>>> work) => work());
        _eventDal.Setup(d => d.GetListAsync(It.IsAny<Expression<Func<CalendarEvent, bool>>>())).ReturnsAsync(new List<CalendarEvent>());

        // Öğlen 12:00 (UTC+3) — kursun saat dilimine göre "bugün"
        var now = new DateTimeOffset(today.ToDateTime(new TimeOnly(9, 0)), TimeSpan.Zero);
        return new CalendarManager(_termDal.Object, _eventDal.Object, _settingDal.Object, tenantContext.Object,
            unitOfWork.Object, new FixedTimeProvider(now));
    }

    [Fact]
    public async Task AddTerm_OverlappingExistingTerm_ShouldReturnConflict()
    {
        _termDal.Setup(d => d.GetAsync(It.IsAny<Expression<Func<AcademicTerm, bool>>>())).ReturnsAsync(Term(TermStart, TermEnd));
        var manager = CreateManager(new DateOnly(2026, 10, 20));

        var result = await manager.AddTermAsync(new CreateAcademicTermDto { Name = "Yeni", StartDate = new DateOnly(2027, 8, 1), EndDate = new DateOnly(2028, 6, 1) });

        result.Success.Should().BeFalse();
        result.ErrorType.Should().Be(ResultErrorType.Conflict);
        result.Message.Should().Contain("2026-2027 Eğitim Dönemi");
        _termDal.Verify(d => d.Add(It.IsAny<AcademicTerm>()), Times.Never);
    }

    [Fact]
    public async Task AddTerm_NoOverlap_ShouldAddTerm()
    {
        _termDal.Setup(d => d.GetAsync(It.IsAny<Expression<Func<AcademicTerm, bool>>>())).ReturnsAsync((AcademicTerm?)null);
        var manager = CreateManager(new DateOnly(2026, 10, 20));

        var result = await manager.AddTermAsync(new CreateAcademicTermDto { Name = " 2027-2028 ", StartDate = new DateOnly(2027, 9, 13), EndDate = new DateOnly(2028, 8, 26) });

        result.Success.Should().BeTrue();
        result.Data!.State.Should().Be("upcoming");
        _termDal.Verify(d => d.Add(It.Is<AcademicTerm>(t => t.Name == "2027-2028")), Times.Once);
    }

    [Theory]
    [InlineData("2027-07-15", true)]   // 44 gün kaldı
    [InlineData("2026-10-20", false)]  // dönem başında
    public async Task Planning_NearTermEnd_ShouldSuggestNextTerm(string today, bool shouldPlan)
    {
        _termDal.Setup(d => d.GetListAsync(It.IsAny<Expression<Func<AcademicTerm, bool>>>()))
            .ReturnsAsync(new List<AcademicTerm> { Term(TermStart, TermEnd) });
        var manager = CreateManager(DateOnly.Parse(today));

        var result = await manager.GetTermPlanningAsync();

        result.Data!.CurrentTerm!.State.Should().Be("current");
        result.Data.ShouldPlanNextTerm.Should().Be(shouldPlan);
        if (shouldPlan)
        {
            result.Data.DaysUntilCurrentTermEnds.Should().Be(44);
            result.Data.SuggestedNextTerm!.StartDate.Should().Be(new DateOnly(2027, 9, 13));
        }
        else
        {
            result.Data.SuggestedNextTerm.Should().BeNull();
        }
    }

    [Fact]
    public async Task Planning_WhenNextTermExists_ShouldNotSuggest()
    {
        _termDal.Setup(d => d.GetListAsync(It.IsAny<Expression<Func<AcademicTerm, bool>>>()))
            .ReturnsAsync(new List<AcademicTerm> { Term(TermStart, TermEnd), Term(new DateOnly(2027, 9, 13), new DateOnly(2028, 8, 26), "Sonraki") });
        var manager = CreateManager(new DateOnly(2027, 8, 1));

        var result = await manager.GetTermPlanningAsync();

        result.Data!.NextTerm!.Name.Should().Be("Sonraki");
        result.Data.ShouldPlanNextTerm.Should().BeFalse();
    }

    [Fact]
    public async Task SchoolDays_WithoutRangeAndWithoutCurrentTerm_ShouldFail()
    {
        _termDal.Setup(d => d.GetListAsync(It.IsAny<Expression<Func<AcademicTerm, bool>>>())).ReturnsAsync(new List<AcademicTerm>());
        var manager = CreateManager(new DateOnly(2026, 10, 20));

        var result = await manager.GetSchoolDaysAsync(null, null);

        result.Success.Should().BeFalse();
    }

    [Fact]
    public async Task ImportHolidays_UnknownKey_ShouldFail()
    {
        var manager = CreateManager(new DateOnly(2026, 10, 20));

        var result = await manager.ImportHolidaysAsync(new ImportHolidaysDto { From = TermStart, To = TermEnd, Keys = { "2030-uydurma" } });

        result.Success.Should().BeFalse();
    }
}
