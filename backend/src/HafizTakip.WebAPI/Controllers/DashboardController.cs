using HafizTakip.Business.Abstract;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HafizTakip.WebAPI.Controllers;

[Route("api/[controller]")]
[ApiController]
// TODO: Özet ve öğrenci ilerleme uçlarını rol/kayıt sahipliği bazında sınırla.
[Authorize]
public class DashboardController : ControllerBase
{
    private readonly IDashboardService _dashboardService;

    public DashboardController(IDashboardService dashboardService)
    {
        _dashboardService = dashboardService;
    }

    [HttpGet("summary")]
    public async Task<IActionResult> GetSummary()
    {
        var result = await _dashboardService.GetSummaryAsync();
        if (result.Success) return Ok(result);
        return BadRequest(result);
    }

    [HttpGet("student/{studentId}/progress")]
    public async Task<IActionResult> GetStudentProgress(Guid studentId)
    {
        var result = await _dashboardService.GetStudentProgressAsync(studentId);
        if (result.Success) return Ok(result);
        return BadRequest(result);
    }

    [HttpGet("group/{groupId}/overview")]
    public async Task<IActionResult> GetGroupOverview(Guid groupId)
    {
        var result = await _dashboardService.GetGroupOverviewAsync(groupId);
        if (result.Success) return Ok(result);
        return BadRequest(result);
    }
}

