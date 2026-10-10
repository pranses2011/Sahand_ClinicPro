using Sahand.ClinicPro.Application.Interfaces;
using System.Text.Json;

namespace Sahand.ClinicPro.API.Middleware;

/// <summary>
/// میدلور اعتبارسنجی لایسنس — کنترل ماژول‌های فعال روی مسیرهای API
/// خطاهای سه‌گانه استاندارد با کد ۴۰۳:
/// LICENSE_INVALID و USER_LIMIT_EXCEEDED و MODULE_NOT_LICENSED
/// </summary>
public class LicenseValidationMiddleware
{
    private readonly RequestDelegate _next;

    /// <summary>سازنده با نقطه بعدی خط لوله</summary>
    public LicenseValidationMiddleware(RequestDelegate next) => _next = next;

    /// <summary>فراخوانی میدلور — نقشه ماژول از مسیر</summary>
    public async Task InvokeAsync(HttpContext context, ILicenseService licenseService)
    {
        // نقشه پیشوند مسیر به کد ماژول
        // ⚠️ نکتهٔ معماری چرخهٔ ۴۱: پیشوند api/v1/consumables عمداً در این نقشه
        // نیست — لوازم مصرفی درمانگاه (سرنگ/سرم/گاز) جزو جریان اصلی «پذیرش خدمتی»
        // است نه ماژول اختیاری داروخانه؛ درمانگاه بدون لایسنس Pharmacy هم باید
        // بتواند مصرفی‌ها را تعریف/انبار/مصرف کند (گزارش مالک). مجوزهای این مسیر
        // در خود ConsumablesController اعمال می‌شوند (receptions.view/create برای
        // فهرست/مصرف و نقش Admin/Manager برای تعریف قلم و ورود کالا).
        var moduleMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["api/v1/patients"] = "Patients",
            ["api/v1/appointments"] = "Appointments",
            ["api/v1/receptions"] = "Receptions",
            ["api/v1/visits"] = "Visits",
            ["api/v1/prescriptions"] = "Prescriptions",
            ["api/v1/invoices"] = "Billing",
            ["api/v1/payments"] = "Billing",
            ["api/v1/accounting"] = "Accounting",
            ["api/v1/insurance"] = "Insurance",
            ["api/v1/lab"] = "Lab",
            ["api/v1/pharmacy"] = "Pharmacy",
            ["api/v1/sms"] = "Sms",
            ["api/v1/reports"] = "Reports"
        };

        // کاربر احراز هویت‌نشده را چالش احراز هویت (۴۰۱) مدیریت می‌کند — نه لایسنس
        if (context.User.Identity?.IsAuthenticated != true)
        {
            await _next(context);
            return;
        }

        var path = context.Request.Path.Value ?? string.Empty;
        var matched = moduleMap.FirstOrDefault(kv =>
            path.StartsWithsWith($"/{kv.Key}") || path.Contains($"/{kv.Key}/"));

        // فقط مسیرهای ماژول‌محور کنترل می‌شوند — احراز هویت و سلامت آزادند
        if (!string.IsNullOrEmpty(matched.Key))
        {
            var status = await licenseService.GetStatusAsync();

            if (!status.IsValid)
            {
                await WriteForbidden(context, "LICENSE_INVALID",
                    "لایسنس سامانه معتبر نیست — با پشتیبانی تماس بگیرید");
                return;
            }

            if (!await licenseService.IsModuleEnabledAsync(matched.Value))
            {
                await WriteForbidden(context, "MODULE_NOT_LICENSED",
                    $"ماژول {matched.Value} در لایسنس شما فعال نیست");
                return;
            }
        }

        await _next(context);
    }

    /// <summary>نوشتن پاسخ ۴۰۳ استاندارد فارسی</summary>
    private static async Task WriteForbidden(HttpContext context, string code, string message)
    {
        context.Response.StatusCode = StatusCodes.Status403Forbidden;
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsync(JsonSerializer.Serialize(new
        {
            isSuccess = false,
            statusCode = 403,
            errorCode = code,
            message
        }));
    }
}

/// <summary>متدهای الحاقی مسیر</summary>
internal static class PathExtensions
{
    /// <summary>شروع با پیشوند — بدون حساسیت بزرگی حروف</summary>
    public static bool StartsWithsWith(this string path, string prefix) =>
        path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase);
}
