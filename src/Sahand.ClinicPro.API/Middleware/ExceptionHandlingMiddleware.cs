using System.Text.Json;
using Sahand.ClinicPro.Application.Common.Exceptions;
using Sahand.ClinicPro.Domain.Exceptions;

namespace Sahand.ClinicPro.API.Middleware;

/// <summary>
/// میدلور مدیریت خطا — همه استثناها را به پاسخ استاندارد فارسی تبدیل می‌کند
/// جزئیات خطای غیرمنتظر برای کاربر پنهان و در لاگ ثبت می‌شود
/// </summary>
public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;
    private readonly IHostEnvironment _environment;

    /// <summary>سازنده با تزریق وابستگی‌ها</summary>
    public ExceptionHandlingMiddleware(RequestDelegate next,
        ILogger<ExceptionHandlingMiddleware> logger, IHostEnvironment environment)
    {
        _next = next;
        _logger = logger;
        _environment = environment;
    }

    /// <summary>فراخوانی میدلور</summary>
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Microsoft.AspNetCore.Http.BadHttpRequestException)
        {
            // ═══ چرخهٔ سی‌امین — قطع اتصال کلاینت، خطای برنامه نیست ═══
            // «Unexpected end of request content»، «request body too long»
            // و موارد هم‌خانواده از خود Kestrel برمی‌خیزند و معنایش رفتنِ
            // کلاینت است، نه خرابی سرور. استثنای زیرساختی Kestrel باید به
            // خودش برگردد تا اتصال را بی‌سروصدا و تمیز ببندد؛ بلعیدنش و
            // نوشتن پاسخ JSON برای کلاینتِ رفته بی‌معناست — و ثبتش به‌عنوان
            // «خطای غیرمنتظره» (ERR) لاگ را با نویز کاذب پر می‌کرد. برای
            // ردیابی، سطح Debug کافی است (به‌طور پیش‌فرض خاموش است).
            _logger.LogDebug("درخواست ناقص/قطع‌شده توسط کلاینت بسته شد — {Path}", context.Request.Path);
            throw;
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    /// <summary>تبدیل استثنا به پاسخ استاندارد</summary>
    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var traceId = context.TraceIdentifier;
        var isServerError = false;
        var (statusCode, message, errors) = exception switch
        {
            AppValidationException validation => (StatusCodes.Status400BadRequest,
                "اطلاعات ارسالی نامعتبر است",
                validation.Errors.SelectMany(kv => kv.Value).ToList()),

            NotFoundException notFound => (StatusCodes.Status404NotFound,
                notFound.Message, new List<string>()),

            BusinessRuleException business => (StatusCodes.Status400BadRequest,
                business.Message, new List<string>()),

            ConflictException conflict => (StatusCodes.Status409Conflict,
                conflict.Message, new List<string>()),

            UnauthorizedAccessException => (StatusCodes.Status403Forbidden,
                "دسترسی لازم برای این عملیات را ندارید", new List<string>()),

            OperationCanceledException => (StatusCodes.Status499ClientClosedRequest,
                "درخواست توسط کلاینت لغو شد", new List<string>()),

            _ => (StatusCodes.Status500InternalServerError,
                "خطای غیرمنتظره در سرور رخ داد — با پشتیبانی تماس بگیرید",
                // ═══ چرخهٔ ۴۳ (گزارش مالک: «در ورود اقلام خطای غیر منتظره میده») ═══
                // برای خطاهای ۵۰۰، نوع و پیام کوتاه استثنا در آرایهٔ errors می‌آید تا
                // کلاینت همان را کنار پیام عمومی نشان دهد و «نخستین رخداد بعدی» بدون
                // نیاز به لاگ سرور قابل تشخیص باشد (مثل SqlException: String or binary
                // data would be truncated). جزئیات کامل (رد پشته) همچنان فقط در لاگ.
                new List<string>())
        };

        if (statusCode >= 500)
        {
            isServerError = true;
            _logger.LogError(exception, "خطای غیرمنتظره — TraceId: {TraceId}", traceId);
        }
        else
        {
            _logger.LogWarning("خطای {ExceptionType} — پیام: {Message} — TraceId: {TraceId}",
                exception.GetType().Name, exception.Message, traceId);
        }

        // ═══ چرخهٔ ۴۳ — جزئیات کوتاه استثنا فقط برای خطاهای سرور (۵۰۰+) ═══
        // «نوع استثنا: پیام» در آرایهٔ errors؛ کلاینت دسکتاپ نخستین جزء را به
        // پیام عمومی می‌چسباند و کاربر همان‌جا ریشهٔ واقعی را می‌بیند. رد پشته
        // و جزئیات درونی هرگز ارسال نمی‌شود (فقط لاگ سرور). پیام به ۳۰۰ نویسه
        // بریده می‌شود تا پاسخ سبک بماند (پیام‌های SqlException گاهی بلندند).
        if (isServerError)
        {
            var detail = $"{exception.GetType().Name}: {exception.Message}";
            if (detail.Length > 300)
                detail = detail[..300] + "…";
            errors = new List<string> { detail };
        }

        // ═══ چرخه بیست‌وچهارم (گزارش هفدهم مالک) — گارد پاسخِ آغازشده ═══
        // لاگ سرور پر از «InvalidOperationException: StatusCode cannot be set
        // because the response has already started» بود. ریشه: برخی استثناها
        // «در میانهٔ نوشتن بدنهٔ پاسخ» رخ می‌دهند (مثلاً خطای Serialize پس از
        // ارسال سرآیندها). در آن حالت Kestrel پاسخ را آغاز کرده و هر تلاش
        // برای تعیین StatusCode/ContentType خودِ استثنای دومی می‌اندازد؛ آن
        // استثنای دوم به‌صورت «unhandled exception» کل اتصال را می‌بست و
        // کلاینت WPF پاسخِ ناقص/خالی می‌گرفت («پاسخ سرور قابل تفسیر نبود»).
        // رفتار صحیح استاندارد ASP.NET Core: اگر پاسخ آغاز شده، فقط ثبت لاگ
        // بحرانی و بازگشت — دست زدن به پاسخ ممنوع. خطای اصلی از این پس در
        // لاگ ثبت می‌شود (نه استثنای ثانویهٔ مبهم) و اتصال سالم می‌ماند.
        if (context.Response.HasStarted)
        {
            _logger.LogCritical(
                "پاسخ پیش از رسیدگی به خطا آغاز شده است — بدنهٔ خطا نوشته نشد. " +
                "خطای اصلی: {ExceptionType} — TraceId: {TraceId}",
                exception.GetType().Name, traceId);
            return;
        }

        var response = new
        {
            isSuccess = false,
            statusCode,
            message,
            errors,
            traceId,
            data = (object?)null,
            elapsedMs = 0
        };

        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsync(JsonSerializer.Serialize(response,
            new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }));
    }
}
