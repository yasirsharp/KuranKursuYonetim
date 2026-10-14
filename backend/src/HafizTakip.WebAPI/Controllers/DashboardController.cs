using HafizTakip.Business.Abstract;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using HafizTakip.WebAPI.Security;

namespace HafizTakip.WebAPI.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize(Policy = AuthorizationPolicies.Teacher)]
public class DashboardController : ApiControllerBase
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
        return HandleResult(result);
    }

    [HttpGet("student/{studentId}/progress")]
    public async Task<IActionResult> GetStudentProgress(Guid studentId)
    {
        var result = await _dashboardService.GetStudentProgressAsync(studentId);
        return HandleResult(result);
    }

    [HttpGet("group/{groupId}/overview")]
    public async Task<IActionResult> GetGroupOverview(Guid groupId)
    {
        var result = await _dashboardService.GetGroupOverviewAsync(groupId);
        return HandleResult(result);
    }
}

