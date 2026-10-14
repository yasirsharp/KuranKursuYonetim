using HafizTakip.Business.Abstract;
using HafizTakip.Core.Utilities.Requests;
using HafizTakip.Entities.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using HafizTakip.WebAPI.Security;

namespace HafizTakip.WebAPI.Controllers;

[Route("api/[controller]")]
[ApiController]
// TODO: Sınav/sonuç okuma uçlarını rol ve veli-öğrenci sahipliği bazında sınırla.
[Authorize(Policy = AuthorizationPolicies.TenantMember)]
public class ExamsController : ApiControllerBase
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
        return HandleResult(result);
    }

    [HttpGet("group/{groupId}")]
    public async Task<IActionResult> GetByGroupId(Guid groupId, [FromQuery] PaginationParams paginationParams)
    {
        var result = await _examService.GetByGroupIdAsync(groupId, paginationParams);
        return HandleResult(result);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await _examService.GetByIdAsync(id);
        return HandleResult(result);
    }

    [HttpPost]
    [Authorize(Policy = AuthorizationPolicies.HeadTeacher)]
    public async Task<IActionResult> Add(CreateExamDto createExamDto)
    {
        var result = await _examService.AddAsync(createExamDto);
        return HandleResult(result);
    }

    // TODO: Production ortamında DELETE ve PUT metodları desteklenmeyebilir.
    // İlerleyen aşamalarda bu metodları HTTP POST olarak değiştirmeyi planlayın.
    [HttpPut]
    [Authorize(Policy = AuthorizationPolicies.HeadTeacher)]
    public async Task<IActionResult> Update(UpdateExamDto updateExamDto)
    {
        var result = await _examService.UpdateAsync(updateExamDto);
        return HandleResult(result);
    }

    // TODO: Production ortamında DELETE ve PUT metodları desteklenmeyebilir.
    // İlerleyen aşamalarda bu metodları HTTP POST olarak değiştirmeyi planlayın.
    [HttpDelete("{id}")]
    [Authorize(Policy = AuthorizationPolicies.TenantAdmin)]
    public async Task<IActionResult> Delete(Guid id)
    {
        var result = await _examService.DeleteAsync(id);
        return HandleResult(result);
    }

    // --- Sınav Sonuçları (ExamResults) Metotları ---

    [HttpGet("{examId}/results")]
    public async Task<IActionResult> GetResultsByExamId(Guid examId, [FromQuery] PaginationParams paginationParams)
    {
        var result = await _examResultService.GetByExamIdAsync(examId, paginationParams);
        return HandleResult(result);
    }

    [HttpGet("results/student/{studentId}")]
    public async Task<IActionResult> GetResultsByStudentId(Guid studentId, [FromQuery] PaginationParams paginationParams)
    {
        var result = await _examResultService.GetByStudentIdAsync(studentId, paginationParams);
        return HandleResult(result);
    }

    [HttpPost("results")]
    [Authorize(Policy = AuthorizationPolicies.Teacher)]
    public async Task<IActionResult> AddResult(CreateExamResultDto createExamResultDto)
    {
        var result = await _examResultService.AddAsync(createExamResultDto);
        return HandleResult(result);
    }

    // TODO: Production ortamında DELETE ve PUT metodları desteklenmeyebilir.
    [HttpPut("results")]
    [Authorize(Policy = AuthorizationPolicies.Teacher)]
    public async Task<IActionResult> UpdateResult(UpdateExamResultDto updateExamResultDto)
    {
        var result = await _examResultService.UpdateAsync(updateExamResultDto);
        return HandleResult(result);
    }

    // TODO: Production ortamında DELETE ve PUT metodları desteklenmeyebilir.
    [HttpDelete("results/{id}")]
    [Authorize(Policy = AuthorizationPolicies.HeadTeacher)]
    public async Task<IActionResult> DeleteResult(Guid id)
    {
        var result = await _examResultService.DeleteAsync(id);
        return HandleResult(result);
    }
}

