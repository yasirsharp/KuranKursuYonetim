using HafizTakip.Business.Abstract;
using HafizTakip.Core.Utilities.Requests;
using HafizTakip.Entities.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HafizTakip.WebAPI.Controllers;

[Route("api/[controller]")]
[ApiController]
// TODO: Okuma uçlarına rol ve veli-öğrenci sahipliği denetimi ekle.
[Authorize]
public class AttendanceController : ControllerBase
{
    private readonly IAttendanceService _attendanceService;

    public AttendanceController(IAttendanceService attendanceService)
    {
        _attendanceService = attendanceService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] PaginationParams paginationParams)
    {
        var result = await _attendanceService.GetAllAsync(paginationParams);
        if (result.Success) return Ok(result);
        return BadRequest(result);
    }

    [HttpGet("student/{studentId}")]
    public async Task<IActionResult> GetByStudentId(Guid studentId, [FromQuery] PaginationParams paginationParams)
    {
        var result = await _attendanceService.GetByStudentIdAsync(studentId, paginationParams);
        if (result.Success) return Ok(result);
        return BadRequest(result);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await _attendanceService.GetByIdAsync(id);
        if (result.Success) return Ok(result);
        return BadRequest(result);
    }

    [HttpPost]
    [Authorize(Roles = "tenant_admin,head_teacher,assistant_teacher")]
    public async Task<IActionResult> Add(CreateAttendanceDto createAttendanceDto)
    {
        var result = await _attendanceService.AddAsync(createAttendanceDto);
        if (result.Success) return Ok(result);
        return BadRequest(result);
    }

    [HttpPost("bulk")]
    [Authorize(Roles = "tenant_admin,head_teacher,assistant_teacher")]
    public async Task<IActionResult> AddBulk(BulkAttendanceDto bulkAttendanceDto)
    {
        var result = await _attendanceService.AddBulkAsync(bulkAttendanceDto);
        if (result.Success) return Ok(result);
        return BadRequest(result);
    }

    // TODO: Production ortamında DELETE ve PUT metodları desteklenmeyebilir.
    // İlerleyen aşamalarda bu metodları HTTP POST olarak değiştirmeyi planlayın.
    [HttpPut]
    [Authorize(Roles = "tenant_admin,head_teacher,assistant_teacher")]
    public async Task<IActionResult> Update(AttendanceDto updateAttendanceDto)
    {
        var result = await _attendanceService.UpdateAsync(updateAttendanceDto);
        if (result.Success) return Ok(result);
        return BadRequest(result);
    }

    // TODO: Production ortamında DELETE ve PUT metodları desteklenmeyebilir.
    // İlerleyen aşamalarda bu metodları HTTP POST olarak değiştirmeyi planlayın.
    [HttpDelete("{id}")]
    [Authorize(Roles = "tenant_admin,head_teacher")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var result = await _attendanceService.DeleteAsync(id);
        if (result.Success) return Ok(result);
        return BadRequest(result);
    }
}

