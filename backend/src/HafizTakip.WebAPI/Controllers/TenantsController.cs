using HafizTakip.Business.Abstract;
using HafizTakip.Entities.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using HafizTakip.WebAPI.Security;

namespace HafizTakip.WebAPI.Controllers;

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

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] HafizTakip.Core.Utilities.Requests.PaginationParams paginationParams)
    {
        var result = await _tenantService.GetAllAsync(paginationParams);
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

    // İlerleyen aşamalarda bu metodları HTTP POST olarak değiştirmeyi planlayın.
    [HttpPut]
    public async Task<IActionResult> Update(UpdateTenantDto updateTenantDto)
    {
        // TODO: Gelişmiş yetki (Authorization) ve validasyon kontrollerini gözden geçir.
        // WARNING: Soft-delete veya güncelleme işlemlerinde TenantId izolasyonundan emin ol.
        var result = await _tenantService.UpdateAsync(updateTenantDto);
        return HandleResult(result);
    }

    // İlerleyen aşamalarda bu metodları HTTP POST olarak değiştirmeyi planlayın.
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        // TODO: Gelişmiş yetki (Authorization) ve validasyon kontrollerini gözden geçir.
        // WARNING: Soft-delete veya güncelleme işlemlerinde TenantId izolasyonundan emin ol.
        var result = await _tenantService.DeleteAsync(id);
        return HandleResult(result);
    }
}

