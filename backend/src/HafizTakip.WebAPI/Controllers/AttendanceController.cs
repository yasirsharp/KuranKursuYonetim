using HafizTakip.Business.Abstract;
using HafizTakip.Core.Utilities.Requests;
using HafizTakip.Entities.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using HafizTakip.WebAPI.Security;

namespace HafizTakip.WebAPI.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize(Policy = AuthorizationPolicies.TenantMember)]
public class AttendanceController : ApiControllerBase
{
    private readonly IAttendanceService _attendanceService;

    public AttendanceController(IAttendanceService attendanceService)
    {
        _attendanceService = attendanceService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] AttendanceFilter filter, [FromQuery] PaginationParams paginationParams)
    {
        var result = await _attendanceService.GetAllAsync(filter, paginationParams);
        return HandleResult(result);
    }

    [HttpGet("student/{studentId}")]
    public async Task<IActionResult> GetByStudentId(Guid studentId, [FromQuery] PaginationParams paginationParams)
    {
        var result = await _attendanceService.GetByStudentIdAsync(studentId, paginationParams);
        return HandleResult(result);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await _attendanceService.GetByIdAsync(id);
        return HandleResult(result);
    }

    [HttpPost]
    [Authorize(Policy = AuthorizationPolicies.Teacher)]
    public async Task<IActionResult> Add(CreateAttendanceDto createAttendanceDto)
    {
        var result = await _attendanceService.AddAsync(createAttendanceDto);
        return HandleResult(result);
    }

    [HttpPost("bulk")]
    [Authorize(Policy = AuthorizationPolicies.Teacher)]
    public async Task<IActionResult> AddBulk(BulkAttendanceDto bulkAttendanceDto)
    {
        var result = await _attendanceService.AddBulkAsync(bulkAttendanceDto);
        return HandleResult(result);
    }

    // İlerleyen aşamalarda bu metodları HTTP POST olarak değiştirmeyi planlayın.
    [HttpPut]
    [Authorize(Policy = AuthorizationPolicies.Teacher)]
    public async Task<IActionResult> Update(AttendanceDto updateAttendanceDto)
    {
        var result = await _attendanceService.UpdateAsync(updateAttendanceDto);
        return HandleResult(result);
    }

    // İlerleyen aşamalarda bu metodları HTTP POST olarak değiştirmeyi planlayın.
    [HttpDelete("{id}")]
    [Authorize(Policy = AuthorizationPolicies.HeadTeacher)]
    public async Task<IActionResult> Delete(Guid id)
    {
        var result = await _attendanceService.DeleteAsync(id);
        return HandleResult(result);
    }
}

