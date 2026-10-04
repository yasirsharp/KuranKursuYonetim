using HafizTakip.Business.Abstract;
using HafizTakip.Core.Utilities.IoC;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace HafizTakip.WebAPI.Controllers;

[AllowAnonymous]
[Route("api/[controller]")]
[ApiController]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    [HttpPost("send-otp")]
    public async Task<IActionResult> SendOtp([FromBody] HafizTakip.Entities.Dtos.SendOtpDto sendOtpDto)
    {
        await _authService.SendOtpAsync(sendOtpDto);
        return Ok(new { success = true, message = "OTP kodu gönderildi." });
    }

    [HttpPost("verify-otp")]
    public async Task<IActionResult> VerifyOtp([FromBody] HafizTakip.Entities.Dtos.VerifyOtpDto verifyOtpDto)
    {
        var result = await _authService.VerifyOtpAsync(verifyOtpDto);
        return Ok(new { success = true, message = "Giriş başarılı.", data = result });
    }
}
