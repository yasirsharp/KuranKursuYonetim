using HafizTakip.Business.Abstract;
using HafizTakip.Core.Utilities.Requests;
using HafizTakip.Entities.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HafizTakip.WebAPI.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize(Roles = "super_admin")]
public class InvoicesController : ControllerBase
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
        if (result.Success) return Ok(result);
        return BadRequest(result);
    }

    [HttpGet("tenant/{tenantId}")]
    public async Task<IActionResult> GetByTenantId(Guid tenantId, [FromQuery] PaginationParams paginationParams)
    {
        var result = await _invoiceService.GetByTenantIdAsync(tenantId, paginationParams);
        if (result.Success) return Ok(result);
        return BadRequest(result);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await _invoiceService.GetByIdAsync(id);
        if (result.Success) return Ok(result);
        return BadRequest(result);
    }

    [HttpPost]
    public async Task<IActionResult> Add(CreateInvoiceDto createInvoiceDto)
    {
        var result = await _invoiceService.AddAsync(createInvoiceDto);
        if (result.Success) return Ok(result);
        return BadRequest(result);
    }

    [HttpPut("status")]
    public async Task<IActionResult> UpdateStatus(UpdateInvoiceStatusDto updateInvoiceStatusDto)
    {
        // TODO: Geli�mi� yetki (Authorization) ve validasyon kontrollerini g�zden ge�ir.
        // WARNING: Soft-delete veya g�ncelleme i�lemlerinde TenantId izolasyonundan emin ol.
        var result = await _invoiceService.UpdateStatusAsync(updateInvoiceStatusDto);
        if (result.Success) return Ok(result);
        return BadRequest(result);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        // TODO: Geli�mi� yetki (Authorization) ve validasyon kontrollerini g�zden ge�ir.
        // WARNING: Soft-delete veya g�ncelleme i�lemlerinde TenantId izolasyonundan emin ol.
        var result = await _invoiceService.DeleteAsync(id);
        if (result.Success) return Ok(result);
        return BadRequest(result);
    }
}

