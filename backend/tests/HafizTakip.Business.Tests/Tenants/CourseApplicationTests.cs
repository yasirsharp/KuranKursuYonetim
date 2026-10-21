using System.Linq.Expressions;
using System.Text.RegularExpressions;
using HafizTakip.Business.Abstract;
using HafizTakip.Business.Concrete;
using HafizTakip.Business.Tests.Common;
using HafizTakip.Business.ValidationRules.FluentValidation;
using HafizTakip.Core.Constants;
using HafizTakip.Core.DataAccess;
using HafizTakip.Core.Entities.Platform;
using HafizTakip.Core.Enums;
using HafizTakip.Core.Utilities.IoC;
using HafizTakip.Core.Utilities.Results;
using HafizTakip.Core.Utilities.Security.Otp;
using HafizTakip.Core.Utilities.Sms;
using HafizTakip.DataAccess.Abstract;
using HafizTakip.Entities.Concrete;
using HafizTakip.Entities.Dtos;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;

namespace HafizTakip.Business.Tests.Tenants;

public class CourseApplicationTests
{
    private const string Phone = "5321112233";

    private readonly Mock<ITenantDal> _tenantDal = new();
    private readonly Mock<ITenantSettingDal> _settingDal = new();
    private readonly Mock<IUserDal> _userDal = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = UnitOfWorks.PassThrough();
    private readonly Mock<IAuthService> _authService = new();
    private readonly Mock<ITenantContext> _tenantContext = new();
    private readonly Mock<ISmsSender> _smsSender = new();
    private readonly Mock<ICurrentUser> _currentUser = new();
    private readonly MemoryCache _cache = new(new MemoryCacheOptions());
    private readonly List<(string Phone, string Message)> _sms = new();
    private readonly List<Tenant> _addedTenants = new();
    private readonly List<User> _addedUsers = new();

