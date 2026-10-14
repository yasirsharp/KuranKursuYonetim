using HafizTakip.Business.Abstract;
using HafizTakip.Core.Utilities.Requests;
using HafizTakip.Entities.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using HafizTakip.WebAPI.Security;

namespace HafizTakip.WebAPI.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize(Policy = AuthorizationPolicies.SuperAdmin)]
public class SubscriptionsController : ApiControllerBase
{
    private readonly ISubscriptionService _subscriptionService;

    public SubscriptionsController(ISubscriptionService subscriptionService)
    {
        _subscriptionService = subscriptionService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] PaginationParams paginationParams)
    {
        var result = await _subscriptionService.GetAllAsync(paginationParams);
        return HandleResult(result);
    }

    [HttpGet("tenant/{tenantId}")]
    public async Task<IActionResult> GetByTenantId(Guid tenantId)
    {
        var result = await _subscriptionService.GetByTenantIdAsync(tenantId);
        return HandleResult(result);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await _subscriptionService.GetByIdAsync(id);
        return HandleResult(result);
    }

    [HttpPost]
    public async Task<IActionResult> Add(CreateSubscriptionDto createSubscriptionDto)
    {
        var result = await _subscriptionService.AddAsync(createSubscriptionDto);
        return HandleResult(result);
    }

    [HttpPut]
    public async Task<IActionResult> Update(UpdateSubscriptionDto updateSubscriptionDto)
    {
        // TODO: Geli�mi� yetki (Authorization) ve validasyon kontrollerini g�zden ge�ir.
        // WARNING: Soft-delete veya g�ncelleme i�lemlerinde TenantId izolasyonundan emin ol.
        var result = await _subscriptionService.UpdateAsync(updateSubscriptionDto);
        return HandleResult(result);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        // TODO: Geli�mi� yetki (Authorization) ve validasyon kontrollerini g�zden ge�ir.
        // WARNING: Soft-delete veya g�ncelleme i�lemlerinde TenantId izolasyonundan emin ol.
        var result = await _subscriptionService.DeleteAsync(id);
        return HandleResult(result);
    }
}

