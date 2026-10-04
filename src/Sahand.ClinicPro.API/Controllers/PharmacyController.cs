using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sahand.ClinicPro.API.Filters;
using Sahand.ClinicPro.Application.Features.Pharmacy;

namespace Sahand.ClinicPro.API.Controllers;

/// <summary>
/// کنترلر داروخانه — موجودی و تحویل نسخه
/// مسیرها: /api/v1/pharmacy
/// </summary>
[ApiController]
[Route("api/v1/pharmacy")]
[Authorize]
public class PharmacyController : ApiBaseController
{
    private readonly IMediator _mediator;

    /// <summary>سازنده با تزریق میانجی</summary>
    public PharmacyController(IMediator mediator) => _mediator = mediator;

    /// <summary>فهرست موجودی — با فیلتر کمبود</summary>
    [HttpGet("inventory")]
    [HasPermission("pharmacy.view")]
    public async Task<IActionResult> GetInventory([FromQuery] bool? lowStock)
    {
        var result = await _mediator.Send(new GetInventoryQuery(lowStock));
        return ToResponse(result);
    }

    /// <summary>ورود موجودی جدید — بچ با تاریخ انقضا</summary>
    [HttpPost("inventory")]
    [HasPermission("pharmacy.create")]
    public async Task<IActionResult> StockIn([FromBody] StockInCommand command)
    {
        var result = await _mediator.Send(command);
        if (result.IsSuccess)
            return CreatedResponse(result, "GetInventory", new { }, "موجودی ثبت شد");
        return ToResponse(result);
    }

    /// <summary>تحویل نسخه — کاهش FEFO و هشدار کمبود خودکار</summary>
    /// <param name="prescriptionId">شناسه نسخه</param>
    [HttpPost("dispense/{prescriptionId:long}")]
    [HasPermission("pharmacy.create")]
    public async Task<IActionResult> Dispense(long prescriptionId)
    {
        var result = await _mediator.Send(new DispensePrescriptionCommand(prescriptionId));
        return ToResponse(result, "نسخه تحویل شد");
    }
}
