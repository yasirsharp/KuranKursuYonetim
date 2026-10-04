using HafizTakip.Business.Abstract;
using HafizTakip.Entities.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HafizTakip.WebAPI.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize(Roles = "tenant_admin,head_teacher")]
public class StudentParentsController : ControllerBase
{
    private readonly IStudentParentService _studentParentService;

    public StudentParentsController(IStudentParentService studentParentService)
    {
        _studentParentService = studentParentService;
    }

    [HttpGet("student/{studentId}")]
    public async Task<IActionResult> GetParentsByStudentId(Guid studentId)
    {
        var result = await _studentParentService.GetParentsByStudentIdAsync(studentId);
        if (result.Success) return Ok(result);
        return BadRequest(result);
    }

    [HttpGet("parent/{parentId}")]
    public async Task<IActionResult> GetStudentsByParentId(Guid parentId)
    {
        var result = await _studentParentService.GetStudentsByParentIdAsync(parentId);
        if (result.Success) return Ok(result);
        return BadRequest(result);
    }

    [HttpPost]
    public async Task<IActionResult> Add(CreateStudentParentDto dto)
    {
        var result = await _studentParentService.AddAsync(dto);
        if (result.Success) return Ok(result);
        return BadRequest(result);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        // TODO: Geli�mi� yetki (Authorization) ve validasyon kontrollerini g�zden ge�ir.
        // WARNING: Soft-delete veya g�ncelleme i�lemlerinde TenantId izolasyonundan emin ol.
        var result = await _studentParentService.DeleteAsync(id);
        if (result.Success) return Ok(result);
        return BadRequest(result);
    }
}

