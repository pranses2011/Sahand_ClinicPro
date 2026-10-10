using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sahand.ClinicPro.API.Filters;
using Sahand.ClinicPro.Application.Features.Pharmacy;
using System.Security.Claims;

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

    /// <summary>فهرست موجودی — با فیلتر کمبود و نوع قلم (چرخهٔ ۳۹: دارو/لوازم مصرفی)</summary>
    /// <param name="lowStock">فقط اقلام زیر نقطه سفارش</param>
    /// <param name="itemKind">نوع قلم — ۱ دارو، ۲ لوازم مصرفی، تهی هر دو</param>
    [HttpGet("inventory")]
    [HasPermission("pharmacy.view")]
    public async Task<IActionResult> GetInventory([FromQuery] bool? lowStock,
        [FromQuery] Sahand.ClinicPro.Domain.Enums.MedicationItemKind? itemKind = null)
    {
        var result = await _mediator.Send(new GetInventoryQuery(lowStock, itemKind));
        return ToResponse(result);
    }

    /// <summary>
    /// ورود موجودی جدید — بچ با تاریخ انقضا. چرخهٔ ۴۳: قلم باید «دارو» باشد —
    /// داروخانه از این پس فقط کاتالوگ داروها را می‌پذیرد (گزارش مالک: «از
    /// داروخانه هم حذف شود چون فقط دارو در اونجا ثبت میشه» — نوع لوازم مصرفی
    /// از صفحهٔ مستقل مصرفی‌ها وارد می‌شود).
    /// </summary>
    [HttpPost("inventory")]
    [HasPermission("pharmacy.create")]
    public async Task<IActionResult> StockIn([FromBody] StockInCommand command)
    {
        var result = await _mediator.Send(command with { ExpectedItemKind = Sahand.ClinicPro.Domain.Enums.MedicationItemKind.Drug });
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

    // ═══════════ چرخهٔ سی‌ونه‌م (گزارش مالک) — لوازم مصرفی درمانگاه ═══════════
    // «لوازمی که استفاده می‌شود مثل سرنگ و سرم و گاز استریل… وقتی بیمار با
    // خودش نمیاره باید از لوازم خود درمانگاه استفاده بکنیم و در پذیرش برای
    // اونها هم هزینه به هزینه‌های بیمار اضافه بشه» — دو اندپوینت زیر فهرستِ
    // اقلام مصرفیِ در دسترس را می‌دهند و مصرفِ واقعی را با تراکنش انبار ثبت
    // می‌کنند؛ هزینهٔ برگشتی توسط پذیرش به فاکتور بیمار افزوده می‌شود.

    /// <summary>
    /// فهرست اقلام مصرفی دارای موجودی — برای بخش «لوازم استفاده‌شده» فرم پذیرش.
    /// بچ‌های هر قلم تجمیع و قیمت فروش پیشنهادی برگردانده می‌شود.
    /// </summary>
    /// <param name="search">جستجوی اختیاری در نام قلم</param>
    [HttpGet("consumables")]
    [HasPermission("pharmacy.view")]
    public async Task<IActionResult> GetConsumables([FromQuery] string? search)
    {
        var result = await _mediator.Send(new GetConsumablesQuery(search));
        return ToResponse(result);
    }

    /// <summary>
    /// ثبت مصرف لوازم درمانگاه برای یک پذیرش — کاهش FEFO موجودی + تراکنش انبار
    /// با منشأ Reception + برگشت فهرست اقلام با قیمت واقعی برای افزودن به فاکتور.
    /// </summary>
    /// <param name="command">شناسهٔ پذیرش + اقلام با تعداد</param>
    [HttpPost("consume")]
    [HasPermission("pharmacy.create")]
    public async Task<IActionResult> Consume([FromBody] ConsumeStockCommand command)
    {
        // شناسهٔ کاربر از توکن — رد پایهٔ تراکنش‌های انبار («چه کسی برداشت کرد»)
        var userIdText = User.FindFirstValue(ClaimTypes.NameIdentifier);
        long? userId = long.TryParse(userIdText, out var uid) ? uid : null;

        var result = await _mediator.Send(command with { UserId = userId });
        return ToResponse(result, "مصرف لوازم ثبت شد و به هزینه‌های پذیرش اضافه شد");
    }
}
