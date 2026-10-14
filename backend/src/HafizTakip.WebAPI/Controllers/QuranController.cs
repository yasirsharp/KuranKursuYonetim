using HafizTakip.Business.Abstract;
using HafizTakip.WebAPI.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HafizTakip.WebAPI.Controllers;

/// <summary>Mushaf referansı (sure listesi, sayfa ve cüz bilgisi). Veri değişmez; yanıtlar istemcide önbelleklenebilir.</summary>
[Route("api/[controller]")]
[ApiController]
[Authorize(Policy = AuthorizationPolicies.TenantMember)]
public class QuranController : ApiControllerBase
{
    private const int ClientCacheSeconds = 60 * 60 * 24;

    private readonly IQuranReferenceService _quranService;

    public QuranController(IQuranReferenceService quranService)
    {
        _quranService = quranService;
    }

    [HttpGet("surahs")]
    [ResponseCache(Duration = ClientCacheSeconds, Location = ResponseCacheLocation.Client)]
    public async Task<IActionResult> GetSurahs()
    {
        return HandleResult(await _quranService.GetSurahsAsync());
    }

    [HttpGet("pages/{page:int}")]
    [ResponseCache(Duration = ClientCacheSeconds, Location = ResponseCacheLocation.Client)]
    public async Task<IActionResult> GetPage(int page)
    {
        return HandleResult(await _quranService.GetPageAsync(page));
    }

    [HttpGet("juz/{juz:int}")]
    [ResponseCache(Duration = ClientCacheSeconds, Location = ResponseCacheLocation.Client)]
    public async Task<IActionResult> GetJuz(int juz)
    {
        return HandleResult(await _quranService.GetJuzAsync(juz));
    }
}
