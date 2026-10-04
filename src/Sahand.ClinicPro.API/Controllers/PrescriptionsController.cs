using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sahand.ClinicPro.API.Filters;
using Sahand.ClinicPro.Application.DTOs.Prescriptions;
using Sahand.ClinicPro.Application.Features.Prescriptions.Commands.CreatePrescription;
using Sahand.ClinicPro.Application.Features.Prescriptions.Queries;

namespace Sahand.ClinicPro.API.Controllers;

/// <summary>
/// کنترلر نسخه — صدور و مشاهده نسخه‌ها با بررسی تداخل دارویی
/// مسیرها: /api/v1/prescriptions
/// </summary>
[ApiController]
[Route("api/v1/prescriptions")]
[Authorize]
public class PrescriptionsController : ApiBaseController
{
    private readonly IMediator _mediator;

    /// <summary>سازنده با تزریق میانجی</summary>
    public PrescriptionsController(IMediator mediator) => _mediator = mediator;

    /// <summary>نسخه‌های یک ویزیت</summary>
    /// <param name="visitId">شناسه ویزیت</param>
    [HttpGet("visit/{visitId:long}")]
    [HasPermission("prescriptions.view")]
    public async Task<IActionResult> GetByVisit(long visitId)
    {
        var result = await _mediator.Send(new GetVisitPrescriptionsQuery(visitId));
        return ToResponse(result);
    }

    /// <summary>جزئیات کامل نسخه با اقلام دارو</summary>
    /// <param name="id">شناسه نسخه</param>
    [HttpGet("{id:long}")]
    [HasPermission("prescriptions.view")]
    public async Task<IActionResult> GetById(long id)
    {
        var result = await _mediator.Send(new GetPrescriptionByIdQuery(id));
        return ToResponse(result);
    }

    /// <summary>صدور نسخه جدید — با بررسی تداخل ممنوع مصرف</summary>
    /// <param name="dto">داده نسخه و اقلام</param>
    [HttpPost]
    [HasPermission("prescriptions.create")]
    public async Task<IActionResult> Create([FromBody] CreatePrescriptionDto dto)
    {
        var result = await _mediator.Send(new CreatePrescriptionCommand(dto));
        if (result.IsSuccess)
            return CreatedResponse(result, nameof(GetById), new { id = result.Value }, "نسخه صادر شد");
        return ToResponse(result);
    }
}
