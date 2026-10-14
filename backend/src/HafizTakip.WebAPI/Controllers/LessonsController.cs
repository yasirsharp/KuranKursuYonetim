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
public class LessonsController : ApiControllerBase
{
    private readonly ILessonService _lessonService;

    public LessonsController(ILessonService lessonService)
    {
        _lessonService = lessonService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] LessonFilter filter, [FromQuery] PaginationParams paginationParams)
    {
        var result = await _lessonService.GetAllAsync(filter, paginationParams);
        return HandleResult(result);
    }

    [HttpGet("student/{studentId}")]
    public async Task<IActionResult> GetByStudentId(Guid studentId, [FromQuery] PaginationParams paginationParams)
    {
        var result = await _lessonService.GetByStudentIdAsync(studentId, paginationParams);
        return HandleResult(result);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await _lessonService.GetByIdAsync(id);
        return HandleResult(result);
    }

    [HttpPost]
    [Authorize(Policy = AuthorizationPolicies.Teacher)]
    public async Task<IActionResult> Add(CreateLessonDto createLessonDto)
    {
        var result = await _lessonService.AddAsync(createLessonDto);
        return HandleResult(result);
    }

    // İlerleyen aşamalarda bu metodları HTTP POST olarak değiştirmeyi planlayın.
    [HttpPut]
    [Authorize(Policy = AuthorizationPolicies.Teacher)]
    public async Task<IActionResult> Update(UpdateLessonDto updateLessonDto)
    {
        var result = await _lessonService.UpdateAsync(updateLessonDto);
        return HandleResult(result);
    }

    // İlerleyen aşamalarda bu metodları HTTP POST olarak değiştirmeyi planlayın.
    [HttpDelete("{id}")]
    [Authorize(Policy = AuthorizationPolicies.HeadTeacher)]
    public async Task<IActionResult> Delete(Guid id)
    {
        var result = await _lessonService.DeleteAsync(id);
        return HandleResult(result);
    }
}

