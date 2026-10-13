using Microsoft.AspNetCore.Authorization;
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

    /// <summary>ثبت پرداخت — نقدی یا کارتی یا آنلاین</summary>
    [HttpPost]
    [HasPermission("payments.create")]
    public async Task<IActionResult> Record([FromBody] RecordPaymentDto dto)
    {
        var result = await _mediator.Send(new RecordPaymentCommand(dto));
        if (result.IsSuccess)
            // چرخهٔ سی‌امین — نام اکشن «GetAll» در این کنترلر وجود ندارد؛
            // در نتیجه هر ثبتِ موفقِ پرداخت پس از ذخیره با «No route matches
            // the supplied values» فرو می‌ریخت و کلاینت ۵۰۰ می‌گرفت. هدف
            // طبیعی، فهرست تراکنش‌های امروز همان صندوق است.
            return CreatedResponse(result, nameof(GetTodayTransactions), new { }, "پرداخت با موفقیت ثبت شد");
        return ToResponse(result);
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
