using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sahand.ClinicPro.API.Filters;
using Sahand.ClinicPro.Application.DTOs.Visits;
using Sahand.ClinicPro.Application.Features.Visits.Commands.SaveVisit;
using Sahand.ClinicPro.Application.Features.Visits.Queries;

namespace Sahand.ClinicPro.API.Controllers;

/// <summary>
/// کنترلر ویزیت — پرونده بالینی با الگوی SOAP
/// مسیرها: /api/v1/visits
/// </summary>
[ApiController]
[Route("api/v1/visits")]
[Authorize]
public class VisitsController : ApiBaseController
{
    private readonly IMediator _mediator;

    /// <summary>سازنده با تزریق میانجی</summary>
    public VisitsController(IMediator mediator) => _mediator = mediator;

    /// <summary>تاریخچه ویزیت‌های یک بیمار</summary>
    /// <param name="patientId">شناسه بیمار</param>
    [HttpGet("patient/{patientId:long}")]
    [HasPermission("visits.view")]
    public async Task<IActionResult> GetByPatient(long patientId)
    {
        var result = await _mediator.Send(new GetPatientVisitsQuery(patientId));
        return ToResponse(result);
    }

    /// <summary>جزئیات کامل یک ویزیت</summary>
    /// <param name="id">شناسه ویزیت</param>
    [HttpGet("{id:long}")]
    [HasPermission("visits.view")]
    public async Task<IActionResult> GetById(long id)
    {
        var result = await _mediator.Send(new GetVisitByIdQuery(id));
        return ToResponse(result);
    }

    /// <summary>ذخیره یا تکمیل ویزیت — ذخیره تدریجی SOAP پشتیبانی می‌شود</summary>
    /// <param name="dto">داده ویزیت</param>
    [HttpPost]
    [HasPermission("visits.create")]
    public async Task<IActionResult> Save([FromBody] SaveVisitDto dto)
    {
        var result = await _mediator.Send(new SaveVisitCommand(dto));
        if (result.IsSuccess)
            return CreatedResponse(result, nameof(GetById), new { id = result.Value }, "ویزیت ذخیره شد");
        return ToResponse(result);
    }
}