    public CourseApplicationTests()
    {
        _smsSender
            .Setup(s => s.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback<string, string, CancellationToken>((phone, message, _) => _sms.Add((phone, message)))
            .ReturnsAsync(true);
        _unitOfWork
            .Setup(u => u.ExecuteInTransactionAsync(It.IsAny<Func<Task<IDataResult<TokenResponseDto>>>>()))
            .Returns((Func<Task<IDataResult<TokenResponseDto>>> work) => work());
        _tenantDal.Setup(d => d.GetTakenSlugsAsync(It.IsAny<string>())).ReturnsAsync(new List<string>());
        _tenantDal.Setup(d => d.Add(It.IsAny<Tenant>())).Callback<Tenant>(_addedTenants.Add);
        _userDal.Setup(d => d.Add(It.IsAny<User>())).Callback<User>(_addedUsers.Add);
        _authService
            .Setup(a => a.CreateSessionAsync(It.IsAny<User>()))
            .ReturnsAsync((User u) => new TokenResponseDto { Token = "t", UserId = u.Id, TenantId = u.TenantId, TenantStatus = TenantStatus.Pending });
    }

    private CourseApplicationManager CreateApplications() => new(
        _tenantDal.Object, _settingDal.Object, _userDal.Object, _unitOfWork.Object, _authService.Object,
        _tenantContext.Object, _smsSender.Object, _cache, new OtpOptions(), TimeProvider.System);

    private TenantManager CreateTenants() => new(
        _tenantDal.Object, _settingDal.Object, _userDal.Object, _unitOfWork.Object, _cache,
        _currentUser.Object, _smsSender.Object, TimeProvider.System, NullLogger<TenantManager>.Instance);

    private static CourseApplicationDto Application(string code) => new()
    {
        PhoneNumber = "0532 111 22 33",
        Code = code,
        CourseName = "Yeşilvadi Kur'an Kursu",
        City = "Konya",
        District = "Selçuklu",
        AdminFirstName = "Kemal",
        AdminLastName = "Arslan"
    };

    private async Task<string> RequestCodeAsync()
    {
        (await CreateApplications().SendCodeAsync(new SendOtpDto { PhoneNumber = Phone })).Success.Should().BeTrue();
        return Regex.Match(_sms.Last().Message, @"\d{6}").Value;
    }

    private Tenant ExistingTenant(string status)
    {
        var tenant = new Tenant { Id = Guid.NewGuid(), Name = "Yeşilvadi", Status = status, IsActive = true };
        _tenantDal.Setup(d => d.GetAsync(It.IsAny<Expression<Func<Tenant, bool>>>())).ReturnsAsync(tenant);
        _tenantContext.Setup(c => c.TenantId).Returns(tenant.Id);
        _userDal.Setup(d => d.GetTenantAdminsAcrossTenantsAsync(tenant.Id))
            .ReturnsAsync(new List<User> { new() { TenantId = tenant.Id, PhoneNumber = Phone, Role = UserRole.TenantAdmin } });
        return tenant;
    }

    // ── Başvuru ────────────────────────────────────────────────────

    [Fact]
    public async Task SendCode_ShouldSendCodeEvenForUnregisteredPhone()
    {
        var code = await RequestCodeAsync();

        code.Should().MatchRegex(@"^\d{6}$");
        _sms.Single().Phone.Should().Be(Phone);
    }

    [Fact]
    public async Task Submit_WithValidCode_ShouldCreatePendingCourseAdminAndSession()
    {
        var code = await RequestCodeAsync();

        var result = await CreateApplications().SubmitAsync(Application(code));

        result.Success.Should().BeTrue();
        var tenant = _addedTenants.Single();
        tenant.Status.Should().Be(TenantStatus.Pending);
        tenant.Slug.Should().Be("yesilvadi-kuran-kursu");
        tenant.ContactPhone.Should().Be(Phone);
        tenant.SubmittedAt.Should().NotBeNull();

        var admin = _addedUsers.Single();
        admin.TenantId.Should().Be(tenant.Id);
        admin.Role.Should().Be(UserRole.TenantAdmin);
        admin.PhoneNumber.Should().Be(Phone);

        _settingDal.Verify(d => d.Add(It.Is<TenantSetting>(s => s.TenantId == tenant.Id)), Times.Once);
        result.Data.TenantStatus.Should().Be(TenantStatus.Pending);
    }

    [Fact]
    public async Task Submit_CodeIsSingleUse()
    {
        var code = await RequestCodeAsync();
        (await CreateApplications().SubmitAsync(Application(code))).Success.Should().BeTrue();

        var second = await CreateApplications().SubmitAsync(Application(code));

        second.Success.Should().BeFalse();
        _addedTenants.Should().ContainSingle();
    }

    [Fact]
    public async Task Submit_WrongCode_ShouldNotCreateAnything()
    {
        var code = await RequestCodeAsync();
        var wrong = code == "000000" ? "111111" : "000000";

        var result = await CreateApplications().SubmitAsync(Application(wrong));

        result.Success.Should().BeFalse();
        _addedTenants.Should().BeEmpty();
        _addedUsers.Should().BeEmpty();
    }

    [Fact]
    public async Task Submit_LoginCodeCannotBeUsedForApplication()
    {
        _userDal.Setup(d => d.GetLoginCandidatesByPhoneAsync(Phone))
            .ReturnsAsync(new List<User> { new() { Id = Guid.NewGuid(), TenantId = Guid.NewGuid(), PhoneNumber = Phone } });
        var auth = new AuthService(_userDal.Object, _tenantDal.Object, Mock.Of<IRefreshTokenDal>(), Mock.Of<HafizTakip.Core.Utilities.Security.Jwt.ITokenHelper>(),
            _cache, _smsSender.Object, Mock.Of<HafizTakip.Core.Utilities.Email.IEmailSender>(), new OtpOptions { ResendCooldownSeconds = 0 }, new HafizTakip.Core.Utilities.Security.Jwt.TokenOptions(), TimeProvider.System);
        await auth.SendOtpAsync(new SendOtpDto { PhoneNumber = Phone });
        var loginCode = Regex.Match(_sms.Last().Message, @"\d{6}").Value;

        var result = await CreateApplications().SubmitAsync(Application(loginCode));

        result.Success.Should().BeFalse();
        _addedTenants.Should().BeEmpty();
    }

    [Fact]
    public async Task Submit_WhenPhoneHasPendingApplication_ShouldReturnConflict()
    {
        var code = await RequestCodeAsync();
        _userDal.Setup(d => d.HasPendingApplicationAsync(Phone)).ReturnsAsync(true);

        var result = await CreateApplications().SubmitAsync(Application(code));

        result.Success.Should().BeFalse();
        result.ErrorType.Should().Be(ResultErrorType.Conflict);
        _addedTenants.Should().BeEmpty();
    }

    [Fact]
    public async Task Submit_TakenSlug_ShouldGetNumberSuffix()
    {
        var code = await RequestCodeAsync();
        _tenantDal.Setup(d => d.GetTakenSlugsAsync("yesilvadi-kuran-kursu"))
            .ReturnsAsync(new List<string> { "yesilvadi-kuran-kursu", "yesilvadi-kuran-kursu-2" });

        await CreateApplications().SubmitAsync(Application(code));

        _addedTenants.Single().Slug.Should().Be("yesilvadi-kuran-kursu-3");
    }

    [Fact]
    public void Validator_ShouldRequireCityDistrictAndAdminName()
    {
        var dto = Application("123456");
        dto.City = "";
        dto.District = " ";
        dto.AdminFirstName = "";

        var errors = new CourseApplicationValidator().Validate(dto).Errors.Select(e => e.PropertyName).ToList();

        errors.Should().Contain(new[] { nameof(dto.City), nameof(dto.District), nameof(dto.AdminFirstName) });
    }

    // ── Başvuru durumu ve yeniden gönderme ─────────────────────────

    [Fact]
    public async Task GetMine_ShouldReturnStatusAndRejectionReason()
    {
        var tenant = ExistingTenant(TenantStatus.Rejected);
        tenant.RejectionReason = "Adres bilgisi eksik.";

        var result = await CreateApplications().GetMineAsync();

        result.Data.Status.Should().Be(TenantStatus.Rejected);
        result.Data.RejectionReason.Should().Be("Adres bilgisi eksik.");
    }

    [Fact]
    public async Task Resubmit_RejectedApplication_ShouldReturnToPendingAndClearReview()
    {
        var tenant = ExistingTenant(TenantStatus.Rejected);
        tenant.RejectionReason = "Adres bilgisi eksik.";
        tenant.ReviewedAt = DateTime.UtcNow;
        _cache.Set(TenantCacheKeys.Status(tenant.Id), "eski");

        var result = await CreateApplications().ResubmitAsync(new CourseApplicationInfoDto
        {
            CourseName = "Yeşilvadi Kur'an Kursu", City = "Konya", District = "Selçuklu", Address = "Cami Sok. No: 3"
        });

        result.Success.Should().BeTrue();
        tenant.Status.Should().Be(TenantStatus.Pending);
        tenant.Address.Should().Be("Cami Sok. No: 3");
        tenant.RejectionReason.Should().BeNull();
        tenant.ReviewedAt.Should().BeNull();
        _cache.TryGetValue(TenantCacheKeys.Status(tenant.Id), out _).Should().BeFalse();
    }

    [Theory]
    [InlineData(TenantStatus.Pending)]
    [InlineData(TenantStatus.Active)]
    public async Task Resubmit_NotRejected_ShouldReturnConflict(string status)
    {
        ExistingTenant(status);

        var result = await CreateApplications().ResubmitAsync(new CourseApplicationInfoDto { CourseName = "X Kursu", City = "Konya", District = "Meram" });

        result.ErrorType.Should().Be(ResultErrorType.Conflict);
    }

    // ── Platform onayı ─────────────────────────────────────────────

    [Fact]
    public async Task Approve_Pending_ShouldActivateRecordReviewerAndNotifyAdmin()
    {
        var tenant = ExistingTenant(TenantStatus.Pending);
        var reviewer = Guid.NewGuid();
        _currentUser.Setup(c => c.UserId).Returns(reviewer);
        _cache.Set(TenantCacheKeys.Status(tenant.Id), "eski");

        var result = await CreateTenants().ApproveAsync(tenant.Id);

        result.Success.Should().BeTrue();
        tenant.Status.Should().Be(TenantStatus.Active);
        tenant.ReviewedBy.Should().Be(reviewer);
        tenant.ReviewedAt.Should().NotBeNull();
        _sms.Should().ContainSingle(s => s.Phone == Phone && s.Message.Contains("onaylandı"));
        _cache.TryGetValue(TenantCacheKeys.Status(tenant.Id), out _).Should().BeFalse();
    }

    [Theory]
    [InlineData(TenantStatus.Active)]
    [InlineData(TenantStatus.Rejected)]
    public async Task ApproveOrReject_NotPending_ShouldReturnConflict(string status)
    {
        var tenant = ExistingTenant(status);

        (await CreateTenants().ApproveAsync(tenant.Id)).ErrorType.Should().Be(ResultErrorType.Conflict);
        (await CreateTenants().RejectAsync(tenant.Id, new RejectCourseApplicationDto { Reason = "Eksik bilgi" }))
            .ErrorType.Should().Be(ResultErrorType.Conflict);
        _sms.Should().BeEmpty();
    }

    [Fact]
    public async Task Reject_Pending_ShouldStoreReasonAndSendItBySms()
    {
        var tenant = ExistingTenant(TenantStatus.Pending);

        var result = await CreateTenants().RejectAsync(tenant.Id, new RejectCourseApplicationDto { Reason = "  Adres bilgisi eksik.  " });

        result.Success.Should().BeTrue();
        tenant.Status.Should().Be(TenantStatus.Rejected);
        tenant.RejectionReason.Should().Be("Adres bilgisi eksik.");
        _sms.Should().ContainSingle(s => s.Message.Contains("Adres bilgisi eksik."));
    }

    [Fact]
    public async Task Approve_WhenSmsFails_ShouldStillApprove()
    {
        var tenant = ExistingTenant(TenantStatus.Pending);
        _smsSender
            .Setup(s => s.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("SMS servisi yanıt vermedi"));

        var result = await CreateTenants().ApproveAsync(tenant.Id);

        result.Success.Should().BeTrue();
        tenant.Status.Should().Be(TenantStatus.Active);
    }

    [Theory]
    [InlineData(TenantStatus.Pending, TenantStatus.Active)]
    [InlineData(TenantStatus.Active, TenantStatus.Pending)]
    [InlineData(TenantStatus.Rejected, TenantStatus.Suspended)]
    public async Task Update_CannotMoveIntoOrOutOfApplicationStatus(string current, string requested)
    {
        var tenant = ExistingTenant(current);

        var result = await CreateTenants().UpdateAsync(new UpdateTenantDto
        {
            Id = tenant.Id, Name = tenant.Name, Status = requested, IsActive = true
        });

        result.ErrorType.Should().Be(ResultErrorType.Conflict);
        tenant.Status.Should().Be(current);
    }

    [Fact]
    public void RejectValidator_ShouldRequireReason()
    {
        new RejectCourseApplicationValidator().Validate(new RejectCourseApplicationDto { Reason = " " }).IsValid.Should().BeFalse();
    }

    // ── Durum kuralları ────────────────────────────────────────────

    [Theory]
    [InlineData(TenantStatus.Pending, true, true, false)]
    [InlineData(TenantStatus.Rejected, true, true, false)]
    [InlineData(TenantStatus.Active, true, true, true)]
    [InlineData(TenantStatus.Trial, true, true, true)]
    [InlineData(TenantStatus.Active, false, false, false)]
    [InlineData(TenantStatus.Suspended, true, false, false)]
    [InlineData(TenantStatus.Cancelled, true, false, false)]
    public void TenantStatus_AccessRules(string status, bool isActive, bool canSignIn, bool fullAccess)
    {
        TenantStatus.CanSignIn(status, isActive).Should().Be(canSignIn);
        TenantStatus.HasFullAccess(status, isActive).Should().Be(fullAccess);
    }
}
