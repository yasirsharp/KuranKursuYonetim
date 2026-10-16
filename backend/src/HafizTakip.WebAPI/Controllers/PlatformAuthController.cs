using HafizTakip.Business.Abstract;
using HafizTakip.Entities.Dtos;
using HafizTakip.WebAPI.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace HafizTakip.WebAPI.Controllers;

/// <summary>Platform yöneticisi girişi (e-posta + şifre). Kurs kullanıcıları /api/auth (telefon + OTP) kullanır.</summary>
[AllowAnonymous]
[Route("api/platform/auth")]
[ApiController]
[EnableRateLimiting(RateLimitPolicies.Auth)]
public class PlatformAuthController : ApiControllerBase
{
    private readonly IPlatformAuthService _platformAuthService;

    public PlatformAuthController(IPlatformAuthService platformAuthService)
    {
        _platformAuthService = platformAuthService;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] PlatformLoginDto dto)
    {
        return HandleResult(await _platformAuthService.LoginAsync(dto));
    }
}
