using HafizTakip.Business.Abstract;
using HafizTakip.Core.Utilities.Requests;
using HafizTakip.Entities.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using HafizTakip.WebAPI.Security;
using HafizTakip.Core.Enums;

namespace HafizTakip.WebAPI.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize(Policy = AuthorizationPolicies.TenantMember)]
public class DailyAssignmentsController : ApiControllerBase
{
    private readonly IDailyAssignmentService _dailyAssignmentService;

    public DailyAssignmentsController(IDailyAssignmentService dailyAssignmentService)
    {
        _dailyAssignmentService = dailyAssignmentService;
    }

    [HttpGet]
    [Authorize(Policy = AuthorizationPolicies.HeadTeacher)]
    public async Task<IActionResult> GetAll([FromQuery] PaginationParams paginationParams)
    {
        var result = await _dailyAssignmentService.GetAllAsync(paginationParams);
        return HandleResult(result);
    }

    [HttpGet("student/{studentId}")]
    [Authorize(Policy = AuthorizationPolicies.ParentOrAbove)]
    public async Task<IActionResult> GetByStudentId(Guid studentId, [FromQuery] PaginationParams paginationParams)
    {
        var result = await _dailyAssignmentService.GetByStudentIdAsync(studentId, paginationParams);
        return HandleResult(result);
    }

    [HttpGet("etut")]
    [Authorize(Policy = AuthorizationPolicies.Teacher)]
    public async Task<IActionResult> GetEtutAssignments([FromQuery] DateTime date, [FromQuery] PaginationParams paginationParams)
    {
        var result = await _dailyAssignmentService.GetEtutAssignmentsAsync(date, paginationParams);
        return HandleResult(result);
    }

    [HttpGet("{id}")]
    [Authorize(Policy = AuthorizationPolicies.ParentOrAbove)]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await _dailyAssignmentService.GetByIdAsync(id);
        return HandleResult(result);
    }

    [HttpPost]
    [Authorize(Policy = AuthorizationPolicies.Teacher)]
    public async Task<IActionResult> Add(CreateDailyAssignmentDto createDailyAssignmentDto)
    {
        var result = await _dailyAssignmentService.AddAsync(createDailyAssignmentDto);
        return HandleResult(result);
    }

    [HttpPost("complete-etut")]
    [Authorize(Policy = AuthorizationPolicies.Teacher)]
    public async Task<IActionResult> CompleteEtut(CompleteEtutAssignmentDto dto)
    {
        var result = await _dailyAssignmentService.CompleteEtutAsync(dto);
        return HandleResult(result);
    }

    [HttpPost("complete-home")]
    [Authorize(Roles = UserRole.TenantAdmin + "," + UserRole.Parent)]
    public async Task<IActionResult> CompleteHome(CompleteHomeAssignmentDto dto)
    {
        var result = await _dailyAssignmentService.CompleteHomeAsync(dto);
        return HandleResult(result);
    }
}

