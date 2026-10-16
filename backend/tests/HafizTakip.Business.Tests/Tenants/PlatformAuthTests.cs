using HafizTakip.Business.Concrete;
using HafizTakip.Core.Entities.Platform;
using HafizTakip.Core.Enums;
using HafizTakip.Core.Utilities.Helpers;
using HafizTakip.Core.Utilities.Results;
using HafizTakip.Core.Utilities.Security.Hashing;
using HafizTakip.Core.Utilities.Security.Jwt;
using HafizTakip.DataAccess.Abstract;
using HafizTakip.Entities.Dtos;
using Microsoft.Extensions.Caching.Memory;

namespace HafizTakip.Business.Tests.Tenants;

public class PlatformAuthTests
{
    private const string Password = "Dogru-Sifre-123";

    // Testleri hızlandırmak için düşük iterasyon; doğrulama iterasyonu özetten okur
    private static readonly string StoredHash = PasswordHasher.Hash(Password, iterations: 1_000);

    private readonly Mock<IPlatformUserDal> _dal = new();
    private readonly Mock<ITokenHelper> _tokenHelper = new();
    private readonly MemoryCache _cache = new(new MemoryCacheOptions());
    private readonly PlatformUser _admin = new()
    {
        Id = Guid.NewGuid(), Email = "admin@hafiztakip.com", PasswordHash = StoredHash, Role = UserRole.SuperAdmin
    };

    public PlatformAuthTests()
    {
        _dal.Setup(d => d.GetByEmailAsync("admin@hafiztakip.com")).ReturnsAsync(_admin);
        _tokenHelper
            .Setup(t => t.CreateToken(It.IsAny<PlatformUser>(), It.IsAny<List<string>>(), It.IsAny<Guid>()))
            .Returns(new AccessToken { Token = "platform-token", Expiration = DateTime.UtcNow.AddHours(1) });
    }

    private PlatformAuthService Create() => new(_dal.Object, _tokenHelper.Object, _cache);

    [Fact]
    public async Task Login_CorrectPassword_ShouldIssueTokenWithoutTenant()
    {
        var result = await Create().LoginAsync(new PlatformLoginDto { Email = " Admin@HafizTakip.com ", Password = Password });

        result.Success.Should().BeTrue();
        result.Data.Token.Should().Be("platform-token");
        result.Data.Role.Should().Be(UserRole.SuperAdmin);
        _tokenHelper.Verify(t => t.CreateToken(_admin, It.Is<List<string>>(r => r.Single() == UserRole.SuperAdmin), Guid.Empty));
    }

    [Fact]
    public async Task Login_WrongPasswordOrUnknownEmail_ShouldReturnSameUnauthorizedMessage()
    {
        var wrong = await Create().LoginAsync(new PlatformLoginDto { Email = "admin@hafiztakip.com", Password = "yanlis" });
        var unknown = await Create().LoginAsync(new PlatformLoginDto { Email = "yok@hafiztakip.com", Password = Password });

        wrong.ErrorType.Should().Be(ResultErrorType.Unauthorized);
        unknown.ErrorType.Should().Be(ResultErrorType.Unauthorized);
        wrong.Message.Should().Be(unknown.Message);
    }

    [Fact]
    public async Task Login_AfterTooManyFailures_ShouldLockEvenWithCorrectPassword()
    {
        for (var i = 0; i < PlatformAuthService.MaxFailedAttempts; i++)
            await Create().LoginAsync(new PlatformLoginDto { Email = "admin@hafiztakip.com", Password = "yanlis" });

        var result = await Create().LoginAsync(new PlatformLoginDto { Email = "admin@hafiztakip.com", Password = Password });

        result.ErrorType.Should().Be(ResultErrorType.TooManyRequests);
        _tokenHelper.Verify(t => t.CreateToken(It.IsAny<PlatformUser>(), It.IsAny<List<string>>(), It.IsAny<Guid>()), Times.Never);
    }

    [Fact]
    public async Task Login_SuccessShouldResetFailureCounter()
    {
        for (var i = 0; i < PlatformAuthService.MaxFailedAttempts - 1; i++)
            await Create().LoginAsync(new PlatformLoginDto { Email = "admin@hafiztakip.com", Password = "yanlis" });
        (await Create().LoginAsync(new PlatformLoginDto { Email = "admin@hafiztakip.com", Password = Password })).Success.Should().BeTrue();

        await Create().LoginAsync(new PlatformLoginDto { Email = "admin@hafiztakip.com", Password = "yanlis" });

        (await Create().LoginAsync(new PlatformLoginDto { Email = "admin@hafiztakip.com", Password = Password })).Success.Should().BeTrue();
    }

    [Fact]
    public async Task Login_SeedPlaceholderHash_ShouldNeverMatch()
    {
        _admin.PasswordHash = "hashed_password_here";

        var result = await Create().LoginAsync(new PlatformLoginDto { Email = "admin@hafiztakip.com", Password = "hashed_password_here" });

        result.Success.Should().BeFalse();
    }

    // ── Parola özeti ───────────────────────────────────────────────

    [Fact]
    public void PasswordHasher_ShouldUseRandomSaltAndVerify()
    {
        var first = PasswordHasher.Hash(Password, iterations: 1_000);
        var second = PasswordHasher.Hash(Password, iterations: 1_000);

        first.Should().StartWith("pbkdf2-sha256$1000$").And.NotBe(second);
        PasswordHasher.Verify(Password, first).Should().BeTrue();
        PasswordHasher.Verify(Password + "x", first).Should().BeFalse();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("pbkdf2-sha256$abc$x$y")]
    [InlineData("pbkdf2-sha256$1000$***$***")]
    [InlineData("md5$1$a$b")]
    public void PasswordHasher_MalformedHash_ShouldReturnFalse(string? stored)
    {
        PasswordHasher.Verify(Password, stored).Should().BeFalse();
    }

    // ── Kısa ad (slug) ─────────────────────────────────────────────

    [Theory]
    [InlineData("Merkez Kur'an Kursu", "merkez-kuran-kursu")]
    [InlineData("İSTANBUL Çağlayan Şükrü Öğüt", "istanbul-caglayan-sukru-ogut")]
    [InlineData("  Ilgın -- Hafızlık   Merkezi!  ", "ilgin-hafizlik-merkezi")]
    [InlineData("Kur’an Kursu (2. Şube)", "kuran-kursu-2-sube")]
    [InlineData("!!!", "kurs")]
    [InlineData("", "kurs")]
    public void Slug_From(string name, string expected)
    {
        SlugHelper.From(name).Should().Be(expected);
    }

    [Fact]
    public void Slug_ShouldBeTruncated()
    {
        SlugHelper.From(new string('a', 120)).Should().HaveLength(SlugHelper.MaxLength);
    }

    [Fact]
    public void Slug_MakeUnique_ShouldPickSmallestFreeSuffix()
    {
        SlugHelper.MakeUnique("kurs", Array.Empty<string>()).Should().Be("kurs");
        SlugHelper.MakeUnique("kurs", new[] { "kurs", "kurs-3" }).Should().Be("kurs-2");
    }
}
