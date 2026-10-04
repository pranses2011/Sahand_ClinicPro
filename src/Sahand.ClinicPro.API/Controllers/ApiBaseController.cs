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
