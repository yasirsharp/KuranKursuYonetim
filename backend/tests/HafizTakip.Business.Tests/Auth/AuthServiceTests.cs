using System.Linq.Expressions;
using System.Text.RegularExpressions;
using HafizTakip.Business.Concrete;
using HafizTakip.Core.Entities.Platform;
using HafizTakip.Core.Utilities.Results;
using HafizTakip.Core.Utilities.Security.Jwt;
using HafizTakip.Core.Utilities.Security.Otp;
using HafizTakip.Core.Utilities.Sms;
using HafizTakip.DataAccess.Abstract;
using HafizTakip.Entities.Concrete;
using HafizTakip.Entities.Dtos;
using Microsoft.Extensions.Caching.Memory;

namespace HafizTakip.Business.Tests.Auth;

public class AuthServiceTests
{
    private const string Phone = "5321112233";

    private readonly Mock<IUserDal> _userDal = new();
    private readonly Mock<ITenantDal> _tenantDal = new();
    private readonly Mock<IRefreshTokenDal> _refreshTokenDal = new();
    private readonly Mock<ITokenHelper> _tokenHelper = new();
    private readonly Mock<ISmsSender> _smsSender = new();
    private readonly MemoryCache _cache = new(new MemoryCacheOptions());
    private readonly OtpOptions _otpOptions = new();
    private readonly List<string> _sentMessages = new();
    private readonly Guid _tenantA = Guid.NewGuid();
    private readonly Guid _tenantB = Guid.NewGuid();

