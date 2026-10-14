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
public class InvoicesController : ApiControllerBase
{
    private readonly IInvoiceService _invoiceService;

    public InvoicesController(IInvoiceService invoiceService)
    {
        _invoiceService = invoiceService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] PaginationParams paginationParams)
    {
        var result = await _invoiceService.GetAllAsync(paginationParams);
        return HandleResult(result);
    }

    [HttpGet("tenant/{tenantId}")]
    public async Task<IActionResult> GetByTenantId(Guid tenantId, [FromQuery] PaginationParams paginationParams)
    {
        var result = await _invoiceService.GetByTenantIdAsync(tenantId, paginationParams);
        return HandleResult(result);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await _invoiceService.GetByIdAsync(id);
        return HandleResult(result);
    }

    [HttpPost]
    public async Task<IActionResult> Add(CreateInvoiceDto createInvoiceDto)
    {
        var result = await _invoiceService.AddAsync(createInvoiceDto);
        return HandleResult(result);
    }

    [HttpPut("status")]
    public async Task<IActionResult> UpdateStatus(UpdateInvoiceStatusDto updateInvoiceStatusDto)
    {
        // TODO: Geli�mi� yetki (Authorization) ve validasyon kontrollerini g�zden ge�ir.
        // WARNING: Soft-delete veya g�ncelleme i�lemlerinde TenantId izolasyonundan emin ol.
        var result = await _invoiceService.UpdateStatusAsync(updateInvoiceStatusDto);
        return HandleResult(result);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        // TODO: Geli�mi� yetki (Authorization) ve validasyon kontrollerini g�zden ge�ir.
        // WARNING: Soft-delete veya g�ncelleme i�lemlerinde TenantId izolasyonundan emin ol.
        var result = await _invoiceService.DeleteAsync(id);
        return HandleResult(result);
    }
}

