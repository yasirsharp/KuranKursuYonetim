using HafizTakip.Business.Abstract;
using HafizTakip.Core.Utilities.Requests;
using HafizTakip.Entities.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HafizTakip.WebAPI.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class DailyAssignmentsController : ControllerBase
{
    private readonly IDailyAssignmentService _dailyAssignmentService;

    public DailyAssignmentsController(IDailyAssignmentService dailyAssignmentService)
    {
        _dailyAssignmentService = dailyAssignmentService;
    }

    [HttpGet]
    [Authorize(Roles = "tenant_admin,head_teacher")]
    public async Task<IActionResult> GetAll([FromQuery] PaginationParams paginationParams)
    {
        var result = await _dailyAssignmentService.GetAllAsync(paginationParams);
        if (result.Success) return Ok(result);
        return BadRequest(result);
    }

    [HttpGet("student/{studentId}")]
    [Authorize(Roles = "tenant_admin,head_teacher,assistant_teacher,parent")]
    public async Task<IActionResult> GetByStudentId(Guid studentId, [FromQuery] PaginationParams paginationParams)
    {
        var result = await _dailyAssignmentService.GetByStudentIdAsync(studentId, paginationParams);
        if (result.Success) return Ok(result);
        return BadRequest(result);
    }

    [HttpGet("etut")]
    [Authorize(Roles = "tenant_admin,head_teacher,assistant_teacher")]
    public async Task<IActionResult> GetEtutAssignments([FromQuery] DateTime date, [FromQuery] PaginationParams paginationParams)
    {
        var result = await _dailyAssignmentService.GetEtutAssignmentsAsync(date, paginationParams);
        if (result.Success) return Ok(result);
        return BadRequest(result);
    }

    [HttpGet("{id}")]
    [Authorize(Roles = "tenant_admin,head_teacher,assistant_teacher,parent")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await _dailyAssignmentService.GetByIdAsync(id);
        if (result.Success) return Ok(result);
        return BadRequest(result);
    }

    [HttpPost]
    [Authorize(Roles = "tenant_admin,head_teacher,assistant_teacher")]
    public async Task<IActionResult> Add(CreateDailyAssignmentDto createDailyAssignmentDto)
    {
        var result = await _dailyAssignmentService.AddAsync(createDailyAssignmentDto);
        if (result.Success) return Ok(result);
        return BadRequest(result);
    }

    [HttpPost("complete-etut")]
    [Authorize(Roles = "tenant_admin,head_teacher,assistant_teacher")]
    public async Task<IActionResult> CompleteEtut(CompleteEtutAssignmentDto dto)
    {
        var result = await _dailyAssignmentService.CompleteEtutAsync(dto);
        if (result.Success) return Ok(result);
        return BadRequest(result);
    }

    [HttpPost("complete-home")]
    [Authorize(Roles = "tenant_admin,parent")]
    public async Task<IActionResult> CompleteHome(CompleteHomeAssignmentDto dto)
    {
        var result = await _dailyAssignmentService.CompleteHomeAsync(dto);
        if (result.Success) return Ok(result);
        return BadRequest(result);
    }
}

