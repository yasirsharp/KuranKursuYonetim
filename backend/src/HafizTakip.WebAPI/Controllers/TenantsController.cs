using HafizTakip.Business.Abstract;
using HafizTakip.Core.Utilities.Requests;
using HafizTakip.Entities.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using HafizTakip.WebAPI.Security;

namespace HafizTakip.WebAPI.Controllers;

/// <summary>Platform yöneticisinin kurs yönetimi ve kurs başvurusu onayı.</summary>
[Route("api/[controller]")]
[ApiController]
[Authorize(Policy = AuthorizationPolicies.SuperAdmin)]
public class TenantsController : ApiControllerBase
{
    private readonly ITenantService _tenantService;

    public TenantsController(ITenantService tenantService)
    {
        _tenantService = tenantService;
    }

    /// <summary>Kurs listesi. status=pending → onay bekleyen başvurular (en eskisi önce); search → ad/il.</summary>
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] TenantFilterDto filter, [FromQuery] PaginationParams paginationParams)
    {
        var result = await _tenantService.GetAllAsync(filter, paginationParams);
        return HandleResult(result);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await _tenantService.GetByIdAsync(id);
        return HandleResult(result);
    }

    [HttpPost]
    public async Task<IActionResult> Add(CreateTenantDto createTenantDto)
    {
        var result = await _tenantService.AddAsync(createTenantDto);
        return HandleResult(result);
    }

    [HttpPut]
    public async Task<IActionResult> Update(UpdateTenantDto updateTenantDto)
    {
        var result = await _tenantService.UpdateAsync(updateTenantDto);
        return HandleResult(result);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var result = await _tenantService.DeleteAsync(id);
        return HandleResult(result);
    }

    [HttpPost("{id}/approve")]
    public async Task<IActionResult> Approve(Guid id)
    {
        return HandleResult(await _tenantService.ApproveAsync(id));
    }

    [HttpPost("{id}/reject")]
    public async Task<IActionResult> Reject(Guid id, [FromBody] RejectCourseApplicationDto dto)
    {
        return HandleResult(await _tenantService.RejectAsync(id, dto));
    }
}
