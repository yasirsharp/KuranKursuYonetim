using HafizTakip.Core.Utilities.IoC;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;
using HafizTakip.DataAccess.Abstract;

namespace HafizTakip.WebAPI.Middlewares;

public class TenantResolutionMiddleware
{
    private readonly RequestDelegate _next;

    public TenantResolutionMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, ITenantContext tenantContext, ITenantDal tenantDal)
    {
        if (context.User.Identity?.IsAuthenticated == true)
        {
            var roleClaim = context.User.FindFirst(ClaimTypes.Role)?.Value;
            
            // Super Admin bypass
            if (roleClaim == "super_admin")
            {
                await _next(context);
                return;
            }

            var tenantClaim = context.User.FindFirst("tenant_id");
            if (tenantClaim != null && Guid.TryParse(tenantClaim.Value, out Guid tenantId))
            {
                // IsActive kontrolü
                var tenant = await tenantDal.GetAsync(t => t.Id == tenantId);
                if (tenant == null || !tenant.IsActive)
                {
                    context.Response.StatusCode = StatusCodes.Status403Forbidden;
                    await context.Response.WriteAsJsonAsync(new { Message = "Kurum hesabı askıya alınmıştır veya bulunamamıştır. Lütfen sistem yöneticisi ile iletişime geçin." });
                    return;
                }

                tenantContext.SetTenantId(tenantId);
            }
        }
        else
        {
            if (context.Request.Headers.TryGetValue("x-tenant-id", out var tenantHeader))
            {
                if (Guid.TryParse(tenantHeader.ToString(), out Guid tenantId))
                {
                    var tenant = await tenantDal.GetAsync(t => t.Id == tenantId);
                    if (tenant != null && tenant.IsActive)
                    {
                        tenantContext.SetTenantId(tenantId);
                    }
                }
            }
        }

        await _next(context);
    }
}
