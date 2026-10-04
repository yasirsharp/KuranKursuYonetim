using HafizTakip.Business.Abstract;
using HafizTakip.Core.Utilities.Requests;
using HafizTakip.Entities.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HafizTakip.WebAPI.Controllers;

[Route("api/[controller]")]
[ApiController]
// TODO: Sınav/sonuç okuma uçlarını rol ve veli-öğrenci sahipliği bazında sınırla.
[Authorize]
public class ExamsController : ControllerBase
{
    private readonly IExamService _examService;
    private readonly IExamResultService _examResultService;

    public ExamsController(IExamService examService, IExamResultService examResultService)
    {
        _examService = examService;
        _examResultService = examResultService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] PaginationParams paginationParams)
    {
        var result = await _examService.GetAllAsync(paginationParams);
        if (result.Success) return Ok(result);
        return BadRequest(result);
    }

    [HttpGet("group/{groupId}")]
    public async Task<IActionResult> GetByGroupId(Guid groupId, [FromQuery] PaginationParams paginationParams)
    {
        var result = await _examService.GetByGroupIdAsync(groupId, paginationParams);
        if (result.Success) return Ok(result);
        return BadRequest(result);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await _examService.GetByIdAsync(id);
        if (result.Success) return Ok(result);
        return BadRequest(result);
    }

    [HttpPost]
    [Authorize(Roles = "tenant_admin,head_teacher")]
    public async Task<IActionResult> Add(CreateExamDto createExamDto)
    {
        var result = await _examService.AddAsync(createExamDto);
        if (result.Success) return Ok(result);
        return BadRequest(result);
    }

    // TODO: Production ortamında DELETE ve PUT metodları desteklenmeyebilir.
    // İlerleyen aşamalarda bu metodları HTTP POST olarak değiştirmeyi planlayın.
    [HttpPut]
    [Authorize(Roles = "tenant_admin,head_teacher")]
    public async Task<IActionResult> Update(UpdateExamDto updateExamDto)
    {
        var result = await _examService.UpdateAsync(updateExamDto);
        if (result.Success) return Ok(result);
        return BadRequest(result);
    }

    // TODO: Production ortamında DELETE ve PUT metodları desteklenmeyebilir.
    // İlerleyen aşamalarda bu metodları HTTP POST olarak değiştirmeyi planlayın.
    [HttpDelete("{id}")]
    [Authorize(Roles = "tenant_admin")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var result = await _examService.DeleteAsync(id);
        if (result.Success) return Ok(result);
        return BadRequest(result);
    }

    // --- Sınav Sonuçları (ExamResults) Metotları ---

    [HttpGet("{examId}/results")]
    public async Task<IActionResult> GetResultsByExamId(Guid examId, [FromQuery] PaginationParams paginationParams)
    {
        var result = await _examResultService.GetByExamIdAsync(examId, paginationParams);
        if (result.Success) return Ok(result);
        return BadRequest(result);
    }

    [HttpGet("results/student/{studentId}")]
    public async Task<IActionResult> GetResultsByStudentId(Guid studentId, [FromQuery] PaginationParams paginationParams)
    {
        var result = await _examResultService.GetByStudentIdAsync(studentId, paginationParams);
        if (result.Success) return Ok(result);
        return BadRequest(result);
    }

    [HttpPost("results")]
    [Authorize(Roles = "tenant_admin,head_teacher,assistant_teacher")]
    public async Task<IActionResult> AddResult(CreateExamResultDto createExamResultDto)
    {
        var result = await _examResultService.AddAsync(createExamResultDto);
        if (result.Success) return Ok(result);
        return BadRequest(result);
    }

    // TODO: Production ortamında DELETE ve PUT metodları desteklenmeyebilir.
    [HttpPut("results")]
    [Authorize(Roles = "tenant_admin,head_teacher,assistant_teacher")]
    public async Task<IActionResult> UpdateResult(UpdateExamResultDto updateExamResultDto)
    {
        var result = await _examResultService.UpdateAsync(updateExamResultDto);
        if (result.Success) return Ok(result);
        return BadRequest(result);
    }

    // TODO: Production ortamında DELETE ve PUT metodları desteklenmeyebilir.
    [HttpDelete("results/{id}")]
    [Authorize(Roles = "tenant_admin,head_teacher")]
    public async Task<IActionResult> DeleteResult(Guid id)
    {
        var result = await _examResultService.DeleteAsync(id);
        if (result.Success) return Ok(result);
        return BadRequest(result);
    }
}

