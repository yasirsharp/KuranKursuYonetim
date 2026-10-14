using HafizTakip.Business.Abstract;
using HafizTakip.Entities.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using HafizTakip.WebAPI.Security;

namespace HafizTakip.WebAPI.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize(Policy = AuthorizationPolicies.HeadTeacher)]
public class StudentParentsController : ApiControllerBase
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
        return HandleResult(result);
    }

    [HttpGet("parent/{parentId}")]
    public async Task<IActionResult> GetStudentsByParentId(Guid parentId)
    {
        var result = await _studentParentService.GetStudentsByParentIdAsync(parentId);
        return HandleResult(result);
    }

    [HttpPost]
    public async Task<IActionResult> Add(CreateStudentParentDto dto)
    {
        var result = await _studentParentService.AddAsync(dto);
        return HandleResult(result);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        // TODO: Gelişmiş yetki (Authorization) ve validasyon kontrollerini gözden geçir.
        // WARNING: Soft-delete veya güncelleme işlemlerinde TenantId izolasyonundan emin ol.
        var result = await _studentParentService.DeleteAsync(id);
        return HandleResult(result);
    }
}

