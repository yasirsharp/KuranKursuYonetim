using HafizTakip.Business.Abstract;
using HafizTakip.Core.Utilities.Requests;
using HafizTakip.Entities.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HafizTakip.WebAPI.Controllers;

[Route("api/[controller]")]
[ApiController]
// TODO: Duyuruların hedef kitlesiyle rollerin okuma yetkisini eşleştir.
[Authorize]
public class AnnouncementsController : ControllerBase
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
        if (result.Success) return Ok(result);
        return BadRequest(result);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await _announcementService.GetByIdAsync(id);
        if (result.Success) return Ok(result);
        return BadRequest(result);
    }

    [HttpPost]
    [Authorize(Roles = "tenant_admin,head_teacher")]
    public async Task<IActionResult> Add(CreateAnnouncementDto createAnnouncementDto)
    {
        // Yazar kimliği normalde token'dan alınabilir. Şimdilik body'den alınıyor.
        var result = await _announcementService.AddAsync(createAnnouncementDto);
        if (result.Success) return Ok(result);
        return BadRequest(result);
    }

    // TODO: Production ortamında DELETE ve PUT metodları desteklenmeyebilir.
    // İlerleyen aşamalarda bu metodları HTTP POST olarak değiştirmeyi planlayın.
    [HttpPut]
    [Authorize(Roles = "tenant_admin,head_teacher")]
    public async Task<IActionResult> Update(UpdateAnnouncementDto updateAnnouncementDto)
    {
        var result = await _announcementService.UpdateAsync(updateAnnouncementDto);
        if (result.Success) return Ok(result);
        return BadRequest(result);
    }

    // TODO: Production ortamında DELETE ve PUT metodları desteklenmeyebilir.
    // İlerleyen aşamalarda bu metodları HTTP POST olarak değiştirmeyi planlayın.
    [HttpDelete("{id}")]
    [Authorize(Roles = "tenant_admin,head_teacher")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var result = await _announcementService.DeleteAsync(id);
        if (result.Success) return Ok(result);
        return BadRequest(result);
    }
}

