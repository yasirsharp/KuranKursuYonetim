using HafizTakip.Business.Abstract;
using HafizTakip.Entities.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HafizTakip.WebAPI.Controllers;

[Route("api/[controller]")]
[ApiController]
// TODO: Okuma uçlarına endpoint rolü ve gerektiğinde grup sahipliği denetimi ekle.
[Authorize]
public class GroupsController : ControllerBase
{
    private readonly IGroupService _groupService;

    public GroupsController(IGroupService groupService)
    {
        _groupService = groupService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] HafizTakip.Core.Utilities.Requests.PaginationParams paginationParams)
    {
        var result = await _groupService.GetAllAsync(paginationParams);
        if (result.Success) return Ok(result);
        return BadRequest(result);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await _groupService.GetByIdAsync(id);
        if (result.Success) return Ok(result);
        return BadRequest(result);
    }

    [HttpPost]
    [Authorize(Roles = "tenant_admin,head_teacher")]
    public async Task<IActionResult> Add(CreateGroupDto createGroupDto)
    {
        var result = await _groupService.AddAsync(createGroupDto);
        if (result.Success) return Ok(result);
        return BadRequest(result);
    }

    // TODO: Production ortamında DELETE ve PUT metodları desteklenmeyebilir.
    // İlerleyen aşamalarda bu metodları HTTP POST olarak değiştirmeyi planlayın.
    [HttpPut]
    [Authorize(Roles = "tenant_admin,head_teacher")]
    public async Task<IActionResult> Update(UpdateGroupDto updateGroupDto)
    {
        var result = await _groupService.UpdateAsync(updateGroupDto);
        if (result.Success) return Ok(result);
        return BadRequest(result);
    }

    // TODO: Production ortamında DELETE ve PUT metodları desteklenmeyebilir.
    // İlerleyen aşamalarda bu metodları HTTP POST olarak değiştirmeyi planlayın.
    [HttpDelete("{id}")]
    [Authorize(Roles = "tenant_admin,head_teacher")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var result = await _groupService.DeleteAsync(id);
        if (result.Success) return Ok(result);
        return BadRequest(result);
    }
}

