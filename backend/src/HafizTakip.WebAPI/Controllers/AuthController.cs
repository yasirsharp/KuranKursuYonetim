using HafizTakip.Business.Abstract;
using HafizTakip.Entities.Dtos;
using HafizTakip.WebAPI.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace HafizTakip.WebAPI.Controllers;

[AllowAnonymous]
[Route("api/[controller]")]
[ApiController]
[EnableRateLimiting(RateLimitPolicies.Auth)]
[AllowPendingTenant] // refresh/logout onay bekleyen kursta da çalışmalı
public class AuthController : ApiControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    [HttpPost("send-otp")]
    public async Task<IActionResult> SendOtp([FromBody] SendOtpDto sendOtpDto)
    {
        return HandleResult(await _authService.SendOtpAsync(sendOtpDto));
    }

    [HttpPost("verify-otp")]
    public async Task<IActionResult> VerifyOtp([FromBody] VerifyOtpDto verifyOtpDto)
    {
        return HandleResult(await _authService.VerifyOtpAsync(verifyOtpDto));
    }

    [HttpPost("send-email-code")]
    public async Task<IActionResult> SendEmailCode([FromBody] SendEmailCodeDto sendEmailCodeDto)
    {
        return HandleResult(await _authService.SendEmailCodeAsync(sendEmailCodeDto));
    }

    [HttpPost("verify-email-code")]
    public async Task<IActionResult> VerifyEmailCode([FromBody] VerifyEmailCodeDto verifyEmailCodeDto)
    {
        return HandleResult(await _authService.VerifyEmailCodeAsync(verifyEmailCodeDto));
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh([FromBody] RefreshTokenRequestDto request)
    {
        return HandleResult(await _authService.RefreshAsync(request));
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout([FromBody] RefreshTokenRequestDto request)
    {
        return HandleResult(await _authService.LogoutAsync(request));
    }
}
