using Microsoft.AspNetCore.Mvc;
using Sahand.ClinicPro.Application.Common.Models;
using Sahand.ClinicPro.Domain.Common;

namespace Sahand.ClinicPro.API.Controllers;

/// <summary>
/// کنترلر پایه همه کنترلرها — تبدیل Result دامنه به ApiResponse استاندارد
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
public abstract class ApiBaseController : ControllerBase
{
    /// <summary>تبدیل نتیجه دامنه به پاسخ HTTP</summary>
    protected IActionResult ToResponse<T>(Result<T> result, string? successMessage = null)
    {
        if (result.IsFailure)
        {
            return BadRequest(ApiResponse<T>.Failure(result.Error.Message,
                StatusCodes.Status400BadRequest,
                new List<string> { result.Error.Code }));
        }

        return Ok(ApiResponse<T>.Success(result.Value!,
            successMessage ?? "عملیات با موفقیت انجام شد"));
    }

    /// <summary>
    /// پاسخ صفحه‌بندی‌شده — چرخه نوزدهم (گزارش سیزدهم مالک):
    /// PagedResponse خودش فرزند ApiResponse است؛ اگر مثل بقیه در ApiResponse پوشانده شود
    /// پاسخ «دوبل» می‌شود (data.data) و کلاینت دسکتاپ که data را در ریشه انتظار دارد
    /// فهرست را خالی می‌بیند (بیماران/پزشکان/نسخه‌ها/فاکتورها). این متد مقدار را
    /// مستقیم و تک‌لایه برمی‌گرداند: { isSuccess, message, data: [...], pageNumber, ... }
    /// </summary>
    protected IActionResult ToPagedResponse<T>(Result<PagedResponse<T>> result)
    {
        if (result.IsFailure)
        {
            return BadRequest(ApiResponse<PagedResponse<T>>.Failure(result.Error.Message,
                StatusCodes.Status400BadRequest,
                new List<string> { result.Error.Code }));
        }

        return Ok(result.Value);
    }

    /// <summary>تبدیل نتیجه بدون داده به پاسخ HTTP</summary>
    protected IActionResult ToResponse(Result result, string? successMessage = null)
    {
        if (result.IsFailure)
        {
            return BadRequest(ApiResponse<object>.Failure(result.Error.Message,
                StatusCodes.Status400BadRequest,
                new List<string> { result.Error.Code }));
        }

        return Ok(ApiResponse<object>.SuccessMessage(successMessage ?? "عملیات با موفقیت انجام شد"));
    }

    /// <summary>
    /// پاسخ ساخته‌شده موفق — کد 201 با مکان منبع.
    /// چرخهٔ سی‌امین — رفع «InvalidOperationException: No route matches the
    /// supplied values»: پیش‌تر CreatedAtAction مستقیم برمی‌گشت و اگر نام
    /// اکشن هدف در کنترلر وجود نداشت (مثل «GetOrders» در پاراکلینیک که
    /// اکشن واقعی GetTodayOrders بود)، استثنا نه هنگام ساختن پاسخ بلکه
    /// «در مرحلهٔ اجرای نتیجه» پرتاب می‌شد — یعنی پس از ذخیرهٔ موفق داده،
    /// کلاینت دسکتاپ پاسخ ۵۰۰ «خطای غیرمنتظره» می‌گرفت و کاربر گمان می‌کرد
    /// عملیات (طرح درمان دندانپزشکی و…) ثبت نشده است. اکنون نشانی منبع
    /// همین‌جا و به‌صورت زودهنگام ساخته می‌شود؛ اگر مسیری یافت نشد، پاسخ
    /// 201 بدون سرآیند Location برگردانده می‌شود (کلاینت دسکتاپ فقط بدنهٔ
    /// JSON استاندارد را می‌خواند) تا این رده از خطا برای همیشه ناممکن شود.
    /// </summary>
    protected IActionResult CreatedResponse<T>(Result<T> result, string actionName, object routeValues,
        string? successMessage = null)
    {
        if (result.IsFailure)
        {
            return BadRequest(ApiResponse<T>.Failure(result.Error.Message,
                StatusCodes.Status400BadRequest,
                new List<string> { result.Error.Code }));
        }

        var payload = ApiResponse<T>.Success(result.Value!, successMessage ?? "با موفقیت ایجاد شد");

        // ساخت نشانی با IUrlHelper — null یعنی مسیری با این نام/مقادیر وجود ندارد
        var location = Url?.Action(actionName, routeValues);
        return location is null
            ? StatusCode(StatusCodes.Status201Created, payload)
            : CreatedAtAction(actionName, routeValues, payload);
    }
}
