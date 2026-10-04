using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sahand.ClinicPro.API.Filters;
using Sahand.ClinicPro.Application.DTOs.Billing;
using Sahand.ClinicPro.Application.Features.Billing.Commands.RecordPayment;
using Sahand.ClinicPro.Domain.Common;

namespace Sahand.ClinicPro.API.Controllers;

/// <summary>
/// کنترلر پرداخت — ثبت دریافتی با به‌روزرسانی خودکار وضعیت فاکتور
/// مسیرها: /api/v1/payments
/// </summary>
[ApiController]
[Route("api/v1/payments")]
[Authorize]
public class PaymentsController : ApiBaseController
{
    private readonly IMediator _mediator;

    /// <summary>سازنده با تزریق میانجی</summary>
    public PaymentsController(IMediator mediator) => _mediator = mediator;

    /// <summary>ثبت پرداخت — نقدی یا کارتی یا آنلاین</summary>
    [HttpPost]
    [HasPermission("payments.create")]
    public async Task<IActionResult> Record([FromBody] RecordPaymentDto dto)
    {
        var result = await _mediator.Send(new RecordPaymentCommand(dto));
        if (result.IsSuccess)
            return CreatedResponse(result, "GetAll", new { }, "پرداخت با موفقیت ثبت شد");
        return ToResponse(result);
    }
}
