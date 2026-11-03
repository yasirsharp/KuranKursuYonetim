using HafizTakip.Business.Abstract;
using HafizTakip.Entities.Dtos;
using HafizTakip.WebAPI.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HafizTakip.WebAPI.Controllers;

/// <summary>
/// Personel davetleri ve katılım istekleri (kurs yöneticisi ve başöğretmen).
/// Başöğretmen yalnızca yardımcı hoca davetlerini/isteklerini görür ve yönetir.
/// </summary>
[Route("api/invitations")]
[ApiController]
[Authorize(Policy = AuthorizationPolicies.HeadTeacher)]
public class InvitationsController : ApiControllerBase
{
    private readonly IInvitationService _service;

    public InvitationsController(IInvitationService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<IActionResult> GetActive() => HandleResult(await _service.GetActiveAsync());

    /// <summary>Yeni davet; aynı roldeki önceki geçerli davet iptal edilir.</summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateInvitationDto dto) => HandleResult(await _service.CreateAsync(dto));

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Revoke(Guid id) => HandleResult(await _service.RevokeAsync(id));

    /// <summary>status: pending (varsayılan) | approved | rejected</summary>
    [HttpGet("join-requests")]
    public async Task<IActionResult> GetJoinRequests([FromQuery] string? status) =>
        HandleResult(await _service.GetJoinRequestsAsync(status));

    [HttpPost("join-requests/{id:guid}/approve")]
    public async Task<IActionResult> Approve(Guid id) => HandleResult(await _service.ApproveAsync(id));

    [HttpPost("join-requests/{id:guid}/reject")]
    public async Task<IActionResult> Reject(Guid id, [FromBody] RejectJoinRequestDto dto) =>
        HandleResult(await _service.RejectAsync(id, dto));
}
