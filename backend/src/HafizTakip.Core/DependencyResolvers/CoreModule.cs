using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Caching.Memory;
using HafizTakip.Core.CrossCuttingConcerns.Caching;
using HafizTakip.Core.CrossCuttingConcerns.Caching.Microsoft;
using HafizTakip.Core.Utilities.IoC;

namespace HafizTakip.Core.DependencyResolvers;

public class CoreModule : ICoreModule
{
    public void Load(IServiceCollection serviceCollection)
    {
        serviceCollection.AddMemoryCache();
        serviceCollection.AddSingleton<ICacheManager, MemoryCacheManager>();
        
        // TenantContext scoped olmalı ki her request'te kendi tenant_id'sini tutsun
        serviceCollection.AddScoped<ITenantContext, TenantContext>();

        // İsteği yapan kullanıcı (JWT claim'leri) — HttpContext üzerinden okunur
        serviceCollection.AddHttpContextAccessor();
        serviceCollection.AddScoped<ICurrentUser, CurrentUser>();
    }
}
