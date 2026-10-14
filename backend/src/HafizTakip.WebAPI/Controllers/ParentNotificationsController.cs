using HafizTakip.Business.Abstract;
using HafizTakip.Core.Utilities.Requests;
using HafizTakip.Entities.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using HafizTakip.WebAPI.Security;

namespace HafizTakip.WebAPI.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize(Policy = AuthorizationPolicies.TenantMember)]
public class ParentNotificationsController : ApiControllerBase
{
    private readonly IParentNotificationService _parentNotificationService;

    public ParentNotificationsController(IParentNotificationService parentNotificationService)
    {
        _parentNotificationService = parentNotificationService;
    }

    [HttpGet]
    [Authorize(Policy = AuthorizationPolicies.TenantAdmin)]
    public async Task<IActionResult> GetAll([FromQuery] PaginationParams paginationParams)
    {
        var result = await _parentNotificationService.GetAllAsync(paginationParams);
        return HandleResult(result);
    }

    [HttpGet("student/{studentId}")]
    [Authorize(Policy = AuthorizationPolicies.Teacher)]
    public async Task<IActionResult> GetByStudentId(Guid studentId, [FromQuery] PaginationParams paginationParams)
    {
        var result = await _parentNotificationService.GetByStudentIdAsync(studentId, paginationParams);
        return HandleResult(result);
    }

    [HttpGet("{id}")]
    [Authorize(Policy = AuthorizationPolicies.HeadTeacher)]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await _parentNotificationService.GetByIdAsync(id);
        return HandleResult(result);
    }

    [HttpPost]
    [Authorize(Policy = AuthorizationPolicies.HeadTeacher)]
    public async Task<IActionResult> Add(CreateParentNotificationDto createParentNotificationDto)
    {
        var result = await _parentNotificationService.AddAsync(createParentNotificationDto);
        return HandleResult(result);
    }

    [HttpPost("{id}/send")]
    [Authorize(Policy = AuthorizationPolicies.HeadTeacher)]
    public async Task<IActionResult> SendNotification(Guid id)
    {
        var result = await _parentNotificationService.SendNotificationAsync(id);
        return HandleResult(result);
    }
}

