namespace Sahand.ClinicPro.Desktop.Services;

/// <summary>
/// پاسخ خطای استاندارد API — برای تشخیص پیام فارسی سرور
/// </summary>
public record ApiErrorResponse(bool IsSuccess, string Message, int StatusCode);
