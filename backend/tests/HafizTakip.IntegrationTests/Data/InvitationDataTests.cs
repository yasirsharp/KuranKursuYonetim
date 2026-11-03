using HafizTakip.Business.Concrete;
using HafizTakip.Core.Enums;
using HafizTakip.Core.Utilities.Results;
using HafizTakip.Core.Utilities.Security.Otp;
using HafizTakip.DataAccess.Concrete.EntityFramework;
using HafizTakip.Entities.Dtos;
using HafizTakip.IntegrationTests.Infrastructure;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;

namespace HafizTakip.IntegrationTests.Data;

/// <summary>Davetle personel katılımı: yönetici daveti → herkese açık sayfa → SMS kodu → istek → onay.</summary>
[Collection(PostgresCollection.Name)]
public class InvitationDataTests
{
    private readonly PostgresDatabaseFixture _db;
    private readonly MemoryCache _cache = new(new MemoryCacheOptions());
    private readonly CapturingSmsSender _sms = new();

    public InvitationDataTests(PostgresDatabaseFixture db) => _db = db;

    private static string NewPhone() => "55" + Random.Shared.Next(10_000_000, 99_999_999);

    /// <summary>Her test kendi kursunu kullanır (davet kodları ve numaralar çakışmasın).</summary>
    private async Task<(Guid TenantId, Guid AdminId)> CreateCourseAsync(string status = TenantStatus.Active)
    {
        var tenantId = Guid.NewGuid();
        var adminId = Guid.NewGuid();
        await _db.ExecuteSqlAsync($"""
            INSERT INTO tenants (id, name, slug, status, is_active, city, district)
                VALUES ('{tenantId}', 'Davet Kursu', 'davet-{tenantId:N}', '{status}', true, 'Konya', 'Meram');
            INSERT INTO users (id, tenant_id, first_name, last_name, phone_number, role)
                VALUES ('{adminId}', '{tenantId}', 'Müdür', 'Bey', '{NewPhone()}', 'tenant_admin');
            """);
        return (tenantId, adminId);
    }

    private InvitationManager Manager(HafizTakipDbContext context, TestTenantContext tenant, Guid? userId = null, string role = UserRole.TenantAdmin) => new(
        new EfInvitationDal(context), new EfJoinRequestDal(context), new EfUserDal(context), new EfUnitOfWork(context),
        tenant, new FixedCurrentUser(userId ?? Guid.NewGuid(), role), _sms, new NullEmailSender(),
        _cache, new OtpOptions { ResendCooldownSeconds = 0 }, TimeProvider.System, NullLogger<InvitationManager>.Instance);

    /// <summary>Yönetici olarak davet oluşturur.</summary>
    private async Task<InvitationDto> InviteAsync(Guid tenantId, Guid adminId, string role = UserRole.HeadTeacher)
    {
        await using var context = _db.CreateContext(tenantId);
        var result = await Manager(context, new TestTenantContext(tenantId), adminId).CreateAsync(new CreateInvitationDto { Role = role });
        result.Success.Should().BeTrue(result.Message);
        return result.Data;
    }

    /// <summary>Anonim kişi olarak (kurs bağlamı yok) davetle katılır.</summary>
    private async Task<IResult> JoinAsync(string code, string phone, string firstName = "Yusuf", string? email = null)
    {
        // Uygulamadaki gibi DbContext ile servis aynı (istek başına) kurs bağlamını paylaşır
        var anonymous = new TestTenantContext(Guid.Empty);
        await using var context = _db.CreateContext(anonymous);
        var manager = Manager(context, anonymous);
        var sent = await manager.SendJoinCodeAsync(code, new SendOtpDto { PhoneNumber = phone });
        if (!sent.Success) return sent;
        return await manager.JoinAsync(code, new JoinWithInvitationDto
        {
            FirstName = firstName, LastName = "Kaya", PhoneNumber = phone, Email = email, Code = _sms.LastCode()
        });
    }

    private async Task<IDataResult<InvitationPublicDto>> GetPublicAsync(string code)
    {
        await using var context = _db.CreateContext(Guid.Empty);
        return await Manager(context, new TestTenantContext(Guid.Empty)).GetPublicAsync(code);
    }

