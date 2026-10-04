using Microsoft.Extensions.DependencyInjection;

namespace HafizTakip.Core.Utilities.IoC;

public interface ICoreModule
{
    void Load(IServiceCollection serviceCollection);
}

public static class ServiceTool
{
    public static IServiceProvider ServiceProvider { get; private set; } = null!;

    public static IServiceCollection Create(IServiceCollection services)
    {
        ServiceProvider = services.BuildServiceProvider();
        return services;
    }
}
