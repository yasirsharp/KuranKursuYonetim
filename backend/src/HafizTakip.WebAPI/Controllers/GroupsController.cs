using HafizTakip.Business.Abstract;
using HafizTakip.Entities.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using HafizTakip.WebAPI.Security;

namespace HafizTakip.WebAPI.Controllers;

[Route("api/[controller]")]
[ApiController]
// TODO: Okuma uçlarına endpoint rolü ve gerektiğinde grup sahipliği denetimi ekle.
[Authorize(Policy = AuthorizationPolicies.TenantMember)]
public class GroupsController : ApiControllerBase
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
        return HandleResult(result);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await _groupService.GetByIdAsync(id);
        return HandleResult(result);
    }

    [HttpPost]
    [Authorize(Policy = AuthorizationPolicies.HeadTeacher)]
    public async Task<IActionResult> Add(CreateGroupDto createGroupDto)
    {
        var result = await _groupService.AddAsync(createGroupDto);
        return HandleResult(result);
    }

    // TODO: Production ortamında DELETE ve PUT metodları desteklenmeyebilir.
    // İlerleyen aşamalarda bu metodları HTTP POST olarak değiştirmeyi planlayın.
    [HttpPut]
    [Authorize(Policy = AuthorizationPolicies.HeadTeacher)]
    public async Task<IActionResult> Update(UpdateGroupDto updateGroupDto)
    {
        var result = await _groupService.UpdateAsync(updateGroupDto);
        return HandleResult(result);
    }

    // TODO: Production ortamında DELETE ve PUT metodları desteklenmeyebilir.
    // İlerleyen aşamalarda bu metodları HTTP POST olarak değiştirmeyi planlayın.
    [HttpDelete("{id}")]
    [Authorize(Policy = AuthorizationPolicies.HeadTeacher)]
    public async Task<IActionResult> Delete(Guid id)
    {
        var result = await _groupService.DeleteAsync(id);
        return HandleResult(result);
    }
}

