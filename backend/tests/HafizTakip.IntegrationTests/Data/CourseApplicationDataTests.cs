using System.Text.RegularExpressions;
using HafizTakip.Business.Abstract;
using HafizTakip.Business.Concrete;
using HafizTakip.Core.Entities.Platform;
using HafizTakip.Core.Enums;
using HafizTakip.DataAccess.Concrete.EntityFramework;
using HafizTakip.Core.Utilities.Results;
using HafizTakip.Core.Utilities.Security.Otp;
using HafizTakip.Core.Utilities.Sms;
using HafizTakip.Entities.Concrete;
using HafizTakip.Entities.Dtos;
using HafizTakip.IntegrationTests.Infrastructure;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using static HafizTakip.IntegrationTests.Infrastructure.PostgresDatabaseFixture;

namespace HafizTakip.IntegrationTests.Data;

[Collection(PostgresCollection.Name)]
public class CourseApplicationDataTests
{
    private readonly PostgresDatabaseFixture _db;

    public CourseApplicationDataTests(PostgresDatabaseFixture db) => _db = db;

    /// <summary>Sorgu testleri için başvuru kaydı (istek henüz bir kursa bağlı değil gibi, anonim bağlamda).</summary>
    private async Task<(Guid TenantId, string Phone)> CreateApplicationAsync(string status, string slug, bool deleted = false)
    {
        var phone = "59" + Random.Shared.Next(10_000_000, 99_999_999);
        var tenant = new Tenant
        {
            Id = Guid.NewGuid(), Name = "Başvuru " + slug, Slug = slug, Status = status, IsActive = true,
            City = "Konya", District = "Selçuklu", SubmittedAt = DateTime.UtcNow
        };

        await using (var context = _db.CreateContext(Guid.Empty))
        {
            context.Tenants.Add(tenant);
            await context.SaveChangesAsync();
            context.Users.Add(new User
            {
                TenantId = tenant.Id, FirstName = "Kemal", LastName = "Arslan", PhoneNumber = phone, Role = UserRole.TenantAdmin
            });
            await context.SaveChangesAsync();
        }

        if (deleted)
            await _db.ExecuteSqlAsync($"UPDATE tenants SET is_deleted = true, deleted_at = now() WHERE id = '{tenant.Id}';");

        return (tenant.Id, phone);
    }

    [PostgresFact]
    public async Task Submit_ShouldCreateCourseSettingsAndAdminInOneGo()
    {
        var sms = new CapturingSmsSender();
        var phone = "59" + Random.Shared.Next(10_000_000, 99_999_999);
        await using var context = _db.CreateContext(Guid.Empty);
        var cache = new MemoryCache(new MemoryCacheOptions());
        var manager = new CourseApplicationManager(
            new EfTenantDal(context), new EfTenantSettingDal(context), new EfUserDal(context), new EfUnitOfWork(context),
            new StubSessionAuthService(), new TestTenantContext(Guid.Empty), sms, cache, new OtpOptions(), TimeProvider.System);

        (await manager.SendCodeAsync(new SendOtpDto { PhoneNumber = phone })).Success.Should().BeTrue();
        var result = await manager.SubmitAsync(new CourseApplicationDto
        {
            PhoneNumber = phone, Code = sms.LastCode(), CourseName = "Entegrasyon Kur'an Kursu",
            City = "Konya", District = "Meram", AdminFirstName = "Kemal", AdminLastName = "Arslan"
        });

        result.Success.Should().BeTrue(result.Message);
        var tenantId = result.Data.TenantId;
        (await _db.ScalarAsync<string>($"SELECT status FROM tenants WHERE id = '{tenantId}'")).Should().Be(TenantStatus.Pending);
        (await _db.ScalarAsync<long>($"SELECT count(*) FROM tenant_settings WHERE tenant_id = '{tenantId}'")).Should().Be(1);
        (await _db.ScalarAsync<string>($"SELECT role FROM users WHERE tenant_id = '{tenantId}' AND phone_number = '{phone}'"))
            .Should().Be(UserRole.TenantAdmin);
    }

    [PostgresFact]
    public async Task PlatformAddTenant_ShouldCreateCourseWithSettings()
    {
        await using var context = _db.CreateContext(Guid.Empty);
        var manager = new TenantManager(
            new EfTenantDal(context), new EfTenantSettingDal(context), new EfUserDal(context), new EfUnitOfWork(context),
            new MemoryCache(new MemoryCacheOptions()), new FixedCurrentUser(Guid.NewGuid(), UserRole.SuperAdmin),
            new CapturingSmsSender(), TimeProvider.System, NullLogger<TenantManager>.Instance);

        var result = await manager.AddAsync(new CreateTenantDto { Name = "Platform Kursu", Slug = "platform-kursu-it" });

        result.Success.Should().BeTrue(result.Message);
        (await _db.ScalarAsync<long>(
            "SELECT count(*) FROM tenant_settings s JOIN tenants t ON t.id = s.tenant_id WHERE t.slug = 'platform-kursu-it'"))
            .Should().Be(1);
    }

