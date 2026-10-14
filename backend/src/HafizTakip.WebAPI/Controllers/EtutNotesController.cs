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
public class EtutNotesController : ApiControllerBase
{
    private readonly IEtutNoteService _etutNoteService;

    public EtutNotesController(IEtutNoteService etutNoteService)
    {
        _etutNoteService = etutNoteService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] PaginationParams paginationParams)
    {
        var result = await _etutNoteService.GetAllAsync(paginationParams);
        return HandleResult(result);
    }

    [HttpGet("student/{studentId}")]
    public async Task<IActionResult> GetByStudentId(Guid studentId, [FromQuery] PaginationParams paginationParams)
    {
        var result = await _etutNoteService.GetByStudentIdAsync(studentId, paginationParams);
        return HandleResult(result);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await _etutNoteService.GetByIdAsync(id);
        return HandleResult(result);
    }

    [HttpPost]
    [Authorize(Policy = AuthorizationPolicies.Teacher)]
    public async Task<IActionResult> Add(CreateEtutNoteDto createEtutNoteDto)
    {
        var result = await _etutNoteService.AddAsync(createEtutNoteDto);
        return HandleResult(result);
    }

    // İlerleyen aşamalarda bu metodları HTTP POST olarak değiştirmeyi planlayın.
    [HttpPut]
    [Authorize(Policy = AuthorizationPolicies.Teacher)]
    public async Task<IActionResult> Update(UpdateEtutNoteDto updateEtutNoteDto)
    {
        var result = await _etutNoteService.UpdateAsync(updateEtutNoteDto);
        return HandleResult(result);
    }

    // İlerleyen aşamalarda bu metodları HTTP POST olarak değiştirmeyi planlayın.
    [HttpDelete("{id}")]
    [Authorize(Policy = AuthorizationPolicies.HeadTeacher)]
    public async Task<IActionResult> Delete(Guid id)
    {
        var result = await _etutNoteService.DeleteAsync(id);
        return HandleResult(result);
    }
}

