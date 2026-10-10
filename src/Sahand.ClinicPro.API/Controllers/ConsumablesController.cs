using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sahand.ClinicPro.API.Filters;
using Sahand.ClinicPro.Application.Features.Lookups;
using Sahand.ClinicPro.Application.Features.Pharmacy;
using Sahand.ClinicPro.Domain.Enums;
using System.Security.Claims;

namespace Sahand.ClinicPro.API.Controllers;

/// <summary>
/// کنترلر لوازم مصرفی درمانگاه — چرخهٔ چهل‌ویکم (گزارش مالک)
/// مسیرها: /api/v1/consumables
/// </summary>
/// <remarks>
/// «احتمال داره یک درمانگاه ماژول داروخانه رو نداشته باشه و اونموقع نمیتونه
/// لوازم مصرفی رو تعریف و استفاده بکنه» — تا چرخهٔ ۳۹ کل چرخهٔ لوازم مصرفی
/// (فهرست اقلام دارای موجودی، تعریف قلم، ورود کالا و برداشت هنگام پذیرش)
/// روی اندپوینت‌های /api/v1/pharmacy سوار بود و میدلور لایسنس همان پیشوند
/// را به ماژول «Pharmacy» گره زده بود؛ یعنی درمانگاهِ بدون لایسنس داروخانه
/// حتی برای سرنگ و گازِ پذیرش هم ۴۰۳ MODULE_NOT_LICENSED می‌گرفت.
///
/// راهکار این چرخه: «مسیر مستقل و بدون‌گیتِ ماژولی» — این کنترلر عمداً در
/// نقشهٔ ماژول‌های LicenseValidationMiddleware نیست (مثل lookups) چون مصرف
/// سرنگ/سرم/گاز حین خدمت، جزو جریان اصلی پذیرش است نه داروخانهٔ اختیاری.
/// تمام منطق کسب‌وکار روی همان Handlerهای چرخهٔ ۳۹ (MediatR) بازاستفاده
/// می‌شود — هیچ قاعدهٔ انباری دومی وجود ندارد تا دو مسیر از هم واگرا شوند.
///
/// نقشهٔ مجوزها (آگاهانه متفاوت از داروخانه):
///   • فهرست‌ها → receptions.view — هرکس می‌تواند پذیرش ثبت کند، مصرفی‌ها را
///     می‌بیند (حذف اصطکاکِ چرخهٔ ۳۹ که مجوز pharmacy.* به نقش پذیرش می‌خواست)؛
///   • برداشت مصرف → receptions.create — مصرف، حلقهٔ طبیعیِ ثبت پذیرش خدمتی است؛
///   • تعریف قلم و ورود کالا (عملیات مدیریتی/مالی خرید) → نقش Admin/Manager —
///     همان گیتِ POST lookups/medications برای سازگاری کامل.
/// </remarks>
[ApiController]
[Route("api/v1/consumables")]
[Authorize]
public class ConsumablesController : ApiBaseController
{
    private readonly IMediator _mediator;

    /// <summary>سازنده با تزریق میانجی</summary>
    public ConsumablesController(IMediator mediator) => _mediator = mediator;

    /// <summary>
    /// فهرست اقلام مصرفی دارای موجودی — برای بخش «لوازم استفاده‌شده» فرم پذیرش؛
    /// بچ‌های هر قلم تجمیع و قیمت فروش پیشنهادی برگردانده می‌شود (همان خروجی
    /// چرخهٔ ۳۹، این‌بار بدون گیت ماژول داروخانه).
    /// </summary>
    /// <param name="search">جستجوی اختیاری در نام قلم</param>
    [HttpGet]
    [HasPermission("receptions.view")]
    public async Task<IActionResult> GetConsumables([FromQuery] string? search)
    {
        var result = await _mediator.Send(new GetConsumablesQuery(search));
        return ToResponse(result);
    }