    [PostgresFact]
    public async Task UnknownTenantStatus_ShouldBeRejectedByCheckConstraint()
    {
        var act = () => _db.ExecuteSqlAsync(
            $"INSERT INTO tenants (id, name, slug, status) VALUES ('{Guid.NewGuid()}', 'Hatalı', 'hatali-durum', 'onay_bekliyor');");

        await act.Should().ThrowAsync<Npgsql.PostgresException>().Where(e => e.SqlState == "23514");
    }

    [PostgresFact]
    public async Task LoginCandidates_ShouldIncludePendingAndRejectedButNotSuspended()
    {
        var (pendingTenant, pendingPhone) = await CreateApplicationAsync(TenantStatus.Pending, "aday-bekleyen");
        var (rejectedTenant, rejectedPhone) = await CreateApplicationAsync(TenantStatus.Rejected, "aday-reddedilen");
        var (_, suspendedPhone) = await CreateApplicationAsync(TenantStatus.Suspended, "aday-askida");

        await using var context = _db.CreateContext(Guid.Empty);
        var dal = new EfUserDal(context);

        (await dal.GetLoginCandidatesByPhoneAsync(pendingPhone)).Should().ContainSingle(u => u.TenantId == pendingTenant);
        (await dal.GetLoginCandidatesByPhoneAsync(rejectedPhone)).Should().ContainSingle(u => u.TenantId == rejectedTenant);
        (await dal.GetLoginCandidatesByPhoneAsync(suspendedPhone)).Should().BeEmpty();
    }

    [PostgresFact]
    public async Task HasPendingApplication_ShouldOnlyCountPendingCourses()
    {
        var (_, pendingPhone) = await CreateApplicationAsync(TenantStatus.Pending, "bekleyen-kontrol");
        var (_, rejectedPhone) = await CreateApplicationAsync(TenantStatus.Rejected, "reddedilen-kontrol");

        await using var context = _db.CreateContext(Guid.Empty);
        var dal = new EfUserDal(context);

        (await dal.HasPendingApplicationAsync(pendingPhone)).Should().BeTrue();
        (await dal.HasPendingApplicationAsync(rejectedPhone)).Should().BeFalse();
    }

    [PostgresFact]
    public async Task TenantAdminsAcrossTenants_ShouldWorkWithoutTenantContext()
    {
        var (tenantId, phone) = await CreateApplicationAsync(TenantStatus.Pending, "yonetici-bul");

        await using var context = _db.CreateContext(Guid.Empty);
        var admins = await new EfUserDal(context).GetTenantAdminsAcrossTenantsAsync(tenantId);

        admins.Should().ContainSingle().Which.PhoneNumber.Should().Be(phone);
    }

    [PostgresFact]
    public async Task TakenSlugs_ShouldIncludeDeletedCoursesAndSuffixesOnly()
    {
        await CreateApplicationAsync(TenantStatus.Pending, "slug-test");
        await CreateApplicationAsync(TenantStatus.Pending, "slug-test-2", deleted: true);
        await CreateApplicationAsync(TenantStatus.Pending, "slug-testere");

        await using var context = _db.CreateContext(Guid.Empty);
        var taken = await new EfTenantDal(context).GetTakenSlugsAsync("slug-test");

        taken.Should().BeEquivalentTo("slug-test", "slug-test-2");
    }

    [PostgresFact]
    public async Task PlatformUser_ShouldBeFoundByEmailCaseInsensitively()
    {
        await _db.ExecuteSqlAsync(
            "INSERT INTO platform_users (email, password_hash) VALUES ('Yonetici@HafizTakip.com', 'x') ON CONFLICT (email) DO NOTHING;");

        await using var context = _db.CreateContext(Guid.Empty);
        var user = await new EfPlatformUserDal(context).GetByEmailAsync("yonetici@hafiztakip.COM");

        user.Should().NotBeNull();
        user!.Role.Should().Be(UserRole.SuperAdmin);
    }
}

internal sealed class CapturingSmsSender : ISmsSender
{
    private readonly List<string> _messages = new();

    public Task<bool> SendAsync(string phoneNumber, string message, CancellationToken cancellationToken = default)
    {
        _messages.Add(message);
        return Task.FromResult(true);
    }

    public string LastCode() => Regex.Match(_messages.Last(), @"\d{6}").Value;
}

/// <summary>Oturum üretimini taklit eder; başvuru testinde yalnızca kayıtların yazılması ölçülür.</summary>
internal sealed class StubSessionAuthService : IAuthService
{
    public Task<TokenResponseDto> CreateSessionAsync(User user) =>
        Task.FromResult(new TokenResponseDto { UserId = user.Id, TenantId = user.TenantId, Role = user.Role });

    public Task<IResult> SendOtpAsync(SendOtpDto sendOtpDto) => throw new NotSupportedException();
    public Task<IDataResult<TokenResponseDto>> VerifyOtpAsync(VerifyOtpDto verifyOtpDto) => throw new NotSupportedException();
    public Task<IDataResult<TokenResponseDto>> RefreshAsync(RefreshTokenRequestDto request) => throw new NotSupportedException();
    public Task<IResult> LogoutAsync(RefreshTokenRequestDto request) => throw new NotSupportedException();
}
