using HafizTakip.Business.Abstract;
using HafizTakip.Core.Utilities.Requests;
using HafizTakip.Entities.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using HafizTakip.WebAPI.Security;

namespace HafizTakip.WebAPI.Controllers;

[Route("api/[controller]")]
[ApiController]
// TODO: Duyuruların hedef kitlesiyle rollerin okuma yetkisini eşleştir.
[Authorize(Policy = AuthorizationPolicies.TenantMember)]
public class AnnouncementsController : ApiControllerBase
{
    private readonly IAnnouncementService _announcementService;

    public AnnouncementsController(IAnnouncementService announcementService)
    {
        _announcementService = announcementService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] PaginationParams paginationParams)
    {
        var result = await _announcementService.GetAllAsync(paginationParams);
        return HandleResult(result);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await _announcementService.GetByIdAsync(id);
        return HandleResult(result);
    }

    [HttpPost]
    [Authorize(Policy = AuthorizationPolicies.HeadTeacher)]
    public async Task<IActionResult> Add(CreateAnnouncementDto createAnnouncementDto)
    {
        // Yazar kimliği normalde token'dan alınabilir. Şimdilik body'den alınıyor.
        var result = await _announcementService.AddAsync(createAnnouncementDto);
        return HandleResult(result);
    }

    // TODO: Production ortamında DELETE ve PUT metodları desteklenmeyebilir.
    // İlerleyen aşamalarda bu metodları HTTP POST olarak değiştirmeyi planlayın.
    [HttpPut]
    [Authorize(Policy = AuthorizationPolicies.HeadTeacher)]
    public async Task<IActionResult> Update(UpdateAnnouncementDto updateAnnouncementDto)
    {
        var result = await _announcementService.UpdateAsync(updateAnnouncementDto);
        return HandleResult(result);
    }

    // TODO: Production ortamında DELETE ve PUT metodları desteklenmeyebilir.
    // İlerleyen aşamalarda bu metodları HTTP POST olarak değiştirmeyi planlayın.
    [HttpDelete("{id}")]
    [Authorize(Policy = AuthorizationPolicies.HeadTeacher)]
    public async Task<IActionResult> Delete(Guid id)
    {
        var result = await _announcementService.DeleteAsync(id);
        return HandleResult(result);
    }
}

