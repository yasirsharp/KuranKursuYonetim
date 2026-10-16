using HafizTakip.Business.Abstract;
using HafizTakip.Entities.Dtos;
using HafizTakip.WebAPI.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HafizTakip.WebAPI.Controllers;

/// <summary>Oturumdaki kursun ayarları ve kurs bilgileri. Okuma hocalara, değişiklik kurs müdürüne açıktır.</summary>
[Route("api/[controller]")]
[ApiController]
[Authorize(Policy = AuthorizationPolicies.Teacher)]
public class SettingsController : ApiControllerBase
{
    private readonly ITenantSettingsService _settingsService;

    public SettingsController(ITenantSettingsService settingsService)
    {
        _settingsService = settingsService;
    }

    [HttpGet]
    public async Task<IActionResult> Get()
    {
        return HandleResult(await _settingsService.GetAsync());
    }

    [HttpPut]
    [Authorize(Policy = AuthorizationPolicies.TenantAdmin)]
    public async Task<IActionResult> Update(TenantSettingsDto settings)
    {
        return HandleResult(await _settingsService.UpdateAsync(settings));
    }

    [HttpGet("course")]
    public async Task<IActionResult> GetCourseInfo()
    {
        return HandleResult(await _settingsService.GetCourseInfoAsync());
    }

    [HttpPut("course")]
    [Authorize(Policy = AuthorizationPolicies.TenantAdmin)]
    public async Task<IActionResult> UpdateCourseInfo(UpdateCourseInfoDto courseInfo)
    {
        return HandleResult(await _settingsService.UpdateCourseInfoAsync(courseInfo));
    }
}
