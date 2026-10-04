using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sahand.ClinicPro.API.Filters;
using Sahand.ClinicPro.Application.Features.Laboratory;
using Sahand.ClinicPro.Application.Features.Lookups;
using Sahand.ClinicPro.Domain.Common;
using Sahand.ClinicPro.Domain.Enums;

namespace Sahand.ClinicPro.API.Controllers;

/// <summary>
/// کنترلر آزمایشگاه — درخواست‌ها و نتایج
/// مسیرها: /api/v1/lab
/// </summary>
[ApiController]
[Route("api/v1/lab")]
[Authorize]
public class LabController : ApiBaseController
{
    private readonly IMediator _mediator;

    /// <summary>سازنده با تزریق میانجی</summary>
    public LabController(IMediator mediator) => _mediator = mediator;

    /// <summary>فهرست درخواست‌های آزمایش</summary>
    [HttpGet("orders")]
    [HasPermission("lab.view")]
    public async Task<IActionResult> GetOrders([FromQuery] LabOrderStatus? status,
        [FromQuery] long? patientId)
    {
        var result = await _mediator.Send(new GetLabOrdersQuery(status, patientId));
        return ToResponse(result);
    }

    /// <summary>جزئیات درخواست آزمایش با اقلام — برای ثبت نتیجه</summary>
    /// <param name="id">شناسه درخواست</param>
    [HttpGet("orders/{id:long}")]
    [HasPermission("lab.view")]
    public async Task<IActionResult> GetById(long id)
    {
        var result = await _mediator.Send(new GetLabOrderByIdQuery(id));
        return ToResponse(result);
    }

    /// <summary>ثبت درخواست آزمایش جدید</summary>
    [HttpPost("orders")]
    [HasPermission("lab.create")]
    public async Task<IActionResult> CreateOrder([FromBody] CreateLabOrderDto dto)
    {
        var result = await _mediator.Send(new CreateLabOrderCommand(dto));
        if (result.IsSuccess)
            return CreatedResponse(result, "GetOrders", new { }, "درخواست آزمایش ثبت شد");
        return ToResponse(result);
    }

    /// <summary>ثبت نتیجه یک قلم آزمایش</summary>
    [HttpPut("results")]
    [HasPermission("lab.edit")]
    public async Task<IActionResult> RecordResult([FromBody] RecordResultDto dto)
    {
        var result = await _mediator.Send(new RecordLabResultCommand(dto));
        return ToResponse(result, "نتیجه ثبت شد");
    }
}
