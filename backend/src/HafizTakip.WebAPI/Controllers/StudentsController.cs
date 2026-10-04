using HafizTakip.Business.Abstract;
using HafizTakip.Entities.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HafizTakip.WebAPI.Controllers;

[Route("api/[controller]")]
[ApiController]
// TODO: GET uçları tüm giriş yapmış rollere açık; rol bazlı erişim ve veli için yalnızca bağlı öğrencileri döndürme kuralı ekle.
[Authorize]
public class StudentsController : ControllerBase
{
    private readonly IStudentService _studentService;

    public StudentsController(IStudentService studentService)
    {
        _studentService = studentService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] HafizTakip.Core.Utilities.Requests.PaginationParams paginationParams)
    {
        var result = await _studentService.GetAllAsync(paginationParams);
        if (result.Success) return Ok(result);
        return BadRequest(result);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await _studentService.GetByIdAsync(id);
        if (result.Success) return Ok(result);
        return BadRequest(result);
    }

    [HttpPost]
    [Authorize(Roles = "tenant_admin,head_teacher,assistant_teacher")]
    public async Task<IActionResult> Add(CreateStudentDto createStudentDto)
    {
        var result = await _studentService.AddAsync(createStudentDto);
        if (result.Success) return Ok(result);
        return BadRequest(result);
    }

    // TODO: Production ortamında DELETE ve PUT metodları desteklenmeyebilir.
    // İlerleyen aşamalarda bu metodları HTTP POST olarak değiştirmeyi planlayın.
    [HttpPut]
    [Authorize(Roles = "tenant_admin,head_teacher,assistant_teacher")]
    public async Task<IActionResult> Update(UpdateStudentDto updateStudentDto)
    {
        var result = await _studentService.UpdateAsync(updateStudentDto);
        if (result.Success) return Ok(result);
        return BadRequest(result);
    }

    // TODO: Production ortamında DELETE ve PUT metodları desteklenmeyebilir.
    // İlerleyen aşamalarda bu metodları HTTP POST olarak değiştirmeyi planlayın.
    [HttpDelete("{id}")]
    [Authorize(Roles = "tenant_admin,head_teacher")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var result = await _studentService.DeleteAsync(id);
        if (result.Success) return Ok(result);
        return BadRequest(result);
    }
}

