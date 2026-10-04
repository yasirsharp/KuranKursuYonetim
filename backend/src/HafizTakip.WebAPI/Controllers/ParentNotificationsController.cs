using HafizTakip.Business.Abstract;
using HafizTakip.Core.Utilities.Requests;
using HafizTakip.Entities.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HafizTakip.WebAPI.Controllers;

[Route("api/[controller]")]
[ApiController]
// TODO: Bildirim okuma yetkilerini bağlı veli-öğrenci ilişkisiyle sınırla.
[Authorize]
public class ParentNotificationsController : ControllerBase
{
    private readonly IParentNotificationService _parentNotificationService;

    public ParentNotificationsController(IParentNotificationService parentNotificationService)
    {
        _parentNotificationService = parentNotificationService;
    }

    [HttpGet]
    [Authorize(Roles = "tenant_admin")]
    public async Task<IActionResult> GetAll([FromQuery] PaginationParams paginationParams)
    {
        var result = await _parentNotificationService.GetAllAsync(paginationParams);
        if (result.Success) return Ok(result);
        return BadRequest(result);
    }

    [HttpGet("student/{studentId}")]
    [Authorize(Roles = "tenant_admin,head_teacher,assistant_teacher")]
    public async Task<IActionResult> GetByStudentId(Guid studentId, [FromQuery] PaginationParams paginationParams)
    {
        var result = await _parentNotificationService.GetByStudentIdAsync(studentId, paginationParams);
        if (result.Success) return Ok(result);
        return BadRequest(result);
    }

    [HttpGet("{id}")]
    [Authorize(Roles = "tenant_admin,head_teacher")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await _parentNotificationService.GetByIdAsync(id);
        if (result.Success) return Ok(result);
        return BadRequest(result);
    }

    [HttpPost]
    [Authorize(Roles = "tenant_admin,head_teacher")]
    public async Task<IActionResult> Add(CreateParentNotificationDto createParentNotificationDto)
    {
        var result = await _parentNotificationService.AddAsync(createParentNotificationDto);
        if (result.Success) return Ok(result);
        return BadRequest(result);
    }

    [HttpPost("{id}/send")]
    [Authorize(Roles = "tenant_admin,head_teacher")]
    public async Task<IActionResult> SendNotification(Guid id)
    {
        var result = await _parentNotificationService.SendNotificationAsync(id);
        if (result.Success) return Ok(result);
        return BadRequest(result);
    }
}

