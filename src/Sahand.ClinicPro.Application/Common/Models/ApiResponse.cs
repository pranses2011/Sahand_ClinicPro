namespace Sahand.ClinicPro.Application.Common.Models;

/// <summary>
/// پاسخ استاندارد همه endpointهای API — فرمت یکسان برای موفقیت و خطا
/// </summary>
/// <typeparam name="T">نوع داده خروجی</typeparam>
public class ApiResponse<T>
{
    /// <summary>آیا عملیات موفق بود</summary>
    public bool IsSuccess { get; init; }

    /// <summary>کد وضعیت HTTP</summary>
    public int StatusCode { get; init; }

    /// <summary>پیام فارسی قابل نمایش به کاربر</summary>
    public string Message { get; init; } = string.Empty;

    /// <summary>داده خروجی در صورت موفقیت</summary>
    public T? Data { get; init; }

    /// <summary>فهرست خطاها در صورت شکست</summary>
    public IReadOnlyList<string> Errors { get; init; } = Array.Empty<string>();

    /// <summary>شناسه ردیابی درخواست</summary>
    public string TraceId { get; init; } = string.Empty;

    /// <summary>زمان اجرای درخواست به میلی‌ثانیه</summary>
    public long ElapsedMs { get; init; }

    /// <summary>ساخت پاسخ موفق با داده</summary>
    public static ApiResponse<T> Success(T data, string message = "عملیات با موفقیت انجام شد",
        int statusCode = 200) => new()
    {
        IsSuccess = true, StatusCode = statusCode, Message = message, Data = data
    };

    /// <summary>ساخت پاسخ موفق بدون داده</summary>
    public static ApiResponse<T> SuccessMessage(string message = "عملیات با موفقیت انجام شد",
        int statusCode = 200) => new()
    {
        IsSuccess = true, StatusCode = statusCode, Message = message
    };

    /// <summary>ساخت پاسخ ناموفق</summary>
    public static ApiResponse<T> Failure(string message, int statusCode = 400,
        IReadOnlyList<string>? errors = null) => new()
    {
        IsSuccess = false, StatusCode = statusCode, Message = message,
        Errors = errors ?? Array.Empty<string>()
    };
}
