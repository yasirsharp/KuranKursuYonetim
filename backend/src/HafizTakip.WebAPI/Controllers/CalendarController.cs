using HafizTakip.Business.Abstract;
using HafizTakip.Entities.Dtos;
using HafizTakip.WebAPI.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HafizTakip.WebAPI.Controllers;

/// <summary>
/// Eğitim dönemleri, kurs takvimi (tatil/etkinlik) ve ders günleri.
/// Okuma hocalara, değişiklik kurs müdürüne açıktır. Tarihler "yyyy-MM-dd" biçimindedir.
/// </summary>
[Route("api/[controller]")]
[ApiController]
[Authorize(Policy = AuthorizationPolicies.Teacher)]
public class CalendarController : ApiControllerBase
{
    private readonly ICalendarService _calendarService;

    public CalendarController(ICalendarService calendarService)
    {
        _calendarService = calendarService;
    }

    // ── Dönemler ──

    [HttpGet("terms")]
    public async Task<IActionResult> GetTerms()
    {
        return HandleResult(await _calendarService.GetTermsAsync());
    }

    [HttpGet("terms/planning")]
    public async Task<IActionResult> GetTermPlanning()
    {
        return HandleResult(await _calendarService.GetTermPlanningAsync());
    }

    [HttpPost("terms")]
    [Authorize(Policy = AuthorizationPolicies.TenantAdmin)]
    public async Task<IActionResult> AddTerm(CreateAcademicTermDto term)
    {
        return HandleResult(await _calendarService.AddTermAsync(term));
    }

    [HttpPut("terms")]
    [Authorize(Policy = AuthorizationPolicies.TenantAdmin)]
    public async Task<IActionResult> UpdateTerm(UpdateAcademicTermDto term)
    {
        return HandleResult(await _calendarService.UpdateTermAsync(term));
    }

    [HttpDelete("terms/{id}")]
    [Authorize(Policy = AuthorizationPolicies.TenantAdmin)]
    public async Task<IActionResult> DeleteTerm(Guid id)
    {
        return HandleResult(await _calendarService.DeleteTermAsync(id));
    }

    // ── Takvim kayıtları ──

    [HttpGet("events")]
    public async Task<IActionResult> GetEvents([FromQuery] DateOnly from, [FromQuery] DateOnly to)
    {
        return HandleResult(await _calendarService.GetEventsAsync(from, to));
    }

    [HttpPost("events")]
    [Authorize(Policy = AuthorizationPolicies.TenantAdmin)]
    public async Task<IActionResult> AddEvent(CreateCalendarEventDto calendarEvent)
    {
        return HandleResult(await _calendarService.AddEventAsync(calendarEvent));
    }

    [HttpPut("events")]
    [Authorize(Policy = AuthorizationPolicies.TenantAdmin)]
    public async Task<IActionResult> UpdateEvent(UpdateCalendarEventDto calendarEvent)
    {
        return HandleResult(await _calendarService.UpdateEventAsync(calendarEvent));
    }

    [HttpDelete("events/{id}")]
    [Authorize(Policy = AuthorizationPolicies.TenantAdmin)]
    public async Task<IActionResult> DeleteEvent(Guid id)
    {
        return HandleResult(await _calendarService.DeleteEventAsync(id));
    }

    // ── Tatiller ──

    /// <summary>Aralıktaki resmi tatiller ve dini bayramlar (dönem tarihleri girilince gösterilir).</summary>
    [HttpGet("holidays")]
    public async Task<IActionResult> GetHolidaySuggestions([FromQuery] DateOnly from, [FromQuery] DateOnly to)
    {
        return HandleResult(await _calendarService.GetHolidaySuggestionsAsync(from, to));
    }

    [HttpPost("holidays/import")]
    [Authorize(Policy = AuthorizationPolicies.TenantAdmin)]
    public async Task<IActionResult> ImportHolidays(ImportHolidaysDto import)
    {
        return HandleResult(await _calendarService.ImportHolidaysAsync(import));
    }

    // ── Ders günleri ──

    /// <summary>Ders günleri; aralık verilmezse devam eden dönem.</summary>
    [HttpGet("school-days")]
    public async Task<IActionResult> GetSchoolDays([FromQuery] DateOnly? from, [FromQuery] DateOnly? to)
    {
        return HandleResult(await _calendarService.GetSchoolDaysAsync(from, to));
    }
}
