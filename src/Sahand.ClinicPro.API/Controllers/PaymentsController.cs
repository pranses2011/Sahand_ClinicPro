using Microsoft.EntityFrameworkCore;
using Sahand.ClinicPro.Application.Common.Models;
﻿using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sahand.ClinicPro.API.Filters;
using Sahand.ClinicPro.Application.DTOs.Billing;
using Sahand.ClinicPro.Application.Features.Billing.Commands.RecordPayment;
using Sahand.ClinicPro.Domain.Common;
using Sahand.ClinicPro.Application.Features.Billing.GetCashRegister;
using Sahand.ClinicPro.Application.Features.Billing.Shifts;

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

    /// <summary>ثبت یا بازخوانی ایمن همان درخواست؛ این مسیر شناسه پایدار را الزامی می‌کند.</summary>
    [HttpPost("idempotent")]
    [HasPermission("payments.create")]
    public async Task<IActionResult> RecordIdempotent([FromBody] RecordPaymentDto dto)
    {
        if (!dto.RequestId.HasValue || dto.RequestId == Guid.Empty)
            return BadRequest(ApiResponse<object>.Failure("شناسه پایدار درخواست پرداخت الزامی است.", 400));
        return await Record(dto);
    }

    /// <summary>ثبت پرداخت — نقدی یا کارتی یا آنلاین</summary>
    [HttpPost]
    [HasPermission("payments.create")]
    public async Task<IActionResult> Record([FromBody] RecordPaymentDto dto)
    {
        try
        {
            var result = await _mediator.Send(new RecordPaymentCommand(dto));
            if (result.IsFailure && result.Error.Code == "PAYMENT.REQUEST_CONFLICT")
                return Conflict(ApiResponse<object>.Failure(result.Error.Message, 409));
            if (result.IsSuccess)
                return CreatedResponse(result, nameof(GetTodayTransactions), new { }, "پرداخت با موفقیت ثبت شد");
            return ToResponse(result);
        }
        catch (DbUpdateConcurrencyException)
        {
            return Conflict(ApiResponse<object>.Failure("فاکتور همزمان تغییر کرد؛ پیش از تلاش مجدد وضعیت پرداخت و مانده را بررسی کنید.", 409));
        }
    }

    /// <summary>خلاصه صندوق امروز — کارت‌های آماری فرم ۱۴</summary>
    [HttpGet("today-summary")]
    [HasPermission("payments.view")] // ممیزی چرخه ۱۲
    public async Task<IActionResult> GetTodaySummary()
    {
        var result = await _mediator.Send(new GetCashRegisterQuery());
        return ToResponse(result);
    }

    /// <summary>تراکنش‌های امروز صندوق</summary>
    [HttpGet("today")]
    [HasPermission("payments.view")] // ممیزی چرخه ۱۲
public async Task<IActionResult> GetTodayTransactions()
    {
        var result = await _mediator.Send(new GetTodayTransactionsQuery());
        return ToResponse(result);
    }

    /* ═══════════ بستن شیفت و تحویل صندوق — چرخه ۱۳ (درخواست ۲-۲) ═══════════ */

    /// <summary>بستن شیفت — شمارش فیزیکی + اختلاف‌سنجی + رسید تحویل</summary>
    [HttpPost("shifts/close")]
    [HasPermission("payments.create")]
    public async Task<IActionResult> CloseShift([FromBody] CloseShiftDto dto)
    {
        var result = await _mediator.Send(new CloseShiftCommand(dto));
        return ToResponse(result, "شیفت بسته و رسید تحویل صادر شد");
    }

    /// <summary>تاریخچه شیفت‌های بسته‌شده — ۱۰۰ مورد اخیر</summary>
    [HttpGet("shifts")]
    [HasPermission("payments.view")]
    public async Task<IActionResult> GetShifts()
    {
        var result = await _mediator.Send(new GetShiftsQuery());
        return ToResponse(result);
    }

    /// <summary>
    /// گزینه‌های تحویل‌گیرندهٔ شیفت بعد — بازطراحی چرخهٔ ۷۰ (گزارش مالک:
    /// «در بستن شیفت تحویل‌گیرنده را نمی‌آورد و لیستش خالی است»).
    /// ریشه: پنجرهٔ بستن شیفت فهرست را از «GET users» می‌خواند که مجوز
    /// «users.view» (مدیریتی) می‌خواهد؛ صندوقدار این مجوز را ندارد → ۴۰۳ →
    /// لیست خالی. این اندپوینت با همان مجوزِ بستن شیفت (payments.create)
    /// کار می‌کند: اگر می‌توانی شیفت ببندی، می‌توانی صندوقدارِ بعدی را ببینی.
    /// </summary>
    [HttpGet("shifts/handover-candidates")]
    [HasPermission("payments.create")]
    public async Task<IActionResult> GetHandoverCandidates(CancellationToken ct = default)
    {
        var result = await _mediator.Send(new GetHandoverCandidatesQuery(), ct);
        return ToResponse(result);
    }
}
