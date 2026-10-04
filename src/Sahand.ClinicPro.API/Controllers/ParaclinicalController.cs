using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sahand.ClinicPro.API.Filters;
using Sahand.ClinicPro.Application.Features.Paraclinical;

namespace Sahand.ClinicPro.API.Controllers;

/// <summary>
/// کنترلر پاراکلینیک — تصویربرداری و دندانپزشکی و چشم و فیزیوتراپی
/// مسیرها: /api/v1/imaging و /api/v1/dental و /api/v1/eye و /api/v1/physiotherapy
/// </summary>
[ApiController]
[Authorize]
public class ParaclinicalController : ApiBaseController
{
    private readonly IMediator _mediator;

    /// <summary>سازنده با تزریق میانجی</summary>
    public ParaclinicalController(IMediator mediator) => _mediator = mediator;

    /// <summary>ثبت درخواست تصویربرداری</summary>
    [HttpPost("api/v1/imaging/orders")]
    [HasPermission("imaging.create")]
    public async Task<IActionResult> CreateImagingOrder([FromBody] CreateImagingOrderDto dto)
    {
        var result = await _mediator.Send(new CreateImagingOrderCommand(dto));
        if (result.IsSuccess)
            return CreatedResponse(result, "GetOrders", new { }, "درخواست تصویربرداری ثبت شد");
        return ToResponse(result);
    }

    /// <summary>ثبت گزارش رادیولوژی</summary>
    [HttpPut("api/v1/imaging/reports")]
    [HasPermission("imaging.edit")]
    public async Task<IActionResult> SubmitReport([FromBody] ImagingReportDto dto)
    {
        var result = await _mediator.Send(new SubmitImagingReportCommand(dto));
        return ToResponse(result, "گزارش ثبت شد");
    }

    /// <summary>ثبت طرح درمان دندانپزشکی</summary>
    [HttpPost("api/v1/dental/plans")]
    [HasPermission("dental.create")]
    public async Task<IActionResult> CreateDentalPlan([FromBody] CreateDentalPlanCommand command)
    {
        var result = await _mediator.Send(command);
        if (result.IsSuccess)
            return CreatedResponse(result, "GetOrders", new { }, "طرح درمان ثبت شد");
        return ToResponse(result);
    }

    /// <summary>ثبت معاینه چشم‌پزشکی</summary>
    [HttpPost("api/v1/eye/exams")]
    [HasPermission("eye.create")]
    public async Task<IActionResult> SaveEyeExam([FromBody] SaveEyeExamCommand command)
    {
        var result = await _mediator.Send(command);
        if (result.IsSuccess)
            return CreatedResponse(result, "GetOrders", new { }, "معاینه چشم ثبت شد");
        return ToResponse(result);
    }

    /// <summary>ثبت جلسه فیزیوتراپی</summary>
    [HttpPost("api/v1/physiotherapy/sessions")]
    [HasPermission("physiotherapy.create")]
    public async Task<IActionResult> RecordSession([FromBody] RecordPhysioSessionCommand command)
    {
        var result = await _mediator.Send(command);
        if (result.IsSuccess)
            return CreatedResponse(result, "GetOrders", new { }, "جلسه ثبت شد");
        return ToResponse(result);
    }
}
