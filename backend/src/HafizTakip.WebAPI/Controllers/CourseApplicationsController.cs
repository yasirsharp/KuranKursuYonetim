using HafizTakip.Business.Abstract;
using HafizTakip.Entities.Dtos;
using HafizTakip.WebAPI.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace HafizTakip.WebAPI.Controllers;

/// <summary>
/// "Kurs oluştur" başvurusu (herkese açık) ve başvuranın başvuru durumu ekranı.
/// Akış: send-code → POST (kurs "pending" açılır, oturum döner) → GET me ("kursunuz onay bekliyor").
/// </summary>
[Route("api/course-applications")]
[ApiController]
public class CourseApplicationsController : ApiControllerBase
{
    private readonly ICourseApplicationService _service;

    public CourseApplicationsController(ICourseApplicationService service)
    {
        _service = service;
    }

    [HttpPost("send-code")]
    [AllowAnonymous]
    [EnableRateLimiting(RateLimitPolicies.Auth)]
    public async Task<IActionResult> SendCode([FromBody] SendOtpDto dto)
    {
        return HandleResult(await _service.SendCodeAsync(dto));
    }

    [HttpPost]
    [AllowAnonymous]
    [EnableRateLimiting(RateLimitPolicies.Auth)]
    public async Task<IActionResult> Submit([FromBody] CourseApplicationDto dto)
    {
        return HandleResult(await _service.SubmitAsync(dto));
    }

    [HttpGet("me")]
    [Authorize(Policy = AuthorizationPolicies.TenantAdmin)]
    [AllowPendingTenant]
    public async Task<IActionResult> GetMine()
    {
        return HandleResult(await _service.GetMineAsync());
    }

    [HttpPut("me")]
    [Authorize(Policy = AuthorizationPolicies.TenantAdmin)]
    [AllowPendingTenant]
    public async Task<IActionResult> Resubmit([FromBody] CourseApplicationInfoDto dto)
    {
        return HandleResult(await _service.ResubmitAsync(dto));
    }
}
