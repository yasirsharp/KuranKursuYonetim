using System.Text.RegularExpressions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using HafizTakip.Core.Utilities.IoC;

namespace HafizTakip.Core.CrossCuttingConcerns.Caching.Microsoft;

public class MemoryCacheManager : ICacheManager
{
    private readonly IMemoryCache _memoryCache;

    public MemoryCacheManager()
    {
        _memoryCache = ServiceTool.ServiceProvider.GetService<IMemoryCache>() 
                       ?? throw new InvalidOperationException("IMemoryCache servisi bulunamadı.");
    }

    public void Add(string key, object data, int duration)
    {
        _memoryCache.Set(key, data, TimeSpan.FromMinutes(duration));
    }

    public T? Get<T>(string key)
    {
        return _memoryCache.Get<T>(key);
    }

    public object? Get(string key)
    {
        return _memoryCache.Get(key);
    }

    public bool IsAdd(string key)
    {
        return _memoryCache.TryGetValue(key, out _);
    }

    public void Remove(string key)
    {
        _memoryCache.Remove(key);
    }

    public void RemoveByPattern(string pattern)
    {
        // Not: .NET 8 IMemoryCache'te entries dictionary private olduğu için reflection gerekir.
        // Şimdilik OTP gibi basit cache'ler kullanacağımızdan reflection'ı es geçiyoruz.
        // Gerekirse Redis entegrasyonuna geçildiğinde pattern kullanılabilir.
    }
}
