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

    /// <summary>پاسخ ساخته‌شده موفق — کد 201 با مکان منبع</summary>
    protected IActionResult CreatedResponse<T>(Result<T> result, string actionName, object routeValues,
        string? successMessage = null)
    {
        if (result.IsFailure)
        {
            return BadRequest(ApiResponse<T>.Failure(result.Error.Message,
                StatusCodes.Status400BadRequest,
                new List<string> { result.Error.Code }));
        }

        return CreatedAtAction(actionName, routeValues,
            ApiResponse<T>.Success(result.Value!, successMessage ?? "با موفقیت ایجاد شد"));
    }
}
