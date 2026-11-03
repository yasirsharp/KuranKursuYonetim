using HafizTakip.Business.Abstract;
using HafizTakip.Entities.Dtos;
using HafizTakip.WebAPI.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace HafizTakip.WebAPI.Controllers;

/// <summary>
/// Davet bağlantısıyla katılım (herkese açık): GET {code} (kurs ve rol) → send-code (SMS) → POST (katılım isteği).
/// İstek kurs yöneticisinin onayını bekler; onaylanınca kişi telefon numarasıyla giriş yapar.
/// </summary>
[Route("api/join")]
[ApiController]
[AllowAnonymous]
[EnableRateLimiting(RateLimitPolicies.Auth)]
public class JoinController : ApiControllerBase
{
    private readonly IInvitationService _service;

    public JoinController(IInvitationService service)
    {
        _service = service;
    }

    [HttpGet("{code}")]
    public async Task<IActionResult> Get(string code) => HandleResult(await _service.GetPublicAsync(code));

    [HttpPost("{code}/send-code")]
    public async Task<IActionResult> SendCode(string code, [FromBody] SendOtpDto dto) =>
        HandleResult(await _service.SendJoinCodeAsync(code, dto));

    [HttpPost("{code}")]
    public async Task<IActionResult> Join(string code, [FromBody] JoinWithInvitationDto dto) =>
        HandleResult(await _service.JoinAsync(code, dto));
}