    public AuthServiceTests()
    {
        _smsSender
            .Setup(s => s.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback<string, string, CancellationToken>((_, message, _) => _sentMessages.Add(message))
            .ReturnsAsync(true);

        _tokenHelper
            .Setup(t => t.CreateToken(It.IsAny<PlatformUser>(), It.IsAny<List<string>>(), It.IsAny<Guid>()))
            .Returns(new AccessToken { Token = "access-token", Expiration = DateTime.UtcNow.AddHours(1) });

        _userDal.Setup(d => d.GetLoginCandidatesByPhoneAsync(It.IsAny<string>())).ReturnsAsync(new List<User>());
    }

    private AuthService CreateService() => new(
        _userDal.Object, _tenantDal.Object, _refreshTokenDal.Object, _tokenHelper.Object,
        _cache, _smsSender.Object, _otpOptions, new TokenOptions { RefreshTokenExpirationDays = 30 }, TimeProvider.System);

    private User RegisterUser(Guid tenantId, string role = "head_teacher")
    {
        var user = new User { Id = Guid.NewGuid(), TenantId = tenantId, PhoneNumber = Phone, Role = role, FirstName = "Ahmet" };
        var list = new List<User> { user };
        _userDal.Setup(d => d.GetLoginCandidatesByPhoneAsync(Phone)).ReturnsAsync(() => list.ToList());
        return user;
    }

    private string LastSentCode() => Regex.Match(_sentMessages.Last(), @"\d{6}").Value;

    [Fact]
    public async Task SendOtp_UnknownPhone_ShouldReturnGenericSuccessWithoutSendingSms()
    {
        var result = await CreateService().SendOtpAsync(new SendOtpDto { PhoneNumber = "0532 111 22 33" });

        result.Success.Should().BeTrue();
        _smsSender.Verify(s => s.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SendOtp_KnownPhone_ShouldSendRandomSixDigitCode()
    {
        RegisterUser(_tenantA);

        var result = await CreateService().SendOtpAsync(new SendOtpDto { PhoneNumber = Phone });

        result.Success.Should().BeTrue();
        _sentMessages.Should().ContainSingle();
        LastSentCode().Should().MatchRegex(@"^\d{6}$");
    }

    [Fact]
    public async Task SendOtp_TwiceWithinCooldown_ShouldReturnTooManyRequests()
    {
        RegisterUser(_tenantA);
        var service = CreateService();

        await service.SendOtpAsync(new SendOtpDto { PhoneNumber = Phone });
        var second = await service.SendOtpAsync(new SendOtpDto { PhoneNumber = Phone });

        second.ErrorType.Should().Be(ResultErrorType.TooManyRequests);
        _sentMessages.Should().HaveCount(1);
    }

    [Fact]
    public async Task SendOtp_HourlyLimit_ShouldApplyEvenForUnknownPhones()
    {
        _otpOptions.ResendCooldownSeconds = 0;
        _otpOptions.MaxSendsPerHour = 2;
        var service = CreateService();

        (await service.SendOtpAsync(new SendOtpDto { PhoneNumber = Phone })).Success.Should().BeTrue();
        (await service.SendOtpAsync(new SendOtpDto { PhoneNumber = Phone })).Success.Should().BeTrue();
        var third = await service.SendOtpAsync(new SendOtpDto { PhoneNumber = Phone });

        third.ErrorType.Should().Be(ResultErrorType.TooManyRequests);
    }

    [Fact]
    public async Task VerifyOtp_CorrectCode_ShouldIssueTokensAndStoreOnlyRefreshTokenHash()
    {
        var user = RegisterUser(_tenantA);
        var service = CreateService();
        await service.SendOtpAsync(new SendOtpDto { PhoneNumber = Phone });
        RefreshToken? stored = null;
        _refreshTokenDal.Setup(d => d.AddAsync(It.IsAny<RefreshToken>())).Callback<RefreshToken>(t => stored = t);

        var result = await service.VerifyOtpAsync(new VerifyOtpDto { PhoneNumber = Phone, Code = LastSentCode() });

        result.Success.Should().BeTrue();
        result.Data.Token.Should().Be("access-token");
        result.Data.UserId.Should().Be(user.Id);
        result.Data.RefreshToken.Should().NotBeNullOrEmpty();
        stored.Should().NotBeNull();
        stored!.UserId.Should().Be(user.Id);
        stored.TenantId.Should().Be(_tenantA);
        stored.TokenHash.Should().NotBe(result.Data.RefreshToken);
    }

    [Fact]
    public async Task VerifyOtp_CodeShouldBeSingleUse()
    {
        RegisterUser(_tenantA);
        var service = CreateService();
        await service.SendOtpAsync(new SendOtpDto { PhoneNumber = Phone });
        var code = LastSentCode();

        (await service.VerifyOtpAsync(new VerifyOtpDto { PhoneNumber = Phone, Code = code })).Success.Should().BeTrue();
        var reuse = await service.VerifyOtpAsync(new VerifyOtpDto { PhoneNumber = Phone, Code = code });

        reuse.ErrorType.Should().Be(ResultErrorType.Unauthorized);
    }

    [Fact]
    public async Task VerifyOtp_TooManyWrongAttempts_ShouldInvalidateCode()
    {
        _otpOptions.MaxVerifyAttempts = 2;
        RegisterUser(_tenantA);
        var service = CreateService();
        await service.SendOtpAsync(new SendOtpDto { PhoneNumber = Phone });
        var correct = LastSentCode();
        var wrong = correct == "000000" ? "111111" : "000000";

        (await service.VerifyOtpAsync(new VerifyOtpDto { PhoneNumber = Phone, Code = wrong })).ErrorType.Should().Be(ResultErrorType.Unauthorized);
        (await service.VerifyOtpAsync(new VerifyOtpDto { PhoneNumber = Phone, Code = wrong })).ErrorType.Should().Be(ResultErrorType.Unauthorized);
        var afterLimit = await service.VerifyOtpAsync(new VerifyOtpDto { PhoneNumber = Phone, Code = correct });

        afterLimit.Success.Should().BeFalse("doğru kod bile deneme sınırı aşıldıktan sonra kabul edilmemeli");
    }

    [Fact]
    public async Task VerifyOtp_PhoneInTwoTenants_ShouldAskForTenantThenLoginToChosenTenant()
    {
        var userA = new User { Id = Guid.NewGuid(), TenantId = _tenantA, PhoneNumber = Phone, Role = "head_teacher" };
        var userB = new User { Id = Guid.NewGuid(), TenantId = _tenantB, PhoneNumber = Phone, Role = "parent" };
        _userDal.Setup(d => d.GetLoginCandidatesByPhoneAsync(Phone)).ReturnsAsync(() => new List<User> { userA, userB });
        _tenantDal.Setup(d => d.GetListAsync(It.IsAny<Expression<Func<Tenant, bool>>>()))
            .ReturnsAsync(new List<Tenant> { new() { Id = _tenantA, Name = "A Kursu" }, new() { Id = _tenantB, Name = "B Kursu" } });
        var service = CreateService();
        await service.SendOtpAsync(new SendOtpDto { PhoneNumber = Phone });
        var code = LastSentCode();

        var first = await service.VerifyOtpAsync(new VerifyOtpDto { PhoneNumber = Phone, Code = code });

        first.Success.Should().BeTrue();
        first.Data.RequiresTenantSelection.Should().BeTrue();
        first.Data.Token.Should().BeEmpty();
        first.Data.Tenants.Select(t => t.TenantName).Should().BeEquivalentTo("A Kursu", "B Kursu");

        var second = await service.VerifyOtpAsync(new VerifyOtpDto { PhoneNumber = Phone, Code = code, TenantId = _tenantB });

        second.Success.Should().BeTrue();
        second.Data.TenantId.Should().Be(_tenantB);
        second.Data.Role.Should().Be("parent");
    }

    [Fact]
    public async Task Refresh_RevokedTokenReuse_ShouldRevokeAllUserSessions()
    {
        var userId = Guid.NewGuid();
        _refreshTokenDal.Setup(d => d.GetByHashAcrossTenantsAsync(It.IsAny<string>()))
            .ReturnsAsync(new RefreshToken { UserId = userId, RevokedAt = DateTime.UtcNow.AddMinutes(-5), ExpiresAt = DateTime.UtcNow.AddDays(1) });

        var result = await CreateService().RefreshAsync(new RefreshTokenRequestDto { RefreshToken = "stolen" });

        result.ErrorType.Should().Be(ResultErrorType.Unauthorized);
        _refreshTokenDal.Verify(d => d.RevokeAllForUserAsync(userId, It.IsAny<DateTime>()), Times.Once);
    }

    [Fact]
    public async Task Refresh_ValidToken_ShouldRotate()
    {
        var user = new User { Id = Guid.NewGuid(), TenantId = _tenantA, Role = "head_teacher" };
        var current = new RefreshToken { UserId = user.Id, TenantId = _tenantA, ExpiresAt = DateTime.UtcNow.AddDays(1) };
        _refreshTokenDal.Setup(d => d.GetByHashAcrossTenantsAsync(It.IsAny<string>())).ReturnsAsync(current);
        _userDal.Setup(d => d.GetByIdAcrossTenantsAsync(user.Id)).ReturnsAsync(user);
        _tenantDal.Setup(d => d.GetAsync(It.IsAny<Expression<Func<Tenant, bool>>>()))
            .ReturnsAsync(new Tenant { Id = _tenantA, IsActive = true });
        RefreshToken? replacement = null;
        _refreshTokenDal.Setup(d => d.RotateAsync(current, It.IsAny<RefreshToken>()))
            .Callback<RefreshToken, RefreshToken>((_, r) => replacement = r);

        var result = await CreateService().RefreshAsync(new RefreshTokenRequestDto { RefreshToken = "valid" });

        result.Success.Should().BeTrue();
        current.RevokedAt.Should().NotBeNull();
        replacement.Should().NotBeNull();
        current.ReplacedByTokenHash.Should().Be(replacement!.TokenHash);
        result.Data.RefreshToken.Should().NotBe("valid");
    }

    [Fact]
    public async Task Refresh_ExpiredToken_ShouldBeUnauthorized()
    {
        _refreshTokenDal.Setup(d => d.GetByHashAcrossTenantsAsync(It.IsAny<string>()))
            .ReturnsAsync(new RefreshToken { UserId = Guid.NewGuid(), ExpiresAt = DateTime.UtcNow.AddMinutes(-1) });

        var result = await CreateService().RefreshAsync(new RefreshTokenRequestDto { RefreshToken = "old" });

        result.ErrorType.Should().Be(ResultErrorType.Unauthorized);
        _refreshTokenDal.Verify(d => d.RotateAsync(It.IsAny<RefreshToken>(), It.IsAny<RefreshToken>()), Times.Never);
    }

    [Theory]
    [InlineData("pending", true, true)]
    [InlineData("rejected", true, true)]
    [InlineData("active", true, true)]
    [InlineData("suspended", true, false)]
    [InlineData("active", false, false)]
    public async Task Refresh_ShouldFollowTenantSignInRules(string status, bool isActive, bool allowed)
    {
        var user = new User { Id = Guid.NewGuid(), TenantId = _tenantA, Role = "tenant_admin" };
        _refreshTokenDal.Setup(d => d.GetByHashAcrossTenantsAsync(It.IsAny<string>()))
            .ReturnsAsync(new RefreshToken { UserId = user.Id, TenantId = _tenantA, ExpiresAt = DateTime.UtcNow.AddDays(1) });
        _userDal.Setup(d => d.GetByIdAcrossTenantsAsync(user.Id)).ReturnsAsync(user);
        _tenantDal.Setup(d => d.GetAsync(It.IsAny<Expression<Func<Tenant, bool>>>()))
            .ReturnsAsync(new Tenant { Id = _tenantA, Status = status, IsActive = isActive });

        var result = await CreateService().RefreshAsync(new RefreshTokenRequestDto { RefreshToken = "valid" });

        result.Success.Should().Be(allowed);
        if (allowed) result.Data.TenantStatus.Should().Be(status);
        else _refreshTokenDal.Verify(d => d.RevokeAllForUserAsync(user.Id, It.IsAny<DateTime>()), Times.Once);
    }
}