    /// <summary>
    /// فهرست بچ‌های لوازم مصرفی — برای صفحهٔ مدیریتی «لوازم مصرفی»؛ نوع قلم
    /// سمت سرور به «مصرفی» قفل می‌شود تا داروها هرگز به این فهرست نشت نکنند
    /// (برخلاف فیلتر اختیاری اندپوینت داروخانه که هر دو نوع را برمی‌گرداند).
    /// </summary>
    /// <param name="lowStock">فقط بچ‌های زیر نقطه سفارش</param>
    [HttpGet("inventory")]
    [HasPermission("receptions.view")]
    public async Task<IActionResult> GetInventory([FromQuery] bool? lowStock)
    {
        var result = await _mediator.Send(new GetInventoryQuery(lowStock, MedicationItemKind.Consumable));
        return ToResponse(result);
    }

    /// <summary>
    /// تعریف قلم جدید (دارو یا لوازم مصرفی) — همان فرمان مشترک چرخهٔ ۳۹؛
    /// نوع قلم از خود درخواست می‌آید چون فرمِ «قلم جدید» برای داروخانه و
    /// صفحهٔ مصرفی‌ها مشترک است. نوشتن دادهٔ مرجع نیازمند نقش مدیریتی است
    /// (هم‌سبک POST lookups/medications).
    /// </summary>
    /// <param name="command">مشخصات قلم — نام فارسی/ژنریک، نوع و مشخصات اختیاری</param>
    [HttpPost("items")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> CreateItem([FromBody] CreatePharmacyItemCommand command)
    {
        var result = await _mediator.Send(command);
        if (result.IsSuccess)
            return CreatedResponse(result, nameof(GetInventory), new { }, "قلم انبار ثبت شد");
        return ToResponse(result);
    }

    /// <summary>
    /// ورود کالا (خرید) برای یک قلم — بچ با تاریخ انقضا، قیمت خرید/فروش و
    /// تراکنش انبار ثبت می‌شود؛ همان فرمان ورود کالای داروخانه، بدون گیت
    /// ماژولی. عملیات مالی خرید است → نقش مدیریتی.
    /// چرخهٔ ۴۳ (گزارش مالک: «چون اقلام مصرفی دیگه از داروخانه مستقل شده، در
    /// افزودن قلم جدید و ورود کالا نباید نوع قلم وجود داشته باشه») — نوع
    /// «لوازم مصرفی» سمت سرور قفل می‌شود تا داروها از این مسیر وارد نشوند.
    /// </summary>
    /// <param name="command">شناسهٔ قلم + بچ/تعداد/قیمت‌ها/انقضای شمسی</param>
    [HttpPost("inventory")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> StockIn([FromBody] StockInCommand command)
    {
        var result = await _mediator.Send(command with { ExpectedItemKind = MedicationItemKind.Consumable });
        if (result.IsSuccess)
            return CreatedResponse(result, nameof(GetInventory), new { }, "موجودی ثبت شد");
        return ToResponse(result);
    }

    /// <summary>
    /// ثبت مصرف لوازم درمانگاه برای یک پذیرش — کاهش FEFO موجودی + تراکنش انبار
    /// با منشأ Reception + برگشت اقلام با قیمت واقعی بچ‌ها برای فاکتور بیمار
    /// (همان فرمان چرخهٔ ۳۹). مجوز همان ثبت پذیرش است چون مصرف، حلقهٔ طبیعی
    /// همانی است که در فرم پذیرش خدمتی اتفاق می‌افتد.
    /// </summary>
    /// <param name="command">شناسهٔ پذیرش + اقلام با تعداد</param>
    [HttpPost("consume")]
    [HasPermission("receptions.create")]
    public async Task<IActionResult> Consume([FromBody] ConsumeStockCommand command)
    {
        // شناسهٔ کاربر از توکن — رد پایهٔ تراکنش‌های انبار («چه کسی برداشت کرد»)
        var userIdText = User.FindFirstValue(ClaimTypes.NameIdentifier);
        long? userId = long.TryParse(userIdText, out var uid) ? uid : null;

        var result = await _mediator.Send(command with { UserId = userId });
        return ToResponse(result, "مصرف لوازم ثبت شد و به هزینه‌های پذیرش اضافه شد");
    }
}