    [PostgresFact]
    public async Task FullFlow_InviteJoinApprove_ShouldAddStaffWhoCanSignInByPhone()
    {
        var (tenantId, adminId) = await CreateCourseAsync();
        var invitation = await InviteAsync(tenantId, adminId);
        invitation.Code.Should().MatchRegex("^[A-Z2-9]{8}$");

        var info = await GetPublicAsync(invitation.Code.ToLowerInvariant().Insert(4, "-"));
        info.Success.Should().BeTrue("kod küçük harf ve tireyle de yazılabilir");
        info.Data.CourseName.Should().Be("Davet Kursu");
        info.Data.Role.Should().Be(UserRole.HeadTeacher);

        var phone = NewPhone();
        var joined = await JoinAsync(invitation.Code, "0" + phone, email: "Yusuf.Kaya@Ornek.com");
        joined.Success.Should().BeTrue(joined.Message);

        await using (var lookup = _db.CreateContext(Guid.Empty))
        {
            (await new EfJoinRequestDal(lookup).HasPendingAcrossTenantsAsync(phone)).Should().BeTrue("girişte 'onay bekliyor' denir");
            (await new EfUserDal(lookup).GetLoginCandidatesByPhoneAsync(phone)).Should().BeEmpty("onaylanmadan giriş yapamaz");
        }

        await using (var context = _db.CreateContext(tenantId))
        {
            var manager = Manager(context, new TestTenantContext(tenantId), adminId);
            var pending = (await manager.GetJoinRequestsAsync(null)).Data;
            var request = pending.Should().ContainSingle().Subject;
            request.PhoneNumber.Should().Be(phone);
            request.Email.Should().Be("yusuf.kaya@ornek.com");

            var approved = await manager.ApproveAsync(request.Id);
            approved.Success.Should().BeTrue(approved.Message);
            (await manager.ApproveAsync(request.Id)).Success.Should().BeFalse("iki kez onaylanamaz");
        }

        await using (var lookup = _db.CreateContext(Guid.Empty))
        {
            var user = (await new EfUserDal(lookup).GetLoginCandidatesByPhoneAsync(phone)).Should().ContainSingle().Subject;
            user.TenantId.Should().Be(tenantId);
            user.Role.Should().Be(UserRole.HeadTeacher);
            user.FirstName.Should().Be("Yusuf");
            (await new EfJoinRequestDal(lookup).HasPendingAcrossTenantsAsync(phone)).Should().BeFalse();
        }

        // Artık üye: aynı davetle tekrar katılamaz
        var again = await JoinAsync(invitation.Code, phone);
        again.Success.Should().BeFalse();
        again.Message.Should().Contain("zaten kayıtlı");
    }

    [PostgresFact]
    public async Task NewInvitation_ShouldRevokeThePreviousOneForThatRole()
    {
        var (tenantId, adminId) = await CreateCourseAsync();
        var first = await InviteAsync(tenantId, adminId);
        var assistant = await InviteAsync(tenantId, adminId, UserRole.AssistantTeacher);
        var second = await InviteAsync(tenantId, adminId);

        (await GetPublicAsync(first.Code)).ErrorType.Should().Be(ResultErrorType.NotFound);
        (await GetPublicAsync(second.Code)).Success.Should().BeTrue();
        (await GetPublicAsync(assistant.Code)).Success.Should().BeTrue("başka roldeki davet etkilenmez");

        await using var context = _db.CreateContext(tenantId);
        var active = (await Manager(context, new TestTenantContext(tenantId), adminId).GetActiveAsync()).Data;
        active.Select(i => i.Code).Should().BeEquivalentTo(new[] { second.Code, assistant.Code });
    }

    [PostgresFact]
    public async Task RevokedExpiredOrInactiveCourseInvitation_ShouldNotBeUsable()
    {
        var (tenantId, adminId) = await CreateCourseAsync();
        var revoked = await InviteAsync(tenantId, adminId);
        await using (var context = _db.CreateContext(tenantId))
            (await Manager(context, new TestTenantContext(tenantId), adminId).RevokeAsync(revoked.Id)).Success.Should().BeTrue();
        (await GetPublicAsync(revoked.Code)).Success.Should().BeFalse();
        (await JoinAsync(revoked.Code, NewPhone())).ErrorType.Should().Be(ResultErrorType.NotFound);

        var expired = await InviteAsync(tenantId, adminId, UserRole.AssistantTeacher);
        await _db.ExecuteSqlAsync($"UPDATE invitations SET expires_at = now() - interval '1 minute' WHERE id = '{expired.Id}';");
        (await GetPublicAsync(expired.Code)).Success.Should().BeFalse();

        var (pendingTenant, pendingAdmin) = await CreateCourseAsync(TenantStatus.Pending);
        var notApproved = await InviteAsync(pendingTenant, pendingAdmin);
        (await GetPublicAsync(notApproved.Code)).Success.Should().BeFalse("onaylanmamış kursa katılınamaz");

        (await GetPublicAsync("YOKBOYLE")).Success.Should().BeFalse();
        (await GetPublicAsync("kısa")).Success.Should().BeFalse();
    }

