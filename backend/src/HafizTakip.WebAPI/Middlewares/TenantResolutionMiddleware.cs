using HafizTakip.Core.Enums;
using HafizTakip.Core.Utilities.IoC;
using System.Security.Claims;
using HafizTakip.DataAccess.Abstract;
using Microsoft.Extensions.Caching.Memory;

namespace HafizTakip.WebAPI.Middlewares;

/// <summary>
/// Oturum açmış kullanıcının JWT'sindeki tenant_id'yi çözer ve ITenantContext'e yazar.
/// Tenant yalnızca imzalı token'dan gelir; istemcinin gönderdiği header'lara güvenilmez.
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
            await WriteForbiddenAsync(context, "Oturum bilgisinde kurum bilgisi bulunamadı. Lütfen tekrar giriş yapın.");
            return;
        }

        var isActive = await cache.GetOrCreateAsync($"tenant_active_{tenantId}", async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TenantStatusCacheDuration;
            var tenant = await tenantDal.GetAsync(t => t.Id == tenantId);
            return tenant != null && tenant.IsActive;
        });

        if (!isActive)
        {
            await WriteForbiddenAsync(context, "Kurum hesabı askıya alınmıştır veya bulunamamıştır. Lütfen sistem yöneticisi ile iletişime geçin.");
            return;
        }

        tenantContext.SetTenantId(tenantId);
        await _next(context);
    }

    private static Task WriteForbiddenAsync(HttpContext context, string message)
    {
        context.Response.StatusCode = StatusCodes.Status403Forbidden;
        return context.Response.WriteAsJsonAsync(new { success = false, message, statusCode = StatusCodes.Status403Forbidden });
    }
}
