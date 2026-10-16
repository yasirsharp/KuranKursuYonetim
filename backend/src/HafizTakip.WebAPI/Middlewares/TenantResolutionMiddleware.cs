using HafizTakip.Core.Constants;
using HafizTakip.Core.Enums;
using HafizTakip.Core.Utilities.IoC;
using HafizTakip.DataAccess.Abstract;
using HafizTakip.WebAPI.Security;
using Microsoft.Extensions.Caching.Memory;

namespace HafizTakip.WebAPI.Middlewares;

/// <summary>
/// Oturum açmış kullanıcının JWT'sindeki tenant_id'yi çözer ve ITenantContext'e yazar.
/// Tenant yalnızca imzalı token'dan gelir; istemcinin gönderdiği header'lara güvenilmez.
/// Kursun durumu da burada uygulanır:
/// - askıya alınmış/iptal/silinmiş kurs → 403 (code: tenant_inactive)
/// - onay bekleyen/reddedilen kurs → yalnızca [AllowPendingTenant] uçları; diğerleri 403 (code: tenant_pending / tenant_rejected)
/// </summary>
public class TenantResolutionMiddleware
{
    private static readonly TimeSpan TenantStatusCacheDuration = TimeSpan.FromMinutes(1);

    private readonly RequestDelegate _next;

    public TenantResolutionMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, ITenantContext tenantContext, ITenantDal tenantDal, IMemoryCache cache)
    {
        if (context.User.Identity?.IsAuthenticated != true)
        {
            await _next(context);
            return;
        }

        // Super Admin platform uçlarını kullanır; tenant bağlamı yoktur
        if (context.User.IsInRole(UserRole.SuperAdmin))
        {
            await _next(context);
            return;
        }

        var tenantClaim = context.User.FindFirst("tenant_id")?.Value;
        if (!Guid.TryParse(tenantClaim, out var tenantId) || tenantId == Guid.Empty)
        {
            await WriteForbiddenAsync(context, "tenant_missing", "Oturum bilgisinde kurum bilgisi bulunamadı. Lütfen tekrar giriş yapın.");
            return;
        }

        var state = await cache.GetOrCreateAsync(TenantCacheKeys.Status(tenantId), async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TenantStatusCacheDuration;
            var tenant = await tenantDal.GetAsync(t => t.Id == tenantId);
            return tenant == null ? null : new TenantAccessState(tenant.Status, tenant.IsActive);
        });

        if (state == null || !TenantStatus.CanSignIn(state.Status, state.IsActive))
        {
            await WriteForbiddenAsync(context, "tenant_inactive", "Kurum hesabı askıya alınmıştır veya bulunamamıştır. Lütfen sistem yöneticisi ile iletişime geçin.");
            return;
        }

        if (!TenantStatus.HasFullAccess(state.Status, state.IsActive)
            && context.GetEndpoint()?.Metadata.GetMetadata<AllowPendingTenantAttribute>() == null)
        {
            if (state.Status == TenantStatus.Rejected)
                await WriteForbiddenAsync(context, "tenant_rejected", "Kurs başvurunuz onaylanmadı. Bilgilerinizi düzenleyip yeniden gönderebilirsiniz.");
            else
                await WriteForbiddenAsync(context, "tenant_pending", "Kursunuz onay bekliyor. Platform yöneticisi onayladığında tüm özellikler açılacaktır.");
            return;
        }

        tenantContext.SetTenantId(tenantId);
        await _next(context);
    }

    private static Task WriteForbiddenAsync(HttpContext context, string code, string message)
    {
        context.Response.StatusCode = StatusCodes.Status403Forbidden;
        return context.Response.WriteAsJsonAsync(new { success = false, message, code, statusCode = StatusCodes.Status403Forbidden });
    }

    private sealed record TenantAccessState(string Status, bool IsActive);
}