    [PostgresFact]
    public async Task SecondPendingRequestWithSamePhone_ShouldBeRejected_AndRejectedPersonCanTryAgain()
    {
        var (tenantId, adminId) = await CreateCourseAsync();
        var invitation = await InviteAsync(tenantId, adminId);
        var phone = NewPhone();

        (await JoinAsync(invitation.Code, phone)).Success.Should().BeTrue();
        var duplicate = await JoinAsync(invitation.Code, phone);
        duplicate.ErrorType.Should().Be(ResultErrorType.Conflict);
        duplicate.Message.Should().Contain("onay bekleyen");

        await using (var context = _db.CreateContext(tenantId))
        {
            var manager = Manager(context, new TestTenantContext(tenantId), adminId);
            var request = (await manager.GetJoinRequestsAsync(null)).Data.Single();
            (await manager.RejectAsync(request.Id, new RejectJoinRequestDto { Reason = "Tanımıyoruz" })).Success.Should().BeTrue();
            var rejected = (await manager.GetJoinRequestsAsync(JoinRequestStatus.Rejected)).Data.Single();
            rejected.RejectionReason.Should().Be("Tanımıyoruz");
        }

        (await JoinAsync(invitation.Code, phone)).Success.Should().BeTrue("reddedilen kişi yeniden istek gönderebilir");
    }

    [PostgresFact]
    public async Task HeadTeacher_ShouldOnlyManageAssistantTeacherInvitationsAndRequests()
    {
        var (tenantId, adminId) = await CreateCourseAsync();
        var headInvite = await InviteAsync(tenantId, adminId);
        (await JoinAsync(headInvite.Code, NewPhone())).Success.Should().BeTrue();

        await using var context = _db.CreateContext(tenantId);
        var headTeacher = Manager(context, new TestTenantContext(tenantId), Guid.NewGuid(), UserRole.HeadTeacher);

        (await headTeacher.CreateAsync(new CreateInvitationDto { Role = UserRole.HeadTeacher })).ErrorType
            .Should().Be(ResultErrorType.Forbidden);
        (await headTeacher.CreateAsync(new CreateInvitationDto { Role = UserRole.AssistantTeacher })).Success.Should().BeTrue();
        (await headTeacher.RevokeAsync(headInvite.Id)).ErrorType.Should().Be(ResultErrorType.Forbidden);

        (await headTeacher.GetActiveAsync()).Data.Should().ContainSingle(i => i.Role == UserRole.AssistantTeacher);
        (await headTeacher.GetJoinRequestsAsync(null)).Data.Should().BeEmpty("hoca isteğini yalnızca yönetici görür");
    }

    [PostgresFact]
    public async Task Approve_PreviouslyDeletedStaffWithSamePhone_ShouldReactivateTheSameUser()
    {
        var (tenantId, adminId) = await CreateCourseAsync();
        var phone = NewPhone();
        var oldUserId = Guid.NewGuid();
        await _db.ExecuteSqlAsync($"""
            INSERT INTO users (id, tenant_id, first_name, last_name, phone_number, role, is_deleted, deleted_at)
                VALUES ('{oldUserId}', '{tenantId}', 'Eski', 'Hoca', '{phone}', 'assistant_teacher', true, now());
            """);
        var invitation = await InviteAsync(tenantId, adminId);
        (await JoinAsync(invitation.Code, phone, firstName: "Yeni")).Success.Should().BeTrue("silinmiş kullanıcı üye sayılmaz");

        await using (var context = _db.CreateContext(tenantId))
        {
            var manager = Manager(context, new TestTenantContext(tenantId), adminId);
            var request = (await manager.GetJoinRequestsAsync(null)).Data.Single();
            var approved = await manager.ApproveAsync(request.Id);
            approved.Success.Should().BeTrue(approved.Message);
        }

        (await _db.ScalarAsync<string>($"SELECT first_name || '/' || role || '/' || is_deleted FROM users WHERE id = '{oldUserId}'"))
            .Should().Be("Yeni/head_teacher/false");
        (await _db.ScalarAsync<long>($"SELECT count(*) FROM users WHERE tenant_id = '{tenantId}' AND phone_number = '{phone}'"))
            .Should().Be(1);
    }

    [PostgresFact]
    public async Task Join_WithWrongCode_ShouldNotCreateRequest()
    {
        var (tenantId, adminId) = await CreateCourseAsync();
        var invitation = await InviteAsync(tenantId, adminId);
        var phone = NewPhone();

        var anonymous = new TestTenantContext(Guid.Empty);
        await using var context = _db.CreateContext(anonymous);
        var manager = Manager(context, anonymous);
        (await manager.SendJoinCodeAsync(invitation.Code, new SendOtpDto { PhoneNumber = phone })).Success.Should().BeTrue();
        var wrong = _sms.LastCode() == "000000" ? "111111" : "000000";
        var result = await manager.JoinAsync(invitation.Code, new JoinWithInvitationDto
        {
            FirstName = "Ali", LastName = "Veli", PhoneNumber = phone, Code = wrong
        });

        result.Success.Should().BeFalse();
        (await _db.ScalarAsync<long>($"SELECT count(*) FROM join_requests WHERE phone_number = '{phone}'")).Should().Be(0);
    }
}
