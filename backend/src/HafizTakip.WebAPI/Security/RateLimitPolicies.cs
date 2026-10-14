using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;

namespace HafizTakip.WebAPI.Security;

/// <summary>
/// IP bazlı istek sınırları. Numara bazlı OTP sınırları ayrıca AuthService içinde uygulanır.
/// </summary>
public static class RateLimitPolicies
{
    public const string Auth = nameof(Auth);

    public static IServiceCollection AddHafizTakipRateLimiting(this IServiceCollection services, IConfiguration configuration)
    {
        // Okul ağlarında çok sayıda kullanıcı aynı dış IP'yi paylaşabilir; sınır bu yüzden geniş tutulur.
        var authPermitPerMinute = configuration.GetValue("RateLimiting:AuthPermitPerMinute", 30);

        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.OnRejected = async (context, cancellationToken) =>
            {
                await context.HttpContext.Response.WriteAsJsonAsync(new
                {
                    success = false,
                    message = "Çok fazla istek gönderildi. Lütfen biraz sonra tekrar deneyin.",
                    statusCode = StatusCodes.Status429TooManyRequests
                }, cancellationToken);
            };

            // Giriş uçları: IP başına dakikada en fazla authPermitPerMinute istek
            options.AddPolicy(Auth, httpContext =>
                RateLimitPartition.GetFixedWindowLimiter(
                    httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                    _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = authPermitPerMinute,
                        Window = TimeSpan.FromMinutes(1),
                        QueueLimit = 0
                    }));
        });

        return services;
    }
}
